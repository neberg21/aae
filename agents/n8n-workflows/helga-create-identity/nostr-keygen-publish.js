function makeMainRealmNostrTemplate(mainRealmAnchor, kind, createdAt, content) {
  const MainObject = Object.getPrototypeOf(mainRealmAnchor).constructor;
  const template = new MainObject();
  template.kind = kind;
  template.created_at = createdAt;
  template.tags = [];
  template.content = content;
  return template;
}

async function publishEvent(relay, event) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(relay);
    const t = setTimeout(() => {
      try {
        ws.close();
      } catch (_) {
        /* ignore */
      }
      reject(new Error('nostr publish timeout'));
    }, 10000);
    ws.addEventListener('open', () => {
      ws.send(JSON.stringify(['EVENT', event]));
    });
    ws.addEventListener('message', (msg) => {
      try {
        const data = JSON.parse(typeof msg.data === 'string' ? msg.data : msg.data.toString());
        if (data[0] === 'OK' && data[1] === event.id) {
          clearTimeout(t);
          try {
            ws.close();
          } catch (_) {
            /* ignore */
          }
          resolve(data[2] === true);
        }
      } catch (_) {
        /* ignore non-json */
      }
    });
    ws.addEventListener('error', () => {
      clearTimeout(t);
      reject(new Error('nostr ws error'));
    });
  });
}

const item = $input.first().json;
try {
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
  // Object literals from the n8n Code sandbox fail nostr-tools validateEvent
  // (cross-realm instanceof Object). Build the template in nostr-tools' realm
  // without new Function (n8n disallows code generation from strings).
  const template = makeMainRealmNostrTemplate(nostr, 0, created_at, content);
  const event = finalizeEvent(template, sk);
  const relay = item.relay || 'wss://nostr.neberg.de';
  const ok = await publishEvent(relay, event);
  if (!ok) {
    return { json: { ok: false, error: 'nostr_profile_failed', httpStatus: 502 } };
  }
  return [{ json: { ...item, npub, nsec, ok: true } }];
} catch (e) {
  return { json: { ok: false, error: 'nostr_profile_failed', httpStatus: 502, details: String(e) } };
}
