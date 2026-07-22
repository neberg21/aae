## 1. Die C# .NET Backend Endpunkte (Zentrale & Router)

Das Backend ist das Herzstück. Es speichert den State, orchestriert die Nebenläufigkeit (Parallelität) und reicht Status-Updates an die WebApp durch.

| HTTP Methode & Route | Sender | Beschreibung & Hauptaufgabe |
| --- | --- | --- |
| **POST** `/api/agents/route-chat-message` | n8n & UI | **Der Multi-Router:** Erwartet jetzt ein *Array* von Nachrichten. <br>

<br>• **Target = Agent_X:** Weckt asynchron den n8n-Webhook von Agent_X (Fan-Out). <br>

<br>• **Target = null:** Speichert die Nachricht (FYI/Status-Update) und pusht sie in die UI, weckt aber *keinen* neuen Workflow. |
| **POST** `/api/agents/create-identity` | n8n (Helga) | **Der Krypto-Worker:** Übernimmt Helgas JSON. Generiert Nostr-Keys, speichert das Organigramm (`ManagerId`) und die spezifischen Fähigkeiten (`AllowedTools`), liefert ID zurück. |
| **POST** `/api/await-request-approval` | n8n (Alle) | **Das Approval Gate:** Pausiert den KI-Flow. Pusht Entwürfe (Mockups, Architektur) an die WebApp und wartet auf deine Freigabe. |
| **POST** `/api/resolve-request-approval` | UI | **Der Türöffner:** Triggert die Weiterführung im System, sobald du in der UI auf "Freigeben" (oder "Ablehnen" mit Feedback) klickst. |

---

## 2. Die n8n Workflows & Webhooks (Ausführungsschicht)

Du benötigst exakt **vier Workflows**. Sie arbeiten zustandslos (stateless) und haben keine Wait-Nodes. Das Backend versorgt sie beim Wecken mit allem, was sie wissen müssen (Kontext-Injektion).

### A. Orchestrator-Workflow (CEO)

Plant die übergeordnete Vision und verteilt große Brocken an Fachabteilungen.

* **Trigger-Webhook:** `POST /webhook/ceo-think`
* **Input-Payload:** `ThreadId`, `ChatHistory`, `UserVision`
* **Knoten-Ablauf:**
1. **Webhook:** Empfängt Vision und Historie.
2. **AI Agent:** Zerkleinert die Vision in Features.
3. **Code Node:** Erstellt ein JSON-Array mit Teilaufgaben für verschiedene Teamleiter.
4. **HTTP Request:** POST an `/api/agents/route-chat-message` (übermittelt das Array, um Teamleiter parallel zu wecken).

### B. Helga-Workflow (HR & Identitäts-Schmiede)

Erstellt neue "Software-Seelen" und definiert, was Spezialisten dürfen.

* **Trigger-Webhook:** `POST /webhook/helga-think`
* **Input-Payload:** `ThreadId`, `ChatHistory`, `DelegationRequest`
* **Knoten-Ablauf:**
1. **Webhook:** Empfängt die Anforderung (z. B. "Brauche React-Dev").
2. **AI Agent:** Generiert System-Prompt, wählt `AllowedTools` aus und bestimmt die `ManagerId` (Wer ist der Chef?).
3. **Switch Node:**
* *Pfad 1 (Rückfrage):* POST an `/api/agents/route-chat-message` (Target: User).
* *Pfad 2 (Fertig):* POST an `/api/agents/create-identity` (Backend baut Keys und speichert).

### C. Supervisor-Workflow (Teamleiter – Rekursiv)

Das Gehirn der mittleren Ebene. Kann parallel delegieren (Fan-Out) und auf Ergebnisse warten (Fan-In).

* **Trigger-Webhook:** `POST /webhook/supervisor-think`
* **Input-Payload:** `ThreadId`, `ChatHistory`, `TaskContext`, **`SubordinatesList`** (Wer arbeitet für ihn?).
* **Knoten-Ablauf:**
1. **Webhook:** Empfängt Task und sein spezifisches Organigramm.
2. **AI Agent:** Analysiert Chat-Historie.
3. **Switch Node (Fan-In vs. Fan-Out):**
* *Sind noch Tasks von Spezialisten offen?* -> POST `/api/agents/route-chat-message` (Target: `null`, Content: "Warte noch auf Modul X"). Workflow endet passiv.
* *Muss neue Arbeit verteilt werden?* -> POST `/api/agents/route-chat-message` (Target: Array aus untergeordneten Supervisoren oder Spezialisten = **Fan-Out**).
* *Ist alles fertig?* -> POST `/api/await-request-approval` (zum User) oder zum übergeordneten Manager.

### D. Specialist-Workflow (Tool-Agnostisch)

Die ausführenden Arbeiter. Ein einziger Workflow, der durch das Backend in jede beliebige Rolle gezwungen wird.

* **Trigger-Webhook:** `POST /webhook/specialist-think`
* **Input-Payload:** `ThreadId`, `ChatHistory`, `TaskContext`, **`AllowedTools`** (z. B. `["GenerateCode", "CreateImage"]`).
* **Knoten-Ablauf:**
1. **Webhook:** Empfängt Aufgabe und Werkzeug-Freigabe.
2. **AI Agent:** Führt die Arbeit über dynamische n8n-Tools aus (darf *nur* die Tools nutzen, die im Payload freigegeben wurden).
3. **HTTP Request (Optional):** Während der Ausführung POST an `/api/agents/route-chat-message` (Target: `null`, Content: "Kompiliere gerade...").
4. **HTTP Request (Fertig):** POST an `/api/agents/route-chat-message` (Target: Eigener `ManagerId`, Content: "Task erledigt, hier ist das Ergebnis").


```md
Rolle: Du bist ein Senior Systemarchitekt und n8n-Experte.


Aufgabe: Erstelle für mein "Autonomous Agent Ecosystem" (AAE)  vier komplett funktionsfähige, importierbare n8n-Workflows im JSON-Format.

Architektur-Prinzipien (CRITICAL):

Zustandslosigkeit: n8n ist rein für die Ausführung zuständig. Das "Gehirn", State-Management und die Chat-Historie liegen komplett in einem separaten .NET Backend.
2. Keine Wait-Nodes: Es gibt absolut keine langlaufenden Workflows oder Wait-Nodes. Jeder Workflow ist asynchron (Fire-and-Forget): Er startet per Webhook, macht seinen Job und beendet sich sofort mit einem HTTP POST Request an das Backend.
3. AI Nodes: Nutze für die Agenten-Logik die "Advanced AI" Nodes von n8n (basierend auf LangChain).

Spezifikation der 4 benötigten Workflows:

1. Leo-Workflow (CEO, Orchestrator)
* Trigger: Webhook (POST /webhook/ceo-think) empfängt ThreadId, ChatHistory und UserVision.
* Logic: AI Agent zerkleinert die Vision in Features. Ein Code-Node formatiert ein JSON-Array mit Teilaufgaben für Teamleiter.
* Output: HTTP Request (POST an /api/agents/route-chat-message) mit dem Array, um Teamleiter parallel zu wecken.

2. Helga-Workflow (HR & Identitäts-Schmiede)
* Trigger: Webhook (POST /webhook/helga-think) empfängt Anforderung.
* Logic: AI Agent baut ein Agenten-Profil (Tools, Guardrails, ManagerId). Ein Switch-Node prüft den Status.
* Output: >     * Pfad A (Rückfrage): HTTP Request (POST an /api/agents/route-chat-message, Target: User).
* Pfad B (Fertig): HTTP Request (POST an /api/agents-create-identity).

3. Supervisor-Workflow (Teamleiter - Scrum Master)
* Trigger: Webhook (POST /webhook/supervisor-think) empfängt Task und sein spezifisches Organigramm.
* Tools: Hat Zugriff auf GitHub-Tools (create_github_issue, update_issue_status, add_issue_comment), um Meilensteine als "State Machine" in GitHub zu dokumentieren.

Logic: AI Agent plant und delegiert. Switch-Node für Fan-In/Fan-Out.
* Output: >     * Offene Tasks: HTTP Request (POST an /api/agents/route-chat-message, Target: null für UI-Status).
* Neue Delegation: HTTP Request (POST an /api/agents/route-chat-message, Target: Spezialisten).
* Fertig: HTTP Request (POST an /api/await-request-approval für das Approval Gate).

4. Specialist-Workflow (Tool-Agnostischer Worker)
* Trigger: Webhook (POST /webhook/specialist-think) empfängt Aufgabe und das Array AllowedTools.
* Logic: AI Agent führt die Arbeit mit dynamischen Tools in strikter Isolation aus. Ist nicht hardcodiert auf das Dateisystem.
* Output: HTTP Request (POST an /api/agents/route-chat-message an seine eigene ManagerId).

Ausgabe-Format:
Generiere ausschließlich die reinen, validen JSON-Codeblöcke für diese 4 n8n-Workflows, sodass ich sie direkt importieren kann.
```