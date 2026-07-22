function makeNostrEventTemplate(nip19, pubkey, kind, createdAt, content) {
  // n8n secure Code runner shims Object.getPrototypeOf → always {}, so we cannot
  // recover the main-realm Object constructor. Object literals are cross-realm and
  // fail nostr-tools validateEvent. nip19.decode() returns a plain object created
  // inside nostr-tools (main realm), which passes instanceof Object there.
  const template = nip19.decode(nip19.npubEncode(pubkey));
  template.kind = kind;
  template.created_at = createdAt;
  template.tags = [];
  template.content = content;
  return template;
}

function toRelayEvent(signed) {
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

function publishEvent(WebSocketCtor, relay, event) {
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

const item = $input.first().json;
try {
  const WebSocket = require('ws');
  const nostr = require('nostr-tools');
  const generateSecretKey = nostr.generateSecretKey || nostr.pure?.generateSecretKey;
  const getPublicKey = nostr.getPublicKey || nostr.pure?.getPublicKey;
  const finalizeEvent = nostr.finalizeEvent || nostr.pure?.finalizeEvent;
  const nip19 = nostr.nip19;
  if (!generateSecretKey || !getPublicKey || !finalizeEvent || !nip19) {
    throw new Error('nostr-tools exports missing');
  }
  const sk = generateSecretKey();
  const pk = getPublicKey(sk);
  const nsec = nip19.nsecEncode(sk);
  const npub = nip19.npubEncode(pk);
  const display_name = item.display_name;
  const about = `${item.helga.department} · ${item.helga.role_title}`;
  const content = JSON.stringify({ name: display_name, about });
  const created_at = Math.floor(Date.now() / 1000);
  const template = makeNostrEventTemplate(nip19, pk, 0, created_at, content);
  const signed = finalizeEvent(template, sk);
  const event = toRelayEvent(signed);
  const relay = item.relay || 'wss://nostr.neberg.de';
  const ok = await publishEvent(WebSocket, relay, event);
  if (!ok) {
    return { json: { ok: false, error: 'nostr_profile_failed', httpStatus: 502 } };
  }
  return [{ json: { ...item, npub, nsec, ok: true } }];
} catch (e) {
  return { json: { ok: false, error: 'nostr_profile_failed', httpStatus: 502, details: String(e) } };
}
