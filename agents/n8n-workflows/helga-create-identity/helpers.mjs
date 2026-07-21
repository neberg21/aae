const RELAY_DEFAULT = 'wss://nostr.neberg.de';

export function deriveAgentId(moduleScope, role) {
  if (typeof moduleScope !== 'string' || typeof role !== 'string') {
    throw new Error('moduleScope and role required');
  }
  const trimmedRole = role.trim().toLowerCase();
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(trimmedRole)) {
    throw new Error('role must be kebab-case');
  }
  let domain = moduleScope.trim();
  domain = domain.replace(/^module\./i, '');
  domain = domain.toLowerCase();
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(domain)) {
    if (!/^[a-z0-9]+$/.test(domain)) {
      throw new Error('invalid module_scope domain');
    }
  }
  return `${domain}-${trimmedRole}`;
}

export function parseHrRequest(body) {
  if (!body || typeof body !== 'object') {
    return { ok: false, error: 'invalid_hr_request', details: ['body'] };
  }
  if (body.target_agent !== '@Helga') {
    return { ok: false, error: 'invalid_hr_request', details: ['target_agent'] };
  }
  if (body.intent !== 'hr_request') {
    return { ok: false, error: 'invalid_hr_request', details: ['intent'] };
  }
  const payload = body.payload;
  if (!payload || typeof payload !== 'object') {
    return { ok: false, error: 'invalid_hr_request', details: ['payload'] };
  }
  if (typeof payload.module_scope !== 'string' || !payload.module_scope.trim()) {
    return { ok: false, error: 'invalid_hr_request', details: ['payload.module_scope'] };
  }
  if (typeof payload.role !== 'string' || !payload.role.trim()) {
    return { ok: false, error: 'invalid_hr_request', details: ['payload.role'] };
  }
  let agentId;
  try {
    agentId = deriveAgentId(payload.module_scope, payload.role);
  } catch (e) {
    return {
      ok: false,
      error: 'invalid_hr_request',
      details: ['derive_agent_id', e instanceof Error ? e.message : String(e)],
    };
  }
  return {
    ok: true,
    value: {
      action: body.action,
      target_agent: body.target_agent,
      intent: body.intent,
      payload: {
        message: typeof payload.message === 'string' ? payload.message : '',
        context: typeof payload.context === 'string' ? payload.context : '',
        module_scope: payload.module_scope.trim(),
        role: payload.role.trim().toLowerCase(),
      },
      agent_id: agentId,
      path: identityRepoPath(agentId),
    },
  };
}

export function validateHelgaIdentity(obj) {
  const details = [];
  if (!obj || typeof obj !== 'object') {
    return { ok: false, error: 'invalid_identity', details: ['body'] };
  }
  for (const key of ['role_title', 'department', 'system_prompt']) {
    if (typeof obj[key] !== 'string' || !obj[key].trim()) details.push(key);
  }
  if (!Array.isArray(obj.required_tools)) details.push('required_tools');
  if (!Array.isArray(obj.guardrails)) details.push('guardrails');
  if (details.length) {
    return { ok: false, error: 'invalid_identity', details };
  }
  return {
    ok: true,
    value: {
      role_title: obj.role_title.trim(),
      department: obj.department.trim(),
      system_prompt: obj.system_prompt,
      required_tools: obj.required_tools,
      guardrails: obj.guardrails,
    },
  };
}

function tryParseJsonText(text) {
  if (typeof text !== 'string') return null;
  const t = text.trim();
  if (!t) return null;
  try {
    return JSON.parse(t);
  } catch {
    /* continue */
  }
  const m = t.match(/```(?:json)?\s*([\s\S]*?)```/);
  if (!m) return null;
  try {
    return JSON.parse(m[1]);
  } catch {
    return null;
  }
}

function candidateFromFlowiseBody(raw) {
  if (!raw || typeof raw !== 'object') return null;

  if (typeof raw.text === 'object' && raw.text !== null && !Array.isArray(raw.text)) {
    return raw.text;
  }
  if (typeof raw.text === 'string') {
    const parsed = tryParseJsonText(raw.text);
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) return parsed;
  }
  if (raw.json && typeof raw.json === 'object' && !Array.isArray(raw.json)) {
    return raw.json;
  }

  const flows = raw.agentFlowExecutedData;
  if (Array.isArray(flows)) {
    for (let i = flows.length - 1; i >= 0; i--) {
      const content = flows[i]?.data?.output?.content;
      const parsed = tryParseJsonText(content);
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) return parsed;
    }
  }

  const history = raw.agentFlowExecutedData?.[raw.agentFlowExecutedData.length - 1]?.data?.chatHistory;
  if (Array.isArray(history)) {
    for (let i = history.length - 1; i >= 0; i--) {
      const msg = history[i];
      if (msg?.role !== 'assistant') continue;
      const parsed = tryParseJsonText(msg.content);
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) return parsed;
    }
  }

  return null;
}

export function extractHelgaCandidate(res) {
  if (!res || typeof res !== 'object') {
    return { ok: false, error: 'helga_unavailable', httpStatus: 502 };
  }
  const status = res.statusCode ?? res.status;
  if (status != null && (status < 200 || status >= 300)) {
    return { ok: false, error: 'helga_unavailable', httpStatus: 502 };
  }

  let raw = res.body ?? res;
  if (typeof raw === 'string') {
    try {
      raw = JSON.parse(raw);
    } catch {
      return { ok: false, error: 'helga_unavailable', httpStatus: 502 };
    }
  }

  const candidate = candidateFromFlowiseBody(raw);
  if (!candidate) {
    return { ok: false, error: 'invalid_identity', details: ['body'], httpStatus: 422 };
  }
  const validated = validateHelgaIdentity(candidate);
  if (!validated.ok) {
    return { ok: false, error: validated.error, details: validated.details, httpStatus: 422 };
  }
  return { ok: true, value: validated.value };
}

export function buildPublicIdentity({ helga, agentId, npub, displayName, relay }) {
  return {
    agent_id: agentId,
    role_title: helga.role_title,
    department: helga.department,
    system_prompt: helga.system_prompt,
    required_tools: helga.required_tools,
    guardrails: helga.guardrails,
    nostr: {
      npub,
      display_name: displayName,
      relay: relay || RELAY_DEFAULT,
    },
  };
}

export function identityRepoPath(agentId) {
  return `agents/identities/${agentId}.json`;
}

/**
 * Build a kind-0 EventTemplate in the same JS realm as `anchorFn` (e.g. nostr-tools
 * generateSecretKey). n8n Code nodes run in a VM where object literals fail
 * nostr-tools validateEvent (`instanceof Object` is cross-realm false), which
 * surfaces as: can't serialize event with wrong or missing properties.
 */
export function makeMainRealmNostrTemplate(anchorFn, kind, createdAt, content) {
  if (typeof anchorFn !== 'function') throw new Error('anchorFn required');
  if (typeof kind !== 'number') throw new Error('kind must be number');
  if (typeof createdAt !== 'number') throw new Error('createdAt must be number');
  if (typeof content !== 'string') throw new Error('content must be string');
  const makeTemplate = anchorFn.constructor.constructor(
    'return function (kind, created_at, tags, content) { return { kind: kind, created_at: created_at, tags: tags, content: content }; }',
  )();
  const tags = anchorFn.constructor.constructor('return []')();
  return makeTemplate(kind, createdAt, tags, content);
}

export function parseGithubFileJson(base64Content) {
  const json = Buffer.from(base64Content, 'base64').toString('utf8');
  return JSON.parse(json);
}
