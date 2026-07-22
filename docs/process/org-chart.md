```text
[ USER ]
   │ (Rough vision: "D&D stories")
   ▼
[ LEO (ORCHESTRATOR) ]  ──────────┐
   │ (Assigns work)               │ (Requests new identities when needed)
   ▼                              ▼
[ DOMAIN SUPERVISOR ]        [ HELGA (HR) ]
   │ (Plans architecture,         │ (Writes identities via create-identity)
   │  may spawn nested            │
   │  supervisors, requests        │
   │  specialists, reviews work)  │
   │                              │
   ├──► [ Nested supervisor ] ──► specialists under that scope
   │
   ├──► [ Backend specialist ] (Only inside backend/src/Module.[Name]/)
   │
   └──► [ Frontend specialist ] (Only inside frontend/src/modules/[name]/)
```

Hierarchy is recursive: a supervisor’s `subordinatesList` may include other supervisors as well as specialists. Hiring still goes through Helga (`managerId` = parent supervisor or `leo`).

Modules that exist in the repo today: `Module.Demo`, `Module.Agents`. Domain modules such as `Module.Finance` appear when agents add them under the isolation rules above.
