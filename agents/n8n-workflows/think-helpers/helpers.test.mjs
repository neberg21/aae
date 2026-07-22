import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  BACKEND_BASE,
  normalizeWebhookBody,
  parseLeoDelegations,
  parseHelgaDecision,
  mapHelgaIdentityToCreateRequest,
  parseSupervisorDecision,
  buildSpecialistDoneBody,
  buildFailureRoute,
  buildHelgaHrRequestContent,
  buildParkDelegationBody,
  planLeoItemAction,
} from './helpers.mjs';

describe('normalizeWebhookBody', () => {
  it('maps PascalCase backend fields to camelCase', () => {
    const n = normalizeWebhookBody({
      ThreadId: 't1',
      History: [{ role: 'user', content: 'hi' }],
      UserVision: 'build app',
    });
    assert.equal(n.threadId, 't1');
    assert.deepEqual(n.chatHistory, [{ role: 'user', content: 'hi' }]);
    assert.equal(n.userVision, 'build app');
  });
});

describe('parseLeoDelegations', () => {
  it('expands one HTTP body per delegation', () => {
    const r = parseLeoDelegations(
      JSON.stringify({
        delegations: [
          {
            targetAgentId: 'supervisor-finanzen',
            intent: 'delegation',
            message: 'Own Finanzen module',
            moduleScope: 'Module.Finanzen',
          },
          {
            targetAgentId: 'helga',
            intent: 'hr_request',
            message: 'Need supervisor-dnd',
            moduleScope: 'Module.Dnd',
          },
        ],
      }),
      'thread-1',
    );
    assert.equal(r.ok, true);
    assert.equal(r.items.length, 2);
    assert.equal(r.items[0].senderAgentId, 'leo');
    assert.equal(r.items[0].targetAgentId, 'supervisor-finanzen');
    assert.match(r.items[0].content, /Finanzen/);
    assert.equal(r.items[1].targetAgentId, 'helga');
  });

  it('rejects teamleiter naming', () => {
    const r = parseLeoDelegations(
      JSON.stringify({
        delegations: [{ targetAgentId: 'teamleiter-x', message: 'nope' }],
      }),
      't',
    );
    assert.equal(r.ok, false);
  });
});

describe('parseHelgaDecision', () => {
  it('maps ready identity to create-identity body', () => {
    const r = parseHelgaDecision(
      JSON.stringify({
        status: 'ready',
        clarificationQuestions: null,
        identity: {
          agentId: 'supervisor-finanzen',
          roleTitle: 'Supervisor Finanzen',
          department: 'Operations',
          systemPrompt: 'You supervise Finanzen.',
          tools: ['github_read'],
          guardrails: ['Only Module.Finanzen'],
          managerId: 'leo',
        },
      }),
      't1',
      'Hire Finanzen supervisor',
    );
    assert.equal(r.ok, true);
    assert.equal(r.branch, 'create');
    assert.equal(r.createBody.jobTitle, 'Supervisor Finanzen');
    assert.equal(r.createBody.managerId, 'leo');
    assert.equal(r.createBody.agentId, 'supervisor-finanzen');
    assert.deepEqual(r.createBody.tools, ['github_read']);
  });

  it('rejects ready identity without agentId', () => {
    const r = parseHelgaDecision(
      JSON.stringify({
        status: 'ready',
        clarificationQuestions: null,
        identity: {
          roleTitle: 'Supervisor Finance',
          department: 'Operations',
          systemPrompt: '…',
          tools: [],
          guardrails: [],
          managerId: 'leo',
        },
      }),
      't1',
      'hire',
    );
    assert.equal(r.ok, false);
  });

  it('routes clarification to User', () => {
    const r = parseHelgaDecision(
      JSON.stringify({
        status: 'needs_clarification',
        clarificationQuestions: 'Which module scope?',
        identity: null,
      }),
      't1',
      'hire someone',
    );
    assert.equal(r.ok, true);
    assert.equal(r.branch, 'clarify');
    assert.equal(r.routeBody.targetAgentId, 'User');
    assert.equal(r.routeBody.senderAgentId, 'helga');
  });
});

describe('parseSupervisorDecision', () => {
  it('builds waiting UI status call', () => {
    const r = parseSupervisorDecision(
      JSON.stringify({
        outcome: 'waiting',
        statusMessage: 'Waiting on specialist-react',
        delegations: [],
        approval: null,
      }),
      't1',
      'supervisor-finanzen',
    );
    assert.equal(r.ok, true);
    assert.equal(r.outcome, 'waiting');
    assert.equal(r.httpCalls.length, 1);
    assert.equal(
      r.httpCalls[0].url,
      `${BACKEND_BASE}/api/agents/route-chat-message`,
    );
    assert.equal(r.httpCalls[0].body.targetAgentId, null);
  });

  it('builds one route call per delegation', () => {
    const r = parseSupervisorDecision(
      JSON.stringify({
        outcome: 'delegate',
        statusMessage: null,
        delegations: [
          { targetAgentId: 'specialist-react', content: 'Build UI' },
          { targetAgentId: 'specialist-api', content: 'Build API' },
        ],
        approval: null,
      }),
      't1',
      'supervisor-finanzen',
    );
    assert.equal(r.ok, true);
    assert.equal(r.httpCalls.length, 2);
    assert.equal(r.httpCalls[0].body.targetAgentId, 'specialist-react');
  });

  it('builds approval call when done', () => {
    const r = parseSupervisorDecision(
      JSON.stringify({
        outcome: 'done',
        statusMessage: null,
        delegations: [],
        approval: { content: 'Please approve architecture', artifacts: [] },
      }),
      't1',
      'supervisor-finanzen',
    );
    assert.equal(r.ok, true);
    assert.equal(
      r.httpCalls[0].url,
      `${BACKEND_BASE}/api/await-request-approval`,
    );
    assert.equal(r.httpCalls[0].body.threadId, 't1');
  });
});

describe('buildSpecialistDoneBody', () => {
  it('routes result to managerId', () => {
    const body = buildSpecialistDoneBody({
      threadId: 't1',
      senderAgentId: 'specialist-react',
      managerId: 'supervisor-finanzen',
      content: 'UI done',
    });
    assert.equal(body.targetAgentId, 'supervisor-finanzen');
  });
});

describe('buildFailureRoute', () => {
  it('notifies User', () => {
    const body = buildFailureRoute({
      threadId: 't1',
      senderAgentId: 'leo',
      content: 'parse failed',
    });
    assert.equal(body.targetAgentId, 'User');
  });
});

describe('planLeoItemAction', () => {
  it('routes when supervisor exists', () => {
    const item = {
      threadId: 't1',
      senderAgentId: 'leo',
      targetAgentId: 'supervisor-finance',
      content: 'Own Finance (delegation) [scope=Module.Finance]',
      intent: 'delegation',
      message: 'Own Finance',
      moduleScope: 'Module.Finance',
    };
    const planned = planLeoItemAction(item, true);
    assert.equal(planned.action, 'route');
    assert.equal(planned.routeBody.targetAgentId, 'supervisor-finance');
    assert.equal(planned.routeBody.content, item.content);
  });

  it('parks and wakes helga when supervisor missing', () => {
    const item = {
      threadId: 't1',
      senderAgentId: 'leo',
      targetAgentId: 'supervisor-finance',
      content: 'Own Finance (delegation) [scope=Module.Finance]',
      intent: 'delegation',
      message: 'Own Finance',
      moduleScope: 'Module.Finance',
    };
    const planned = planLeoItemAction(item, false);
    assert.equal(planned.action, 'park_and_hire');
    assert.equal(planned.parkBody.targetAgentId, 'supervisor-finance');
    assert.equal(planned.parkBody.content, item.content);
    assert.equal(planned.helgaRouteBody.targetAgentId, 'helga');
    const hr = JSON.parse(planned.helgaRouteBody.content);
    assert.equal(hr.intent, 'hr_request');
    assert.equal(hr.agentId, 'supervisor-finance');
    assert.equal(hr.role, 'supervisor');
    assert.equal(hr.moduleScope, 'Module.Finance');
    assert.equal(hr.message, 'Own Finance');
  });

  it('routes helga items without park', () => {
    const item = {
      threadId: 't1',
      senderAgentId: 'leo',
      targetAgentId: 'helga',
      content: 'Need hire (hr_request)',
      intent: 'hr_request',
      message: 'Need hire',
      moduleScope: null,
    };
    const planned = planLeoItemAction(item, false);
    assert.equal(planned.action, 'route');
    assert.equal(planned.routeBody.targetAgentId, 'helga');
  });
});

describe('mapHelgaIdentityToCreateRequest agentId', () => {
  it('includes agentId', () => {
    const body = mapHelgaIdentityToCreateRequest(
      {
        agentId: 'supervisor-finance',
        roleTitle: 'Supervisor Finance',
        department: 'Operations',
        systemPrompt: '…',
        tools: [],
        guardrails: [],
        managerId: 'leo',
      },
      'Hire finance supervisor',
    );
    assert.equal(body.agentId, 'supervisor-finance');
  });
});
