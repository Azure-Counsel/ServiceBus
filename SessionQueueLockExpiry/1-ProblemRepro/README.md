# Azure Service Bus Session Lock Expiry Demo

## Why This Repository Exists

Azure Service Bus Sessions are commonly used when applications require First-In-First-Out (FIFO) processing.

Many developers assume this means:

* Message 1 always completes before Message 2
* Message 2 always completes before Message 3
* Ordering is guaranteed regardless of processing time

Unfortunately, that assumption is incorrect.

This repository demonstrates a real production failure scenario where:

1. A worker acquires a Service Bus Session lock.
2. Processing takes longer than the session lock duration.
3. The lock expires.
4. Another worker acquires the same session.
5. Messages begin executing concurrently.
6. FIFO ordering silently collapses.
7. The final business state becomes corrupted.

The goal is to help developers understand why Service Bus Sessions are not a workflow engine and why long-running operations can break ordering guarantees.

---

# The Business Scenario

Imagine a financial ledger account containing:

Balance = $1000

The following messages arrive in order:

Message 1
Debit $100

Message 2
Apply 2% interest

Message 3
Close account

Correct execution should produce:

$1000
→ Debit $100
→ $900

$900
→ Add 2% interest
→ $918

Account Closed

Final State:

Balance = $918
Closed = True

Instead, this demo intentionally creates a lock-expiration event that causes the following sequence:

Apply Interest
→ $1020

Close Account

Debit $100

Final State:

Balance = $920
Closed = True

The result is mathematically incorrect and logically impossible.

No exceptions occur.

No messages fail.

No dead-letter queue is triggered.

The corruption is completely silent.

---

# Repository Structure

SessionLockExpiryDemo

Program.cs

Producer.cs

WorkerA.cs

WorkerB.cs

LedgerStore.cs

Models
└── LedgerMessage.cs

appsettings.json

---

# File Overview

## Program.cs

Purpose:

Application entry point.

Responsibilities:

* Creates Service Bus client
* Seeds messages
* Starts Worker A
* Starts Worker B
* Displays final ledger state

Think of this file as the orchestrator for the simulation.

---

## Producer.cs

Purpose:

Creates the ordered message stream.

Responsibilities:

Creates:

Message 1 → Debit

Message 2 → Interest

Message 3 → Close Account

All messages share the same SessionId.

This ensures they belong to the same FIFO stream.

Without a shared SessionId, Service Bus would not attempt ordered processing.

---

## WorkerA.cs

Purpose:

Simulates the original lock owner.

Responsibilities:

* Acquires the session
* Processes Message 1
* Intentionally delays processing
* Allows the session lock to expire

This worker represents a real production dependency problem such as:

* Slow API calls
* Database throttling
* Network issues
* Long retry loops

The worker continues processing even after the lock has expired.

This is the key condition that enables FIFO corruption.

---

## WorkerB.cs

Purpose:

Simulates a second worker instance.

Responsibilities:

* Waits for lock expiration
* Acquires the same session
* Processes remaining messages

This worker represents another Function App instance, container, or VM that picks up the abandoned session.

From Worker B's perspective, everything appears normal.

Service Bus believes the session is available.

---

## LedgerStore.cs

Purpose:

Simulates the database.

Responsibilities:

Stores:

* Balance
* Account status

Methods:

ApplyDebit()

ApplyInterest()

Close()

This class allows the final corruption to be observed directly.

Without this file, there would be no visible business impact.

---

## LedgerMessage.cs

Purpose:

Represents a Service Bus message payload.

Properties:

Sequence

Defines expected FIFO order.

Action

Business operation being performed.

Amount

Associated monetary value.

This model is intentionally simple so the focus remains on session behavior.

---

# Queue Configuration

Create a Service Bus Queue with the following settings:

Sessions: Enabled

Lock Duration: 30 Seconds

Tier: Premium

Max Delivery Count: 10

The lock duration is intentionally shorter than Worker A's processing time.

This is required to reproduce the issue.

---

# Running The Demo

Step 1

Create the Service Bus Queue.

Step 2

Update appsettings.json with your connection string.

Step 3

Run the application.

Step 4

Observe console output.

---

# Expected Output

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

---

# What This Demonstrates

This demo proves an important architectural concept:

Service Bus Sessions guarantee ordering only while session ownership remains valid.

The guarantee is:

Lock-Based Ordering

not

Work-Based Ordering

Once the lock expires:

FIFO protection disappears.

Any remaining business logic becomes vulnerable to concurrency.

---

# Key Takeaway

Service Bus Sessions are excellent for short-lived ordered processing.

They are not designed to manage long-running workflows.

If your processing regularly approaches lock duration limits, consider:

* Session lock renewal
* Idempotent handlers
* Optimistic concurrency
* Workflow decomposition
* Durable Functions

The moment processing time becomes longer than the lock budget, you should stop thinking in terms of queue semantics and start thinking in terms of workflow orchestration.
