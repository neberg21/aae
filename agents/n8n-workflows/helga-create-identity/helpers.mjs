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
 * Build an EventTemplate that survives n8n's secure Code sandbox.
 * Sandbox object literals fail nostr-tools validateEvent (cross-realm
 * `instanceof Object`). n8n also shims Object.getPrototypeOf → {}, so we cannot
 * recover the main-realm Object constructor. nip19.decode() returns an object
 * allocated inside nostr-tools (main realm).
 */
export function makeNostrEventTemplate(nip19, pubkey, kind, createdAt, content) {
  if (!nip19 || typeof nip19.npubEncode !== 'function' || typeof nip19.decode !== 'function') {
    throw new Error('nip19 required');
  }
  if (typeof kind !== 'number') throw new Error('kind must be number');
  if (typeof createdAt !== 'number') throw new Error('createdAt must be number');
  if (typeof content !== 'string') throw new Error('content must be string');
  const template = nip19.decode(nip19.npubEncode(pubkey));
  template.kind = kind;
  template.created_at = createdAt;
  template.tags = [];
  template.content = content;
  return template;
}

/** Strip nip19 decode leftovers before publishing to a relay. */
export function toRelayEvent(signed) {
  return {
    id: signed.id,
    pubkey: signed.pubkey,
    created_at: signed.created_at,
    kind: signed.kind,
    tags: signed.tags,
    content: signed.content,
    sig: signed.sig,
  };
}

/**
 * Publish a signed event over WebSocket (NIP-01 EVENT / OK).
 * Pass the `ws` package constructor — n8n Code nodes have no global WebSocket.
 */
export function publishEvent(WebSocketCtor, relay, event) {
  if (typeof WebSocketCtor !== 'function') {
    throw new Error('WebSocket constructor required (require("ws") in n8n Code node)');
  }
  return new Promise((resolve, reject) => {
    const ws = new WebSocketCtor(relay);
    const t = setTimeout(() => {
      try {
        ws.close();
      } catch (_) {
        /* ignore */
      }
      reject(new Error('nostr publish timeout'));
    }, 10000);
    ws.on('open', () => {
      ws.send(JSON.stringify(['EVENT', event]));
    });
    ws.on('message', (data) => {
      try {
        const raw = typeof data === 'string' ? data : data.toString();
        const parsed = JSON.parse(raw);
        if (parsed[0] === 'OK' && parsed[1] === event.id) {
          clearTimeout(t);
          try {
            ws.close();
          } catch (_) {
            /* ignore */
          }
          resolve(parsed[2] === true);
        }
      } catch (_) {
        /* ignore non-json */
      }
    });
    ws.on('error', () => {
      clearTimeout(t);
      reject(new Error('nostr ws error'));
    });
  });
}

export function parseGithubFileJson(base64Content) {
  const json = Buffer.from(base64Content, 'base64').toString('utf8');
  return JSON.parse(json);
}
