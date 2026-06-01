┌─────────────────────────────────────────────────────────────┐
│                     MESSAGE PRODUCER                        │
│                                                             │
│  M1: Debit $100                                             │
│  M2: Add Interest                                           │
│  M3: Close Account                                          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  AZURE SERVICE BUS QUEUE                    │
│                                                             │
│                 SessionId = Ledger-1                        │
│                                                             │
│  [ M1 ] ───► [ M2 ] ───► [ M3 ]                            │
└──────────────┬──────────────────────────────┬───────────────┘
               │                              │
               │ Session Lock                 │ Session Reacquired
               │                              │
               ▼                              ▼
┌─────────────────────────┐     ┌─────────────────────────┐
│        WORKER A         │     │        WORKER B         │
│                         │     │                         │
│ Acquires Session        │     │ Acquires Expired        │
│ Processes M1            │     │ Session                 │
│ Long API Call           │     │ Processes M2            │
│ DB Retry Loop           │     │ Processes M3            │
│ Lock Expires            │     │                         │
└─────────────┬───────────┘     └─────────────┬───────────┘
              │                               │
              └──────────────┬────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                       LEDGER STORE                          │
│                                                             │
│  Balance                                                    │
│  Account Status                                             │
│                                                             │
│  Final State = Corrupted                                    │
└─────────────────────────────────────────────────────────────┘