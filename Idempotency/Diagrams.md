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
    C->>DB: Insert Record SUCCESS

    Note over SB: Duplicate within 10 min window

    P->>SB: Replay ORD-100 (09:05)
    SB-->>C: Blocked (duplicate detected)

    Note over SB: Cache expires after 10 minutes

    P->>SB: Replay ORD-100 (next day)
    SB->>C: Treated as NEW message
    C->>DB: Duplicate write 💥
```
> 💡 Broker deduplication is temporary memory — NOT a correctness guarantee.
---
# 💥 FINANCIAL FAILURE MODE (REPLAY + CRASH)
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

    C->>DB: Write transaction TIMEOUT
    Note over C: Consumer crashes before ACK

    P->>SB: Retry ORDER-84721
    SB->>C: Delivered again

    C->>Pay: Charge $500 AGAIN 💥
    Pay-->>C: SUCCESS

    C->>DB: Write transaction SUCCESS
```
❗ Outcome:

Expected: $500
Actual: $1000 ❌

---
# 🧠 IDEMPOTENCY KEY PATTERN (CORRECT MODEL)
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
    DB-->>C: FOUND

    C-->>C: Skip execution (return cached result)
```
✔ Guarantee:
1 message → 1 execution → 1 financial outcome

---

# ❌ REDIS IDEMPOTENCY PITFALL
```mermaid
flowchart TD
    A[Service Bus: Debit $500<br/>Id: ORDER-999] --> B[Consumer Worker]

    B --> C[Check Idempotency Barrier]
    C --> D[Redis Cache]

    D --> E{Lookup Result}

    E -->|Hit| F[Skip Execution]
    E -->|Miss| G[Execute Business Logic]

    E -->|Failure Modes| H[Redis Volatility]

    H --> H1[TTL Expiry]
    H --> H2[Eviction]
    H --> H3[Replica Lag]
    H --> H4[Node Restart]

    G --> I[Stripe Charge $500]
    I --> J[Double Charge Risk]
```
❗ Redis is:

* volatile
* eviction-based
* eventually consistent
* 👉 NOT a correctness boundary

---
# 🧠 HYBRID ARCHITECTURE (CORRECT DESIGN)
```mermaid
flowchart LR
    A[Service Bus] --> B[Worker Function]

    B --> C[Redis Lookup]

    C -->|Hit| D[Fast Path Return]
    C -->|Miss| E[SQL Idempotency Table]

    E -->|Exists| F[Abort Execution]
    E -->|Not Found| G[Execute Business Logic]

    G --> H[External API Call]
    G --> I[Write SQL + Idempotency Key]

    I --> C
```
---

# 📈 SCALE-DRIVEN IDEMPOTENCY DECISION MATRIX
```mermaid
flowchart TB

A[Scale-Driven Idempotency Decision Matrix]

A --> L
A --> M
A --> H

subgraph L[Low Scale ~10 req/sec]
    L1[Worker] --> L2[SQL-Only Table]
    L2 --> L3[Simplicity Wins]
end

subgraph M[Medium Scale ~100 req/sec]
    M1[Worker]
    M2[Redis Cache]
    M3[SQL Database]

    M1 --> M2
    M1 --> M3
    M2 --> M4[Reduce DB Reads]
    M3 --> M4
end

subgraph H[High Scale 1000s+ req/sec]
    H1[Worker] --> H2[Inbox / Ledger]
    H2 --> H3[Atomic Append-Only Writes]
    H3 --> H4[Concurrency Safe Design]
end
```
---
# ✔ FINAL TAKEAWAYS

* Service Bus deduplication ≠ idempotency
* Redis ≠ correctness boundary
* SQL ledger = source of truth
* Inbox pattern = scalable financial safety
