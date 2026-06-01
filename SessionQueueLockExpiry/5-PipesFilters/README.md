# ⚙️ Azure Service Bus Pipes & Filters Pattern (Queue Chaining with Azure Functions)

> A production-style architecture pattern to solve long-running Service Bus processing limitations using queue-based workflow decomposition.

---

# What Problem Are We Solving?

Many systems start with a single Azure Function triggered by Service Bus:

```
Service Bus Message
        │
        ▼
Azure Function
        │
        ├── Call External API (12 sec)
        ├── Execute Business Logic
        ├── Write to Database (28 sec, retry-heavy)
        │
        ▼
Complete Message
```

At first, this looks simple.

But in real production systems, this design breaks down:

---

## ❌ Core Issues

- Service Bus lock expiration during long execution
- Duplicate processing due to retries
- API calls executed multiple times unintentionally
- DB throttling blocks entire workflow
- No independent scaling per step
- Tight coupling of all processing stages

Even increasing:

```
MaxAutoLockRenewalDuration
```

only delays failure — it does not eliminate it.

---

# The Architectural Shift

Instead of executing everything inside one function:

```
Receive Message
      │
      ▼
Do All Work Here ❌
```

We split the workflow into independent stages:

```
Queue A (Orders)
      │
      ▼
Function A (API Step)
      │
      ▼
Queue B (Commits)
      │
      ▼
Function B (DB Step)
      │
      ▼
Database
```

---

# High-Level Architecture

```
┌────────────────────────┐
│ Azure Service Bus      │
│ Queue A: Orders        │
└──────────┬─────────────┘
           │
           ▼
┌────────────────────────┐
│ Function A             │
│ API Execution Step     │
└──────────┬─────────────┘
           │ Forward Message
           ▼
┌────────────────────────┐
│ Azure Service Bus      │
│ Queue B: Commits       │
└──────────┬─────────────┘
           │
           ▼
┌────────────────────────┐
│ Function B             │
│ Database Step          │
└──────────┬─────────────┘
           │
           ▼
        Database
```

---

# Project Structure

```
PipesAndFiltersFunctionApp
│
├── Functions
│   ├── FunctionA_ApiStep.cs
│   └── FunctionB_DbStep.cs
│
├── Models
│   └── OrderMessage.cs
│
├── Services
│   └── FakeApiClient.cs
│
├── Program.cs
├── host.json
├── local.settings.json
├── PipesAndFiltersFunctionApp.csproj
└── README.md
```

---

# Understanding Each Component

---

## Queue A (Orders)

Entry point of the system.

Responsibilities:

```
Receive order message
Trigger Function A
Maintain ordering via SessionId
```

---

## Function A — API Step

Responsibilities:

```
Consume message
Call external API (~12 sec)
Create continuation message
Send to Queue B
Complete original message immediately
```

What it MUST NOT do:

```
Do not perform DB writes
Do not handle long retry logic
Do not execute multi-step workflows
```

Analogy:

```
Factory Assembly Line - Stage 1
```

---

## Queue B (Commits)

Purpose:

- Isolate downstream processing
- Allow longer processing time
- Decouple from API latency

---

## Function B — DB Step

Responsibilities:

```
Consume continuation message
Perform DB write (~28 sec)
Complete processing
```

---

## SessionId Strategy (Critical)

To maintain order per transaction:

```
SessionId = OrderId
```

Ensures:

- FIFO per order
- Ordered DB writes
- No cross-order interference

---

# Message Flow Example

## Input Message

```json
{
  "OrderId": "123",
  "Step": "API_CALL",
  "Payload": "ORDER_PAYLOAD_SAMPLE"
}
```

---

## Step 1 — Function A Execution

```
Message received
API call executed (12 sec)
```

---

## Step 2 — Forwarding

```
Send message to Queue B
SessionId = 123
```

---

## Step 3 — Function B Execution

```
Receive message
Write to database (28 sec)
Complete processing
```

---

# Why This Architecture Works

## Before (Single Function)

```
API + DB + Logic
= One long-running execution
```

Problems:

- Lock expiration
- Retry duplication
- Tight coupling
- Scaling bottlenecks

---

## After (Pipeline)

```
Function A (12 sec)
      │
      ▼
Queue B
      │
      ▼
Function B (28 sec)
```

Benefits:

- Independent scaling per stage
- No lock contention across full workflow
- Fault isolation
- Easier debugging

---

# Trade-Offs

## ✔ Advantages

- Clean separation of responsibilities
- Independent scaling per function
- Reduced lock pressure
- Easier maintenance
- Better resilience

---

## ⚠️ Limitation

### Duplicate Execution Risk

If Function A succeeds API call but fails before enqueue:

```
API may execute again on retry
```

---

## 🔧 Production Mitigations

- Idempotency keys per OrderId
- Outbox pattern
- Persistent state tracking (Cosmos DB / SQL)
- Deduplication store (Redis)

---

# Real-World Use Cases

## E-Commerce

```
Order Validation
→ Payment API
→ Inventory Update
→ Order Finalization
```

---

## Logistics

```
Order Intake
→ Warehouse Allocation
→ Carrier Booking
→ Shipment Tracking
```

---

## Financial Systems

```
Transaction Intake
→ Fraud Check
→ Bank Validation
→ Ledger Update
```

---

# Key Takeaway

Azure Service Bus is a **messaging system**, not a workflow engine.

Forcing it to behave like one leads to:

- Lock issues
- Duplicate processing
- Retry storms
- Scaling bottlenecks

---

# Correct Mental Model

```
Service Bus = Transport Layer
Functions = Processing Units
Queues = Workflow Boundaries
```

---

# Final Architecture Summary

```
Queue A (Orders)
      │
      ▼
Function A (API Step)
      │
      ▼
Queue B (Commits)
      │
      ▼
Function B (DB Step)
      │
      ▼
Database
```

---

# Final Insight

If your workflow:

- exceeds lock duration
- includes external API calls
- has multiple steps
- includes retries or throttling

👉 It should NOT live in a single Service Bus function.

Instead:

👉 Split it into a queue-based pipeline (Pipes & Filters architecture)