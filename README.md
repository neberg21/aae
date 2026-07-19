So funktioniert's:

Community Node installieren: In deinem self-hosted n8n unter Settings → Community Nodes das Paket n8n-nodes-nostrobots installieren.
Credential anlegen: Typ "Nostrobots API" → deinen privaten Nostr-Key (nsec oder hex) eintragen. Das ist das "hinterlegte Konto".
Workflow importieren (... → Import from File), Credential in beiden Nostr-Nodes auswählen.
Im "👉 HIER: Deine Logik einfügen"-Node später ersetzen, was wirklich passieren soll.
Aktivieren – läuft dann alle 2 Minuten, holt neue DMs, entschlüsselt sie automatisch (NIP-04) und schickt testweise ein Echo zurück.

Wichtig: Kein echtes Push/Realtime, sondern Polling alle 2 Minuten (Nostr-DMs lassen sich mit dieser Node nur so zuverlässig abgreifen). Die Dedupe-Logik im Code-Node verhindert, dass dieselbe Nachricht mehrfach verarbeitet wird.






-----








{
  "name": "Nostr DM Listener (Debug Echo)",
  "nodes": [
    {
      "parameters": {
        "content": "## Setup\n1. **Community Node installieren** (self-hosted n8n): Settings → Community Nodes → `n8n-nodes-nostrobots`\n2. **Credential anlegen**: Node-Typ \"Nostrobots API\" → dein privater Nostr-Key (nsec oder hex). Das ist gleichzeitig \"das hinterlegte Konto\".\n3. In allen Nostr-Nodes die Credential auswählen und Relays/PublicKey (deinen eigenen npub) eintragen.\n4. Workflow aktivieren.\n\n**Zwei unabhängige Pfade in diesem Workflow:**\n- **Oben**: Private DMs (Polling alle 2 Min., NIP-04) → Antwort als DM\n- **Unten**: Öffentliche Erwähnungen (Echtzeit-Trigger) → Antwort als DM (nicht öffentlich!)\n\n⚠️ NIP-04 ist veraltet und leakt Metadaten (Absender/Empfänger öffentlich sichtbar, nur Inhalt verschlüsselt). Der Echtzeit-Trigger ist laut Hersteller BETA/experimentell.",
        "height": 460,
        "width": 480
      },
      "id": "sticky-1",
      "name": "Setup-Hinweise",
      "type": "n8n-nodes-base.stickyNote",
      "typeVersion": 1,
      "position": [-200, -320]
    },
    {
      "parameters": {
        "rule": {
          "interval": [
            {
              "field": "minutes",
              "minutesInterval": 2
            }
          ]
        }
      },
      "id": "schedule-1",
      "name": "Alle 2 Minuten",
      "type": "n8n-nodes-base.scheduleTrigger",
      "typeVersion": 1.2,
      "position": [-200, 0]
    },
    {
      "parameters": {
        "strategy": "nip-04",
        "relative": true,
        "from": 10,
        "unit": "minute",
        "relay": "wss://relay.damus.io,wss://nos.lol,wss://relay.nostr.band",
        "errorWithEmptyResult": false
      },
      "id": "nostrread-1",
      "name": "DMs abrufen (Nostr Read)",
      "type": "n8n-nodes-nostrobots.nostrobotsread",
      "typeVersion": 1,
      "position": [40, 0],
      "credentials": {
        "nostrobotsApi": {
          "id": "PLACEHOLDER",
          "name": "Nostr Account"
        }
      }
    },
    {
      "parameters": {
        "jsCode": "// Nur neue, erfolgreich entschlüsselte Nachrichten durchlassen (Dedupe über Workflow Static Data)\nconst staticData = $getWorkflowStaticData('global');\nif (!staticData.processedIds) staticData.processedIds = [];\n\nconst output = [];\n\nfor (const item of $input.all()) {\n  const evt = item.json;\n\n  // fehlgeschlagene Entschlüsselung überspringen\n  if (evt.decrypted === false) continue;\n\n  // bereits verarbeitete Events überspringen\n  if (staticData.processedIds.includes(evt.id)) continue;\n\n  staticData.processedIds.push(evt.id);\n  output.push(item);\n}\n\n// Liste nicht unbegrenzt wachsen lassen\nif (staticData.processedIds.length > 1000) {\n  staticData.processedIds = staticData.processedIds.slice(-1000);\n}\n\nreturn output;"
      },
      "id": "code-1",
      "name": "Nur neue Nachrichten",
      "type": "n8n-nodes-base.code",
      "typeVersion": 2,
      "position": [280, 0]
    },
    {
      "parameters": {},
      "id": "noop-1",
      "name": "👉 HIER: Deine Logik einfügen",
      "type": "n8n-nodes-base.noOp",
      "typeVersion": 1,
      "position": [520, 0],
      "notes": "Ersetze diesen Node durch das, was bei einer neuen Nachricht passieren soll (z.B. IF-Verzweigung, HTTP-Request, KI-Antwort generieren, Slack-Benachrichtigung, etc.). $json.content = entschlüsselter Text, $json.pubkey = Absender."
    },
    {
      "parameters": {
        "resource": "nip-04",
        "operation": "send",
        "content": "=Echo (Debug): {{ $json.content }}",
        "sendTo": "={{ $json.pubkey }}",
        "relay": "wss://relay.damus.io,wss://nos.lol,wss://relay.nostr.band"
      },
      "id": "nostrwrite-1",
      "name": "Debug: Nostr Antwort senden",
      "type": "n8n-nodes-nostrobots.nostrobots",
      "typeVersion": 1,
      "position": [760, 0],
      "credentials": {
        "nostrobotsApi": {
          "id": "PLACEHOLDER",
          "name": "Nostr Account"
        }
      }
    },
    {
      "parameters": {
        "strategy": "mention",
        "publickey": "npub1DEINEIGENESKONTOHIERxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
        "kind": 1,
        "threads": false,
        "relay1": "wss://relay.damus.io",
        "relay2": "wss://nos.lol",
        "ratelimitingCountForAll": 60,
        "ratelimitingCountForOne": 10,
        "period": 60,
        "duration": 180,
        "blackList": "",
        "whiteList": ""
      },
      "id": "trigger-mention-1",
      "name": "Erwähnung erkannt (Nostr Trigger)",
      "type": "n8n-nodes-nostrobots.nostrobotsEventTrigger",
      "typeVersion": 1,
      "position": [-200, 320],
      "credentials": {
        "nostrobotsApi": {
          "id": "PLACEHOLDER",
          "name": "Nostr Account"
        }
      }
    },
    {
      "parameters": {},
      "id": "noop-2",
      "name": "👉 HIER: Deine Logik einfügen (Mention)",
      "type": "n8n-nodes-base.noOp",
      "typeVersion": 1,
      "position": [40, 320],
      "notes": "Ersetze diesen Node durch das, was bei einer öffentlichen Erwähnung passieren soll. $json.content = Text des Posts, $json.pubkey = Autor, $json.id = Event-ID des Posts."
    },
    {
      "parameters": {
        "resource": "nip-04",
        "operation": "send",
        "content": "=Danke für deine Erwähnung! (Debug-Echo): {{ $json.content }}",
        "sendTo": "={{ $json.pubkey }}",
        "relay": "wss://relay.damus.io,wss://nos.lol,wss://relay.nostr.band"
      },
      "id": "nostrwrite-2",
      "name": "Debug: DM-Antwort auf Erwähnung",
      "type": "n8n-nodes-nostrobots.nostrobots",
      "typeVersion": 1,
      "position": [280, 320],
      "credentials": {
        "nostrobotsApi": {
          "id": "PLACEHOLDER",
          "name": "Nostr Account"
        }
      }
    }
  ],
  "connections": {
    "Alle 2 Minuten": {
      "main": [
        [
          {
            "node": "DMs abrufen (Nostr Read)",
            "type": "main",
            "index": 0
          }
        ]
      ]
    },
    "DMs abrufen (Nostr Read)": {
      "main": [
        [
          {
            "node": "Nur neue Nachrichten",
            "type": "main",
            "index": 0
          }
        ]
      ]
    },
    "Nur neue Nachrichten": {
      "main": [
        [
          {
            "node": "👉 HIER: Deine Logik einfügen",
            "type": "main",
            "index": 0
          }
        ]
      ]
    },
    "👉 HIER: Deine Logik einfügen": {
      "main": [
        [
          {
            "node": "Debug: Nostr Antwort senden",
            "type": "main",
            "index": 0
          }
        ]
      ]
    },
    "Erwähnung erkannt (Nostr Trigger)": {
      "main": [
        [
          {
            "node": "👉 HIER: Deine Logik einfügen (Mention)",
            "type": "main",
            "index": 0
          }
        ]
      ]
    },
    "👉 HIER: Deine Logik einfügen (Mention)": {
      "main": [
        [
          {
            "node": "Debug: DM-Antwort auf Erwähnung",
            "type": "main",
            "index": 0
          }
        ]
      ]
    }
  },
  "active": false,
  "settings": {
    "executionOrder": "v1"
  }
}
