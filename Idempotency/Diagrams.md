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
