# Azure Service Bus Session Lock Expiry Demo

## Why This Repository Exists

Azure Service Bus Sessions are commonly used when applications require First-In-First-Out (FIFO) processing.

Many developers assume this means:

- Message 1 always completes before Message 2  
- Message 2 always completes before Message 3  
- Ordering is guaranteed regardless of processing time  

Unfortunately, that assumption is incorrect.

This repository demonstrates a real production failure scenario where:

- A worker acquires a Service Bus Session lock  
- Processing takes longer than the session lock duration  
- The lock expires  
- Another worker acquires the same session  
- Messages begin executing concurrently  
- FIFO ordering silently collapses  
- The final business state becomes corrupted  

The goal is to help developers understand why Service Bus Sessions are not a workflow engine and why long-running operations can break ordering guarantees.

---

## The Business Scenario

Imagine a financial ledger account containing:

**Balance = $1000**

The following messages arrive in order:

1. Message 1 → Debit $100  
2. Message 2 → Apply 2% interest  
3. Message 3 → Close account  

### Correct execution should produce:

$1000 → Debit $100 → $900  
$900 → Add 2% interest → $918  
Account Closed  

**Final State:**
- Balance = $918  
- Closed = True  

---

### Instead, this demo intentionally creates a lock-expiration event that causes the following sequence:

- Apply Interest → $1020  
- Close Account  
- Debit $100  

**Final State:**
- Balance = $920  
- Closed = True  

The result is mathematically incorrect and logically impossible.

No exceptions occur.  
No messages fail.  
No dead-letter queue is triggered.  
The corruption is completely silent.

---

## Repository Structure

```
SessionLockExpiryDemo/
│
├── Program.cs
├── Producer.cs
├── WorkerA.cs
├── WorkerB.cs
├── LedgerStore.cs
├── Models/
│   └── LedgerMessage.cs
├── appsettings.json
```

---

## File Overview

### Program.cs
Application entry point.

Responsibilities:
- Creates Service Bus client  
- Seeds messages  
- Starts Worker A  
- Starts Worker B  
- Displays final ledger state  

Acts as the orchestrator for the simulation.

---

### Producer.cs
Creates the ordered message stream.

Responsibilities:
- Creates Message 1 → Debit  
- Creates Message 2 → Interest  
- Creates Message 3 → Close Account  
- Ensures all messages share the same SessionId  

This ensures FIFO grouping within Service Bus sessions.

---

### WorkerA.cs
Simulates the original lock owner.

Responsibilities:
- Acquires session  
- Processes Message 1  
- Intentionally delays processing  
- Allows session lock to expire  

Represents real-world issues such as:
- Slow API calls  
- Database throttling  
- Network delays  
- Long retry loops  

Key point: continues processing after lock expiry.

---

### WorkerB.cs
Simulates a second worker instance.

Responsibilities:
- Waits for session lock expiration  
- Acquires the same session  
- Processes remaining messages  

Represents another instance (Function App, VM, container).

From Worker B's perspective, everything appears valid.

---

### LedgerStore.cs
Simulates the database.

Stores:
- Balance  
- Account status  

Methods:
- ApplyDebit()  
- ApplyInterest()  
- Close()  

Enables visibility into final corrupted state.

---

### LedgerMessage.cs
Represents Service Bus message payload.

Properties:
- Sequence → Expected FIFO order  
- Action → Business operation  
- Amount → Monetary value  

Kept intentionally simple for clarity.

---

## Queue Configuration

Create a Service Bus Queue with:

- Sessions: Enabled  
- Lock Duration: 30 Seconds  
- Tier: Premium  
- Max Delivery Count: 10  

> Lock duration is intentionally shorter than Worker A processing time.

---

## Running The Demo

### Step 1
Create Service Bus Queue

### Step 2
Update `appsettings.json` with connection string

### Step 3
Run application

### Step 4
Observe console output

---

## Expected Output

```
WORKER A ACQUIRED SESSION
M1 RECEIVED
LOCK EXPIRES
WORKER B STOLE SESSION
INTEREST APPLIED +20
BALANCE=1020
ACCOUNT CLOSED
BALANCE=1020
DEBIT APPLIED -100
BALANCE=920
EXPECTED = 918
ACTUAL = 920
```

---

## What This Demonstrates

Service Bus Sessions guarantee ordering only while session ownership remains valid.

The guarantee is:

**Lock-Based Ordering**

not

**Work-Based Ordering**

Once the lock expires:
- FIFO protection disappears  
- Remaining business logic becomes vulnerable to concurrency  

---

## Key Takeaway

Service Bus Sessions are excellent for short-lived ordered processing.

They are not designed for long-running workflows.

If processing approaches lock duration limits, consider:

- Session lock renewal  
- Idempotent handlers  
- Optimistic concurrency  
- Workflow decomposition  
- Durable Functions  

---

The moment processing time exceeds lock budget, you must shift from queue semantics to workflow orchestration.
