export const BACKEND_BASE = 'https://ai.neberg.de';

export function parseJsonMaybe(text) {
  if (text == null) return null;
  if (typeof text === 'object') return text;
  const raw = String(text).trim();
  const fenced = raw.match(/```(?:json)?\s*([\s\S]*?)```/i);
  const candidate = fenced ? fenced[1].trim() : raw;
  try {
    return JSON.parse(candidate);
  } catch {
    return null;
  }
}

export function normalizeWebhookBody(body) {
  const b = body ?? {};
  return {
    threadId: b.threadId ?? b.ThreadId ?? '',
    chatHistory: b.chatHistory ?? b.History ?? b.history ?? [],
    userVision: b.userVision ?? b.UserVision ?? '',
    delegationRequest: b.delegationRequest ?? b.DelegationRequest ?? null,
    taskContext: b.taskContext ?? b.TaskContext ?? null,
    subordinatesList: b.subordinatesList ?? b.SubordinatesList ?? [],
    allowedTools: b.allowedTools ?? b.AllowedTools ?? [],
    managerId: b.managerId ?? b.ManagerId ?? null,
    senderAgentId: b.senderAgentId ?? b.SenderAgentId ?? null,
  };
}

export function buildLeoPrompt({ userVision, chatHistory }) {
  return [
    'You are Leo, AAE CEO orchestrator. Output ONLY JSON.',
    'Decompose the vision into supervisor-level delegations (or helga hr_request).',
    'Never use the word teamleiter; use supervisor-* agent ids.',
    'Schema: {"delegations":[{"targetAgentId":"supervisor-...|helga","intent":"delegation|hr_request","message":"...","moduleScope":"Module.X"}]}',
    `Vision: ${userVision}`,
    `History: ${JSON.stringify(chatHistory ?? [])}`,
  ].join('\n');
}

function assertNoTeamleiter(id) {
  if (String(id).toLowerCase().includes('teamleiter')) {
    return false;
  }
  return true;
}

export function parseLeoDelegations(agentOutput, threadId) {
  const obj = parseJsonMaybe(agentOutput);
  if (!obj || !Array.isArray(obj.delegations) || obj.delegations.length === 0) {
    return {
      ok: false,
      error: 'invalid_leo_output',
      userMessage: 'Leo could not produce valid delegations.',
    };
  }
  const items = [];
  for (const d of obj.delegations) {
    if (!d?.targetAgentId || !assertNoTeamleiter(d.targetAgentId)) {
      return {
        ok: false,
        error: 'invalid_target',
        userMessage: 'Leo produced an invalid targetAgentId.',
      };
    }
    const scope = d.moduleScope ? ` [scope=${d.moduleScope}]` : '';
    const intent = d.intent ? ` (${d.intent})` : '';
    items.push({
      threadId,
      senderAgentId: 'leo',
      targetAgentId: d.targetAgentId,
      content: `${d.message ?? ''}${intent}${scope}`.trim(),
    });
  }
  return { ok: true, items };
}

export function mapHelgaIdentityToCreateRequest(identity, jobDescription) {
  return {
    jobTitle: identity.roleTitle ?? identity.jobTitle ?? '',
    jobDescription: jobDescription ?? identity.jobDescription ?? '',
    department: identity.department ?? '',
    managerId: identity.managerId ?? null,
    systemPrompt: identity.systemPrompt ?? '',
    guardrails: identity.guardrails ?? [],
    tools: identity.tools ?? identity.required_tools ?? [],
  };
}

export function parseHelgaDecision(agentOutput, threadId, jobDescription) {
  const obj = parseJsonMaybe(agentOutput);
  if (!obj || !obj.status) {
    return {
      ok: false,
      error: 'invalid_helga_output',
      userMessage: 'Helga returned invalid JSON.',
    };
  }
  if (obj.status === 'needs_clarification') {
    return {
      ok: true,
      branch: 'clarify',
      routeBody: {
        threadId,
        senderAgentId: 'helga',
        targetAgentId: 'User',
        content: String(obj.clarificationQuestions ?? 'Need more details.'),
      },
    };
  }
  if (obj.status === 'ready' && obj.identity) {
    if (obj.identity.managerId && !assertNoTeamleiter(obj.identity.managerId)) {
      return {
        ok: false,
        error: 'invalid_manager',
        userMessage: 'Helga used forbidden teamleiter naming.',
      };
    }
    return {
      ok: true,
      branch: 'create',
      createBody: mapHelgaIdentityToCreateRequest(obj.identity, jobDescription),
    };
  }
  return {
    ok: false,
    error: 'invalid_helga_status',
    userMessage: 'Helga status was neither ready nor needs_clarification.',
  };
}

export function parseSupervisorDecision(agentOutput, threadId, senderAgentId) {
  const obj = parseJsonMaybe(agentOutput);
  if (!obj || !obj.outcome) {
    return {
      ok: false,
      error: 'invalid_supervisor_output',
      userMessage: 'Supervisor returned invalid JSON.',
    };
  }
  const routeUrl = `${BACKEND_BASE}/api/agents/route-chat-message`;
  if (obj.outcome === 'waiting') {
    return {
      ok: true,
      outcome: 'waiting',
      httpCalls: [
        {
          url: routeUrl,
          body: {
            threadId,
            senderAgentId,
            targetAgentId: null,
            content: String(obj.statusMessage ?? 'Waiting on subordinates.'),
          },
        },
      ],
    };
  }
  if (obj.outcome === 'delegate') {
    const delegations = Array.isArray(obj.delegations) ? obj.delegations : [];
    if (delegations.length === 0) {
      return {
        ok: false,
        error: 'empty_delegations',
        userMessage: 'Supervisor chose delegate with no targets.',
      };
    }
    const httpCalls = [];
    for (const d of delegations) {
      if (!d?.targetAgentId || !assertNoTeamleiter(d.targetAgentId)) {
        return {
          ok: false,
          error: 'invalid_target',
          userMessage: 'Supervisor produced invalid targetAgentId.',
        };
      }
      httpCalls.push({
        url: routeUrl,
        body: {
          threadId,
          senderAgentId,
          targetAgentId: d.targetAgentId,
          content: String(d.content ?? ''),
        },
      });
    }
    return { ok: true, outcome: 'delegate', httpCalls };
  }
  if (obj.outcome === 'done') {
    return {
      ok: true,
      outcome: 'done',
      httpCalls: [
        {
          url: `${BACKEND_BASE}/api/await-request-approval`,
          body: {
            threadId,
            senderAgentId,
            content: String(obj.approval?.content ?? 'Ready for approval.'),
            artifacts: obj.approval?.artifacts ?? [],
          },
        },
      ],
    };
  }
  return {
    ok: false,
    error: 'unknown_outcome',
    userMessage: `Unknown supervisor outcome: ${obj.outcome}`,
  };
}

export function buildSpecialistDoneBody({
  threadId,
  senderAgentId,
  managerId,
  content,
}) {
  return {
    threadId,
    senderAgentId,
    targetAgentId: managerId,
    content,
  };
}

export function buildFailureRoute({ threadId, senderAgentId, content }) {
  return {
    threadId,
    senderAgentId,
    targetAgentId: 'User',
    content,
  };
}
