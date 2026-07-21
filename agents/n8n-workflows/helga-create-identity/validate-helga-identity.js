function validateHelgaIdentity(obj) {
  const details = [];
  if (!obj || typeof obj !== 'object') return { ok: false, error: 'invalid_identity', details: ['body'] };
  for (const key of ['role_title', 'department', 'system_prompt']) {
    if (typeof obj[key] !== 'string' || !obj[key].trim()) details.push(key);
  }
  if (!Array.isArray(obj.required_tools)) details.push('required_tools');
  if (!Array.isArray(obj.guardrails)) details.push('guardrails');
  if (details.length) return { ok: false, error: 'invalid_identity', details };
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
const prev = $('Parse HR Request').first().json;
const res = $input.first().json;
const status = res.statusCode ?? res.status;
if (!status || status < 200 || status >= 300) {
  return [{ json: { ok: false, error: 'helga_unavailable', httpStatus: 502 } }];
}
let raw = res.body ?? res;
if (typeof raw === 'string') {
  try { raw = JSON.parse(raw); } catch { return [{ json: { ok: false, error: 'helga_unavailable', httpStatus: 502 } }]; }
}
let candidate = raw;
if (typeof raw.text === 'string') {
  const t = raw.text.trim();
  try { candidate = JSON.parse(t); } catch {
    const m = t.match(/```(?:json)?\s*([\s\S]*?)```/);
    if (m) { try { candidate = JSON.parse(m[1]); } catch { /* keep */ } }
    else { try { candidate = JSON.parse(t); } catch { return [{ json: { ok: false, error: 'helga_unavailable', httpStatus: 502 } }]; }
  }
} else if (raw.json && typeof raw.json === 'object') {
  candidate = raw.json;
}
const v = validateHelgaIdentity(candidate);
if (!v.ok) {
  return [{ json: { ok: false, error: v.error, details: v.details, httpStatus: 422 } }];
}
return [{ json: { ok: true, helga: v.value, agent_id: prev.agent_id, path: prev.path, payload: prev.payload, githubRepo: prev.githubRepo, relay: prev.relay } }];
