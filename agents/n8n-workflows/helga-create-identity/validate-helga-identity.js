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

function extractHelgaCandidate(res) {
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

const prev = $('Parse HR Request').first().json;
const res = $input.first().json;
const extracted = extractHelgaCandidate(res);
if (!extracted.ok) {
  return [{ json: { ok: false, error: extracted.error, details: extracted.details, httpStatus: extracted.httpStatus } }];
}
return [{
  json: {
    ok: true,
    helga: extracted.value,
    agent_id: prev.agent_id,
    path: prev.path,
    payload: prev.payload,
    githubRepo: prev.githubRepo,
    relay: prev.relay,
  },
}];
