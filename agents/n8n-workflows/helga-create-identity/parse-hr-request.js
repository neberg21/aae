function deriveAgentId(moduleScope, role) {
  if (typeof moduleScope !== 'string' || typeof role !== 'string') throw new Error('moduleScope and role required');
  const trimmedRole = role.trim().toLowerCase();
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(trimmedRole)) throw new Error('role must be kebab-case');
  let domain = moduleScope.trim().replace(/^module\./i, '').toLowerCase();
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(domain) && !/^[a-z0-9]+$/.test(domain)) throw new Error('invalid module_scope domain');
  return `${domain}-${trimmedRole}`;
}
function identityRepoPath(agentId) {
  return `agents/identities/${agentId}.json`;
}
function parseHrRequest(body) {
  if (!body || typeof body !== 'object') return { ok: false, error: 'invalid_hr_request', details: ['body'] };
  if (body.target_agent !== '@Helga') return { ok: false, error: 'invalid_hr_request', details: ['target_agent'] };
  if (body.intent !== 'hr_request') return { ok: false, error: 'invalid_hr_request', details: ['intent'] };
  const payload = body.payload;
  if (!payload || typeof payload !== 'object') return { ok: false, error: 'invalid_hr_request', details: ['payload'] };
  if (typeof payload.module_scope !== 'string' || !payload.module_scope.trim()) return { ok: false, error: 'invalid_hr_request', details: ['payload.module_scope'] };
  if (typeof payload.role !== 'string' || !payload.role.trim()) return { ok: false, error: 'invalid_hr_request', details: ['payload.role'] };
  let agentId;
  try {
    agentId = deriveAgentId(payload.module_scope, payload.role);
  } catch (e) {
    return { ok: false, error: 'invalid_hr_request', details: ['derive_agent_id', e instanceof Error ? e.message : String(e)] };
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
const cfg = $('Config').first().json;
const webhook = $('Webhook').first().json;
const body = webhook.body ?? webhook;
console.log('Parse HR Request: body', JSON.stringify(body));
const parsed = parseHrRequest(body);
if (!parsed.ok) {
  console.log('Parse HR Request: failed', parsed.error, parsed.details);
  return { json: { ok: false, error: parsed.error, details: parsed.details, httpStatus: 400 } };
}
console.log('Parse HR Request: ok', parsed.value.agent_id, parsed.value.path);
return [{ json: { ok: true, ...parsed.value, githubRepo: cfg.githubRepo, relay: cfg.relay, helgaPredictionUrl: cfg.helgaPredictionUrl } }];
