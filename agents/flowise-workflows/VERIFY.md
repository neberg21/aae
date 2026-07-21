# Helga Flowise — operator verify

1. Import [`helga.json`](helga.json) into https://flowise.neberg.de (Agentflow V2).
2. Confirm canvas: Start → Helga; system message is `hol das aktuelle prompt aus dem repo`; model OpenAI; tools empty.
3. Attach OpenAI credential; paste [`../identities/helga.md`](../identities/helga.md); save.
4. Copy prediction URL into n8n `REPLACE_ME_HELGA_PREDICTION_URL`.
5. Optional: send a short HR-style chat/prediction and check JSON-shaped reply (schema validation stays in n8n).
