# Helga create-identity — manual verification

Operator checklist after image rebuild + workflow import on `https://n8n.neberg.de`.

1. Set `NODE_FUNCTION_ALLOW_EXTERNAL=faker,@faker-js/faker,@noble/secp256k1,@noble/hashes,nostr-tools`
2. Attach GitHub credential; set Config `githubRepo`; replace `REPLACE_ME_HELGA_PREDICTION_URL`
3. Activate workflow; note Production webhook URL

```cmd
curl.exe -s -X POST https://n8n.neberg.de/webhook/helga-create-identity -H "Content-Type: application/json" -d "{\"action\":\"route_message\",\"target_agent\":\"@Helga\",\"intent\":\"hr_request\",\"payload\":{\"message\":\"Create Teamleiter Finanzen\",\"context\":\"\",\"module_scope\":\"Module.Finanzen\",\"role\":\"teamleiter\"}}"
```

```cmd
curl.exe -s -X POST https://convenient-nonie-neberg-ad5744ad.koyeb.app/webhook/helga-create-identity -H "Content-Type: application/json" -d "{\"action\":\"route_message\",\"target_agent\":\"@Helga\",\"intent\":\"hr_request\",\"payload\":{\"message\":\"Create Teamleiter Finanzen\",\"context\":\"\",\"module_scope\":\"Module.Finanzen\",\"role\":\"teamleiter\"}}"
```

Expect: `ok:true`, `already_exists:false`, `agent_id":"finanzen-teamleiter"`, `npub` present; GitHub file without `nsec`.

4. POST same payload again → `already_exists:true`
5. POST `role:researcher` → `finanzen-researcher.json`
6. Confirm kind-0 on `wss://nostr.neberg.de` for returned `npub`
