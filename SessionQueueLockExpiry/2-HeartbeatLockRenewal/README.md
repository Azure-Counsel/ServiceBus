# 🔄 Azure Service Bus Session Lock Heartbeat Renewal Demo

## 📌 Overview

This project demonstrates a **real-world distributed systems problem** in Azure Service Bus session processing:

> How do you safely process long-running work while preventing a Service Bus session lock from expiring?

Azure Service Bus uses **session locks** to ensure ordered, single-consumer processing. However, these locks are time-bound and can expire if the processing takes too long.

This demo shows how to solve that problem using a:

> ✅ Heartbeat-based session lock renewal mechanism

---

## ⚠️ The Problem (Real Production Scenario)

When using **Azure Service Bus Sessions**, only one consumer can process a session at a time.

However:

### Session Lock Characteristics:
- Locks are **time-limited**
- If not renewed → lock expires
- Another consumer can take over the session

### Real-world failure scenarios:
- Slow downstream API calls
- Infinite waits / deadlocks
- Long batch processing
- External dependency failures

### What goes wrong without renewal control:
- Duplicate processing
- Out-of-order execution
- Data corruption in workflows
- “Ghost ownership” of a session

---

## 🧠 Core Idea of This Solution

We introduce a **heartbeat + renewal safety system**:

### Key concept:
> “Only renew the session lock if the process is actively making progress.”

---

### The system consists of:

| Component | Responsibility |
|----------|----------------|
| `HeartbeatTracker` | Tracks last successful progress |
| `LongRunningOrderProcessor` | Simulates real business processing |
| `SessionLockHeartbeatRenewal` | Keeps session lock alive safely |
| `Program` | Orchestrates everything |

---

## 🧩 Architecture (Mental Model)
            ┌──────────────────────────────┐
            │ Azure Service Bus Session   │
            └─────────────┬────────────────┘
                          │
                          ▼
            ┌──────────────────────────────┐
            │ LongRunningOrderProcessor    │
            │ (Business Logic Execution)   │
            └─────────────┬────────────────┘
                          │
                          ▼
            ┌──────────────────────────────┐
            │ HeartbeatTracker             │
            │ (Signals progress)           │
            └─────────────┬────────────────┘
                          │
                          ▼
            ┌──────────────────────────────┐
            │ SessionLockHeartbeatRenewal  │
            │ (Keeps session alive)        │
            └──────────────────────────────┘
            
---

## 🔁 Execution Flow (Step-by-Step)
1) Service Bus session is acquired
2) Heartbeat system is initialized
3) Lock renewal loop starts (background task)
4) Business processor starts executing
5) Processor completes Chunk 1 → sends heartbeat
6) Processor completes Chunk 2 → sends heartbeat
7) Processor hits long-running external call (hang simulation)
8) Heartbeat stops updating
9) Renewal loop detects stale heartbeat
10) Lock renewal stops safely

---

## 🧩 Component Breakdown

---

## 1. 🟢 HeartbeatTracker

### Purpose
Tracks the **last known successful progress point** in processing.

### Key responsibilities:
- Records timestamps when work is completed
- Calculates “age” of last progress

### Core behavior:

```csharp
_lastBeatUtc = DateTime.UtcNow;


---

## 🧩 Component Breakdown

------------------------------------------------------------------------

## 1. 🟢 HeartbeatTracker

### Purpose
Tracks the **last known successful progress point** in processing.

### Key responsibilities:
- Records timestamps when work is completed
- Calculates “age” of last progress

### Core behavior:

```csharp
_lastBeatUtc = DateTime.UtcNow;

Why it matters:

It acts as a liveness indicator for the system.

If heartbeat is fresh → system is healthy
If heartbeat is stale → system is stuck

------------------------------------------------------------------------

2. ⚙️ LongRunningOrderProcessor
Purpose

Simulates real-world business logic that may take time or hang.

Behavior:
Processes Chunk 1
Sends heartbeat
Processes Chunk 2
Sends heartbeat
Simulates external dependency failure

await Task.Delay(Timeout.InfiniteTimeSpan);

Real-world equivalent:
Payment gateway call hanging
External API latency spike
Database deadlock
Downstream microservice failure
Key insight:

Business logic must explicitly signal progress.
------------------------------------------------------------------------

3. 🔄 SessionLockHeartbeatRenewal
Purpose

Continuously renews the Service Bus session lock.

Runs in background:
Every 15 seconds checks system state
Only renews if heartbeat is healthy
Stops if system is unhealthy

Core logic:
⏱ Renewal loop interval
TimeSpan.FromSeconds(15)
⚠️ Heartbeat timeout rule
if (_heartbeat.Age > _heartbeatTimeout)

If true:

Stop renewal
Avoid unnecessary lock extensions
🔐 Session lock renewal call
await _receiver.RenewSessionLockAsync(cancellationToken);
Why this matters:

This prevents session takeover by another consumer.

Failure handling:

If renewal fails:

Log error
Stop renewal loop immediately
------------------------------------------------------------------------
4. 🚀 Program (Orchestration Layer)
Responsibilities:
Connect to Azure Service Bus
Accept session
Start heartbeat system
Start renewal loop (background)
Start business processing
Cancel renewal safely on exit
Flow:
var receiver = await client.AcceptNextSessionAsync("orders");

Session is acquired → processor owns it.

Parallel execution model:
Thread 1 → Business Processing
Thread 2 → Lock Renewal Loop
Shared   → HeartbeatTracker
🧪 What This Demo Shows
Without heartbeat system:

❌ Locks may be renewed even when system is stuck
❌ Silent failures continue renewing dead sessions
❌ Resource leaks in distributed systems

With heartbeat system:

✅ Renewal only happens during active processing
✅ Stuck processes stop renewing safely
✅ Session is released predictably
✅ Prevents hidden system failures
------------------------------------------------------------------------
🚀 How to Run the Project

Prerequisites

.NET 8+
Azure Service Bus Namespace
Queue with Sessions enabled
Valid connection string
Azure Setup

Create or configure:

Queue Name: orders
Sessions: ENABLED
Update connection string
new ServiceBusClient("<connection-string>");
Run application
dotnet run
📊 Expected Console Output Behavior

You will observe:

Session acquisition
Chunk processing logs
Heartbeat updates after each chunk
Session lock renewal logs
External API hang simulation
Renewal stopping due to stale heartbeat