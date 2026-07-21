import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  parseHrRequest,
  deriveAgentId,
  validateHelgaIdentity,
  buildPublicIdentity,
  identityRepoPath,
  parseGithubFileJson,
} from './helpers.mjs';

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
  });
  it('rejects missing role', () => {
    const r = parseHrRequest({
      action: 'route_message',
      target_agent: '@Helga',
      intent: 'hr_request',
      payload: { module_scope: 'Module.Finanzen', message: '', context: '' },
    });
    assert.equal(r.ok, false);
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
