Community Node installieren: In deinem self-hosted n8n unter Settings → Community Nodes das Paket n8n-nodes-nostrobots installieren.
Credential anlegen: Typ "Nostrobots API" → deinen privaten Nostr-Key (nsec oder hex) eintragen. Das ist das "hinterlegte Konto".
Workflow importieren (... → Import from File), Credential in beiden Nostr-Nodes auswählen.
Im "👉 HIER: Deine Logik einfügen"-Node später ersetzen, was wirklich passieren soll.
Aktivieren – läuft dann alle 2 Minuten, holt neue DMs, entschlüsselt sie automatisch (NIP-04) und schickt testweise ein Echo zurück.

Wichtig: Kein echtes Push/Realtime, sondern Polling alle 2 Minuten (Nostr-DMs lassen sich mit dieser Node nur so zuverlässig abgreifen). Die Dedupe-Logik im Code-Node verhindert, dass dieselbe Nachricht mehrfach verarbeitet wird.
