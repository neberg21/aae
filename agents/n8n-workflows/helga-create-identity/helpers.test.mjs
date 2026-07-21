import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { createRequire } from 'node:module';
import {
  parseHrRequest,
  deriveAgentId,
  validateHelgaIdentity,
  extractHelgaCandidate,
  buildPublicIdentity,
  identityRepoPath,
  parseGithubFileJson,
  makeNostrEventTemplate,
  toRelayEvent,
} from './helpers.mjs';

const require = createRequire(import.meta.url);

const validIdentity = {
  agent_id: 'teamleiter-finanzen',
  role_title: 'Teamleiter Finanzen',
  department: 'Operations',
  system_prompt: 'Du bist der Teamleiter im Finanzmodul.',
  required_tools: ['financial_report_generator'],
  guardrails: ['Regel 1: Darf nur im Verzeichnis Module.Finanzen arbeiten'],
};

describe('deriveAgentId', () => {
  it('builds domain-role from Module.Finanzen + teamleiter', () => {
    assert.equal(deriveAgentId('Module.Finanzen', 'teamleiter'), 'finanzen-teamleiter');
  });
  it('lowercases domain and accepts Module.Dnd', () => {
    assert.equal(deriveAgentId('Module.Dnd', 'backend'), 'dnd-backend');
  });
  it('strips Module. case-insensitively', () => {
    assert.equal(deriveAgentId('module.Finanzen', 'researcher'), 'finanzen-researcher');
  });
});

describe('parseHrRequest', () => {
  it('accepts valid Leo envelope', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Helga',
      intent: 'hr_request',
      payload: {
        message: 'hire',
        context: '',
        module_scope: 'Module.Finanzen',
        role: 'teamleiter',
      },
    });
    assert.equal(r.ok, true);
    assert.equal(r.value.agent_id, 'finanzen-teamleiter');
  });
  it('rejects wrong target', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Leo',
      intent: 'hr_request',
      payload: { module_scope: 'Module.Finanzen', role: 'teamleiter', message: '', context: '' },
    });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'invalid_hr_request');
    assert.deepEqual(r.details, ['target_agent']);
  });
  it('rejects missing role', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Helga',
      intent: 'hr_request',
      payload: { module_scope: 'Module.Finanzen', message: '', context: '' },
    });
    assert.equal(r.ok, false);
    assert.deepEqual(r.details, ['payload.role']);
  });
  it('rejects invalid role format with derive details', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Helga',
      intent: 'hr_request',
      payload: { module_scope: 'Module.Finanzen', role: 'Team Leiter', message: '', context: '' },
    });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'invalid_hr_request');
    assert.equal(r.details[0], 'derive_agent_id');
    assert.equal(r.details[1], 'role must be kebab-case');
  });
});

describe('validateHelgaIdentity', () => {
  it('requires core fields and overwrites agent_id externally', () => {
    const r = validateHelgaIdentity({
      agent_id: 'ignored',
      role_title: 'Teamleiter Finanzen',
      department: 'Operations',
      system_prompt: 'You are...',
      required_tools: ['github_read'],
      guardrails: ['Only Module.Finanzen'],
    });
    assert.equal(r.ok, true);
  });
  it('fails when system_prompt missing', () => {
    const r = validateHelgaIdentity({
      role_title: 'X',
      department: 'Backend',
      required_tools: [],
      guardrails: [],
    });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'invalid_identity');
  });
});

describe('extractHelgaCandidate', () => {
  it('parses Flowise fullResponse body.text JSON string', () => {
    const res = {
      statusCode: 200,
      body: { text: JSON.stringify(validIdentity), question: 'Create Teamleiter Finanzen' },
    };
    const r = extractHelgaCandidate(res);
    assert.equal(r.ok, true);
    assert.equal(r.value.role_title, 'Teamleiter Finanzen');
    assert.deepEqual(r.value.required_tools, ['financial_report_generator']);
  });

  it('accepts body.text when already an object', () => {
    const res = {
      statusCode: 200,
      body: { text: validIdentity, question: 'Create Teamleiter Finanzen' },
    };
    const r = extractHelgaCandidate(res);
    assert.equal(r.ok, true);
    assert.equal(r.value.department, 'Operations');
  });

  it('falls back to agentFlowExecutedData output.content', () => {
    const res = {
      statusCode: 200,
      body: {
        text: 'not-json',
        agentFlowExecutedData: [
          {
            nodeLabel: 'Helga',
            data: { output: { content: JSON.stringify(validIdentity) } },
          },
        ],
      },
    };
    const r = extractHelgaCandidate(res);
    assert.equal(r.ok, true);
    assert.equal(r.value.role_title, 'Teamleiter Finanzen');
  });

  it('returns helga_unavailable on HTTP error status', () => {
    const r = extractHelgaCandidate({ statusCode: 500, body: { text: '{}' } });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'helga_unavailable');
    assert.equal(r.httpStatus, 502);
  });

  it('returns invalid_identity when text is non-JSON prose', () => {
    const r = extractHelgaCandidate({
      statusCode: 200,
      body: { text: '**Stellenbeschreibung: Teamleiter Finanzen**' },
    });
    assert.equal(r.ok, false);
    assert.equal(r.error, 'invalid_identity');
    assert.equal(r.httpStatus, 422);
  });
});

describe('buildPublicIdentity', () => {
  it('never includes nsec', () => {
    const id = buildPublicIdentity({
      helga: {
        role_title: 'Teamleiter Finanzen',
        department: 'Operations',
        system_prompt: 'p',
        required_tools: [],
        guardrails: [],
      },
      agentId: 'finanzen-teamleiter',
      npub: 'npub1test',
      displayName: 'Max (Teamleiter Finanzen)',
      relay: 'wss://nostr.neberg.de',
    });
    assert.equal(id.agent_id, 'finanzen-teamleiter');
    assert.equal(id.nostr.npub, 'npub1test');
    assert.equal(id.nostr.display_name, 'Max (Teamleiter Finanzen)');
    assert.equal(Object.prototype.hasOwnProperty.call(id, 'nsec'), false);
    assert.equal(Object.prototype.hasOwnProperty.call(id.nostr, 'nsec'), false);
  });
});

describe('identityRepoPath', () => {
  it('returns agents/identities path', () => {
    assert.equal(identityRepoPath('finanzen-teamleiter'), 'agents/identities/finanzen-teamleiter.json');
  });
});

describe('parseGithubFileJson', () => {
  it('decodes base64 content', () => {
    const obj = { agent_id: 'finanzen-teamleiter', nostr: { npub: 'npub1x', display_name: 'A (B)' } };
    const b64 = Buffer.from(JSON.stringify(obj), 'utf8').toString('base64');
    assert.deepEqual(parseGithubFileJson(b64), obj);
  });
});

describe('makeNostrEventTemplate', () => {
  it('lets finalizeEvent succeed under n8n secure Code shims', () => {
    try {
      require('nostr-tools');
    } catch {
      assert.fail('nostr-tools must be installed to run this test (npm install in this folder)');
    }
    // Mirror n8n createVmExecutableCode secure shims + no eval.
    const prelude = [
      'Object.getPrototypeOf = () => ({})',
      'Reflect.getPrototypeOf = () => ({})',
      'Object.setPrototypeOf = () => false',
      'Reflect.setPrototypeOf = () => false',
      'Object.defineProperty = () => ({})',
      'Object.defineProperties = () => ({})',
    ].join(';');
    const sandbox = { require, console, Math, Date, JSON, Object, Array, makeNostrEventTemplate, toRelayEvent };
    const ctx = vm.createContext(sandbox, {
      codeGeneration: { strings: false, wasm: false },
    });
    const result = vm.runInContext(
      `
      ${prelude};
      const nostr = require('nostr-tools');
      const sk = nostr.generateSecretKey();
      const pk = nostr.getPublicKey(sk);
      const content = JSON.stringify({ name: 'Judith (Teamleiter Finanzen)', about: 'Operations · Teamleiter Finanzen' });
      const created_at = Math.floor(Date.now() / 1000);
      let brokenMsg = null;
      try {
        nostr.finalizeEvent({ kind: 0, created_at, tags: [], content }, sk);
      } catch (e) {
        brokenMsg = String(e.message || e);
      }
      let evalBlocked = false;
      try {
        Function('return 1');
      } catch (e) {
        evalBlocked = e instanceof EvalError || /Code generation from strings disallowed/i.test(String(e));
      }
      const template = makeNostrEventTemplate(nostr.nip19, pk, 0, created_at, content);
      const signed = nostr.finalizeEvent(template, sk);
      const event = toRelayEvent(signed);
      ({
        brokenMsg,
        evalBlocked,
        id: event.id,
        kind: event.kind,
        hasType: Object.prototype.hasOwnProperty.call(event, 'type'),
        sigLen: event.sig.length,
      });
      `,
      ctx,
    );
    assert.equal(result.brokenMsg, "can't serialize event with wrong or missing properties");
    assert.equal(result.evalBlocked, true);
    assert.equal(typeof result.id, 'string');
    assert.equal(result.id.length, 64);
    assert.equal(result.kind, 0);
    assert.equal(result.hasType, false);
    assert.equal(result.sigLen, 128);
  });
});
