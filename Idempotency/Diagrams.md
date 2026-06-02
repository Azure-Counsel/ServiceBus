# 🔥 Idempotency & Duplicate Detection in Distributed Systems

A deep dive into **Service Bus duplicate detection**, **idempotency failures**, and **correct architectural patterns** for building financially correct distributed systems.

---

# ❌ THE DUPLICATE DETECTION ILLUSION (Time-Bound Window)

> Service Bus Duplicate Detection is NOT idempotency. It is a **temporary broker-side cache optimization**.

## Configuration
- DuplicateDetectionWindow = 10 Minutes

```mermaid
sequenceDiagram
    participant P as Producer
    participant SB as Service Bus Broker
    participant C as Consumer
    participant DB as Database

    P->>SB: Message ORD-100 (09:00)
    SB->>SB: Cache MessageId (ORD-100)
    SB->>C: Deliver Message
    C->>DB: Insert Record ✅ SUCCESS

    Note over SB: Duplicate within 10 min window
    P->>SB: Replay ORD-100 (09:05)
    SB-->>C: Blocked (duplicate detected)

    Note over SB: Cache expires after 10 minutes

    P->>SB: Replay ORD-100 (next day)
    SB->>C: Treated as NEW message
    C->>DB: Duplicate write 💥

    💡 Key Insight
    Broker deduplication is temporary memory
    It is NOT a correctness guarantee

    💡 Key Insight
    Broker deduplication is temporary memory
    It is NOT a correctness guarantee

    ```mermaid
    sequenceDiagram
    participant P as Producer
    participant SB as Service Bus
    participant C as Consumer
    participant Pay as Payment API
    participant DB as Azure SQL

    P->>SB: ORDER-84721
    SB->>C: Deliver message

    C->>Pay: Charge $500
    Pay-->>C: SUCCESS

    C->>DB: Write transaction ❌ TIMEOUT
    Note over C: Consumer crashes before ACK

    Note over SB: Duplicate window expires

    P->>SB: Retry ORDER-84721
    SB->>C: Delivered again

    C->>Pay: Charge $500 AGAIN 💥
    Pay-->>C: SUCCESS

    C->>DB: Write transaction SUCCESS

    ❗ Outcome
    Expected: $500
    Actual: $1000 ❌

    ```mermaid
    sequenceDiagram
    participant C as Consumer
    participant DB as SQL Database
    participant API as Payment API

    C->>DB: Check Idempotency Key
    DB-->>C: NOT FOUND

    C->>API: Charge $500
    API-->>C: SUCCESS

    C->>DB: BEGIN TRANSACTION
    C->>DB: Insert Payment + Idempotency Key
    DB-->>C: COMMIT

    Note over C,DB: Replay arrives later

    C->>DB: Check Idempotency Key
    DB-->>C: FOUND (Completed)

    C-->>C: Skip execution (return cached result)

    ✔ Guarantee
    1 message → 1 execution → 1 financial outcome

    ❌ REDIS IDEMPOTENCY PITFALL
    ```mermaid
    flowchart TD

    A[🌊 Service Bus<br/>Deliver Message: Debit $500<br/>Id: ORDER-999] --> B[⚙️ Consumer Worker]

    B --> C[1. Check Idempotency Barrier]

    C --> D[⚡ Redis Memory Cache]

    D --> E{Redis Lookup Result}

    E -->|Hit| F[🛑 Safe: Skip Execution]

    E -->|Miss| G[❓ Key Not Found<br/>Memory is blank]

    E -->|Failure Modes| H[💥 Redis Volatility Issues]

    H --> H1[Key Expired<br/>TTL too short]
    H --> H2[Cache Eviction<br/>Memory pressure]
    H --> H3[Replica Lag<br/>Replication delay]
    H --> H4[Unpersisted Data<br/>Node restart before snapshot]

    G --> I[2. Execute Business Operation]

    I --> J[💳 Stripe Charge $500]

    J --> K[💥 DOUBLE CHARGE OCCURS]

    ❗ Problem
    Redis is:

    volatile
    eviction-based
    eventually consistent

    👉 NOT a correctness boundary

    🧠 HYBRID ARCHITECTURE (CORRECT DESIGN)
    ```mermaid
    flowchart LR
    A[Service Bus] --> B[Worker Function]

    B --> C[Redis Cache Lookup]

    C -->|Hit| D[Fast Path Return]

    C -->|Miss| E[SQL Idempotency Table]

    E -->|Exists| F[Abort Execution]
    E -->|Not Found| G[Execute Business Logic]

    G --> H[External API Call]
    G --> I[Write SQL + Idempotency Key]

    I --> C

    

    ```mermaid
    flowchart TB

%% =========================
%% SCALE-DRIVEN DECISION MATRIX
%% =========================

A[📈 Scale-Driven Idempotency Decision Matrix]

A --> L
A --> M
A --> H

%% =========================
%% LOW SCALE
%% =========================
subgraph L[Low Scale ~10 req/sec]
direction TB

L1[⚙️ Worker]
L2[🗄️ SQL-Only Table]

L1 --> L2

L3[🎯 Simplicity Wins]
L4[• Single DB transaction<br/>• No extra infrastructure<br/>• Least moving parts]

L2 --> L3
L3 --> L4

end

%% =========================
%% MEDIUM SCALE
%% =========================
subgraph M[Medium Scale ~100 req/sec]
direction TB

M1[⚙️ Worker]
M2[⚡ Redis Cache]
M3[🗄️ SQL DB]

M1 --> M2
M1 --> M3

M4[🎯 Reduce Database Reads]
M5[• Redis absorbs lookup traffic<br/>• Protects DB CPU<br/>• SQL fallback on eviction]

M2 --> M4
M3 --> M4
M4 --> M5

end

%% =========================
%% HYPER SCALE
%% =========================
subgraph H[Hyper Scale 1000s+ req/sec]
direction TB

H1[⚙️ Worker]
H2[📥 Inbox / Ledger<br/>(Atomic Append-Only)]

H1 --> H2

H3[🎯 Concurrency Safety & Speed]
H4[• No read-before-write checks<br/>• Atomic append-only writes<br/>• Minimal lock contention<br/>• High concurrency safety]

H2 --> H3
H3 --> H4

end