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

💥 FINANCIAL FAILURE MODE (REPLAY + CRASH)
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

🧠 IDEMPOTENCY KEY PATTERN (CORRECT MODEL)
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
