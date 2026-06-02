# 💥 Azure Service Bus Idempotency Failure Demo (Azure Functions .NET 8 Isolated)

This repository demonstrates a real-world distributed systems failure scenario where a customer is charged twice due to misunderstanding Azure Service Bus Duplicate Detection as idempotency.

---

# 🚨 Problem Statement

Azure Service Bus provides **Duplicate Detection Window**, but this is NOT idempotency.

When a function:

- Executes a side effect (payment)
- Crashes before checkpointing state
- And message is retried after duplicate window expiry

👉 The system executes the operation twice.

---

# 📁 Project Structure

```
AsbIdempotencyDemo/

│
├── AsbIdempotencyDemo.csproj
├── Program.cs
├── host.json
├── local.settings.json
│
├── Models/
│   └── OrderMessage.cs
│
├── Services/
│   ├── PaymentGateway.cs
│   ├── FakeSqlDatabase.cs
│   ├── IdempotencyStore.cs
│
├── Functions/
│   └── OrderProcessorFunction.cs
│
└── README.md
```

---

# 💣 Business Impact

| Expected | Actual |
|----------|--------|
| $500 charge | $1000 charge ❌ |

---

# 🧠 System Architecture

```mermaid
flowchart LR
Producer --> ServiceBusQueue --> AzureFunction
AzureFunction --> PaymentGateway
AzureFunction --> AzureSQL
AzureSQL -. failure .-> AzureFunction
AzureFunction -. crash before checkpoint .-> ServiceBusQueue
ServiceBusQueue -. retry after duplicate window expiry .-> AzureFunction
```

---

# ⏱️ Failure Timeline

```mermaid
sequenceDiagram
participant P as Producer
participant SB as Service Bus
participant F as Azure Function
participant PG as Payment Gateway
participant DB as Azure SQL

P->>SB: Send ORDER-84721
SB->>F: Deliver message

F->>PG: Charge $500
PG-->>F: Success

F->>DB: Save order
DB-->>F: ❌ Connection Pool Exhausted

Note over F: Function crashes before checkpoint

Note over SB: Duplicate Detection Window = 10 min

Note over SB: Message expires from cache

P->>SB: Retry ORDER-84721 (after 15 min)

SB->>F: Treated as NEW message

F->>PG: Charge $500 again 💥
PG-->>F: Success

F->>DB: Save order
DB-->>F: Success
```

---

# 🔥 Root Cause Analysis

## 1. Duplicate Detection ≠ Idempotency

Service Bus only prevents duplicate delivery within a **time window**.

It does NOT guarantee:

- Exactly-once processing
- Business-level correctness
- Cross-system transactional safety

---

## 2. Side Effects Before Persistence

The function performs:

1. Payment (external side effect)
2. Database write (fails)

If the function crashes between these steps:

👉 State is lost, but side effect remains.

---

## 3. Missing Idempotency Layer

No durable mechanism exists to detect:

- "Has this order already been processed?"

---

# 🧪 What This Demo Simulates

- Azure Service Bus Trigger Function
- Payment Gateway side effect
- Azure SQL failure simulation
- Function crash before checkpoint
- Retry after delay
- Duplicate detection expiry
- Double execution of payment

---

# 🧱 Architecture Overview

```mermaid
flowchart TD

SB[Service Bus Queue] --> FN[Azure Function]

FN --> PAY[Payment Gateway]
FN --> DB[Azure SQL Database]

DB -. failure .-> FN
FN -. retry .-> SB
```

---

# 🧠 Key Insight

> Duplicate Detection is a broker-level memory feature — not a consistency guarantee.

---

# 💥 Correct vs Incorrect Mental Model

## ❌ Incorrect

Service Bus ensures:
- No duplicates
- Exactly-once processing

## ✅ Correct

Service Bus ensures:
- At-least-once delivery
- Possible duplicates after failure + retry
- Application must enforce idempotency

---

# 🧱 Fix Pattern (Conceptual)

```mermaid
flowchart LR

Message --> Function --> IdempotencyCheck

IdempotencyCheck -->|Exists| Ignore
IdempotencyCheck -->|Not Exists| WriteDBFirst

WriteDBFirst --> Payment
Payment --> MarkComplete
```

---

# 🏃 Running the Project

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4

---

## Run locally

```bash
func start
```

---

# 📩 Sample Message

```json
{
  "orderId": "ORDER-84721",
  "amount": 500
}
```

---

# 💥 Failure Output

```
First execution:
✔ Payment Success
❌ DB Failure
💥 Function Crash

Retry execution:
✔ Payment Success AGAIN
✔ DB Success

RESULT: DOUBLE CHARGE
```

---

# 🎯 Key Takeaway

If your system:

- Performs side effects
- Before durable state is written
- And relies on retry semantics

👉 You WILL eventually get duplicates in production.

---

# 🧩 Related Patterns

- Idempotency Key Store
- Outbox Pattern
- Poison Message Handling
- DLQ Replay Strategy
- Exactly-once illusion mitigation

---

# 📌 Final Insight

Service Bus guarantees delivery.

It does NOT guarantee correctness.

Correctness is an application-level responsibility.