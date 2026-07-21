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

export function parseGithubFileJson(base64Content) {
  const json = Buffer.from(base64Content, 'base64').toString('utf8');
  return JSON.parse(json);
}
