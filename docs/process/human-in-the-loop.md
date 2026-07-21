Hier ist die schematische Darstellung für den **Human-in-the-Loop (HITL) Freigabe-Flow**.

Dieses Diagramm visualisiert den Weg vom Agenten-Code über die `n8n`-Warteschlange bis zu deiner Entscheidung und dem anschließenden Deployment. Du kannst dieses Diagramm direkt in deine Architektur-Blaupause (z. B. unter einem neuen Unterpunkt "3.3 Human-in-the-Loop & Approval Flow") einfügen.

```text
               ┌─────────────────────────────────────────┐
               │ 1. ENTWICKLUNG & LOKALER REVIEW         │
               └─────────────────────────────────────────┘
                 ┌────────────────┐       (Code/Mockup) 
                 │ Kinder-Agent   │ ──────────────────► 
                 │(Frontend/React)│ ◄────────────────── 
                 └────────────────┘   (Korrekturschleife)

                                            ▼
                                  ┌──────────────────┐
                                  │   Teamleiter     │ (Domain Supervisor)
                                  │   (z.B. D&D)     │
                                  └────────┬─────────┘
                                           │
         (2. Sende JSON Intent: `user_approval_required`)
                                           │
               ┌───────────────────────────▼─────────────────────────────┐
               │ 3. EVENT ROUTING & PAUSE (n8n)                          │
               │ n8n fängt Webhook ab, speichert Kontext im "Wait Node"  │
               │ und leitet das Mockup an das User Interface weiter.     │
               └───────────────────────────┬─────────────────────────────┘
                                           │
                                           ▼
               ┌─────────────────────────────────────────┐
               │ 4. USER INTERVENTION (React UI / Nostr) │
               │ Zeigt interaktive Karte mit Code/Design │
               └───────────────────┬─────────────────────┘
                                   │
                           [ DEINE ENTSCHEIDUNG ]
                                   │
               ┌───────────────────┴───────────────────┐
               │                                       │
     [ ÄNDERUNG VERLANGEN ]                     [ FREIGEBEN (Merge) ]
   (z.B. "Mach Button grün")                           │
               │                                       │
               ▼                                       ▼
     (n8n löst Pause auf &)                  (n8n löst Pause auf &)
     (routet zurück zu 1. )                  (routet zu GitHub   )
                                                       │
                                                       ▼
                                             ┌──────────────────┐
                                             │  GitHub Commit   │
                                             │ (Main Branch)    │
                                             └────────┬─────────┘
                                                      │
                                          (Triggert autom. Build)
                                                      │
                                                      ▼
                                             ┌──────────────────┐
                                             │ Koyeb Deployment │
                                             │  (Multi-Stage)   │
                                             └──────────────────┘

```

### Erklärung der Phasen für deine Dokumentation:

* **Phase 1 & 2 (Die Vorarbeit):** Die Agenten arbeiten isoliert in ihrem Modul. Der Teamleiter erkennt durch seinen System-Prompt, dass er eine UI-Änderung nicht eigenmächtig pushen darf. Er bündelt den Entwurf und schickt ihn als Freigabe-Anfrage an `n8n`.


* **Phase 3 (Der Türsteher):** `n8n` pausiert den Workflow dieses Agenten-Teams (via "Wait Node"), hält aber die restliche Infrastruktur am Laufen. Das Event wird als formatierte Nachricht an dein Interface (Nostr oder das native KI-Modul) gepusht.


* **Phase 4 (Der God-Mode):** Du betrachtest das Mockup. Lehnst du ab, geht der Flow mit deinem kritischen Feedback in die Korrekturschleife zurück an den Teamleiter.
* **Phase 5 (Produktion):** Stimmst du zu, wird der Code ins Monorepo committet, was sofort den Multi-Stage Docker-Build auf Koyeb anstößt, ohne dass du jemals eine Kommandozeile berühren musstest.