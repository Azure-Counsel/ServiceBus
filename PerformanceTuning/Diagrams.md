# ⚙️ Azure Service Bus Prefetch & Concurrency Tuning Model (Stop-and-Go vs Pipelined Throughput)

This document explains how **Prefetch Count, Batch Size, and Concurrency settings** in Azure Service Bus + Azure Functions impact system throughput, starvation, and CPU/network utilization.

It models two states:

- ❌ Stop-and-Go Starvation (bad configuration)
- ✅ Continuous Pipelining (optimal configuration)

---

# ❌ The Bad State: "Stop-and-Go" Starvation

## Configuration

```
Prefetch = 100
Batch Size = 100
```

---

## Problem Description

- The worker pulls exactly the number of messages it can process.
- The buffer empties instantly.
- The system alternates between:
  - network waiting
  - CPU processing

This creates a **stop-start execution pattern**.

---

## System Flow

```
       [ 🌊 RESERVOIR ]       (Azure Service Bus)
              │
              ▼ 1. Network fetches 100 messages (~500ms)
       ┌──────────────┐
       │ 🛢️ LOCAL TANK │   PREFETCH BUFFER (Capacity: 100)
       │  [ EMPTY! ]   │   Current Level: 0
       └──────┬───────┘
              │
              ▼ 2. Pump consumes all 100 instantly
         ┏━━━━━━━━━━┓
         ┃ 🛑 PUMP 🛑 ┃   Batch Size = 100
         ┗━━━━┳━━━━━┛
              ┃
              ▼
            ┏━━━┓
            ┃ DB┃
            ┗━━━┛
```

---

## Result

- CPU finishes work quickly
- Then becomes idle
- Waiting for network refill

### ❌ Outcome

- Starvation cycles
- Poor throughput
- Underutilized CPU
- Increased latency variance

---

# ✅ The Good State: Continuous Pipelining

## Configuration

```
Prefetch = 2000
Batch Size = 100
```

---

## Key Idea

The buffer is **larger than consumption rate**, enabling overlap:

- Network refills buffer
- CPU processes current batch
- Both run in parallel

---

## System Flow

```
       [ 🌊 RESERVOIR ]       (Azure Service Bus)
              │
              ▼ 1. Background refill
       ┌──────────────┐
       │ 🛢️ LOCAL TANK │   PREFETCH BUFFER (Capacity: 2000)
       │ [████████░░]  │   Level: ~1900
       └──────┬───────┘
              │
              ▼ 2. Pump consumes 100 smoothly
         ┏━━━━━━━━━━┓
         ┃ ⚡ PUMP ⚡ ┃   Batch Size = 100
         ┗━━━━┳━━━━━┛
              ┃
              ▼
            ┏━━━┓
            ┃ DB┃
            ┗━━━┛
```

---

## Result

- No idle CPU
- Continuous processing
- Network and compute overlap
- Stable throughput

---

# ⚠️ Critical Misconception

## Prefetch is NOT a batch size

Many assume:

> “Prefetch = chunk of work processed then refilled”

❌ Incorrect

---

## Correct Model

Prefetch is a:

> 🔁 Sliding buffer (credit-based flow control)

---

# 🔄 AMQP Credit-Based Flow Control

Azure Service Bus uses AMQP internally:

- SDK issues "credits"
- Broker sends messages accordingly
- Credits are replenished continuously

---

## Float Valve Analogy

Think of a water tank:

- Broker = Reservoir
- Prefetch buffer = Tank
- Worker = Pump
- Credits = Float valve mechanism

---

## Continuous Refill Cycle

```
1. SDK grants 2000 credits
2. Broker sends 2000 messages
3. Worker consumes 100
4. SDK immediately issues +100 credits
5. Broker refills buffer
6. System stabilizes around high watermark
```

---

## Diagram

```
[ 🌊 AZURE SERVICE BUS ]
          │
          │ 3. AMQP credit signal (+100)
          ▼
   ┌──────────────┐
   │ 🛢️ LOCAL TANK │
   │  (Max 2000)   │
   │ [██████████]  │  ~1900–2000 stable range
   └──────┬───────┘
          │
          ▼
        ⚡ PUMP
          │
          ▼
         DB
```

---

# ⚠️ Edge Cases

---

## Case A: Slow Worker (Backpressure)

### Scenario

- Worker pulls 100 messages
- Processing takes 10 minutes
- DB is slow

### Effect

- Prefetch remains full (~1900)
- SDK stops requesting more credits
- Broker pauses sending messages

### Result

✔ Natural backpressure  
✔ Prevents memory overflow  
✔ Protects worker stability  

---

## Case B: Empty Queue

### Scenario

- Only 50 messages exist
- SDK requests 2000

### Effect

- Broker returns only 50 messages
- Prefetch stays low

### Result

✔ No blocking  
✔ Immediate delivery of new messages  

---

# 📊 Decision Tree for Tuning

## Signals to Monitor

1. Queue depth increasing
2. Lock duration nearing max
3. DB/API latency increasing
4. Retry rate increasing

---

## Adaptive Control Logic

```
                          [ START ]
                              │
     ┌────────────────────────┴────────────────────────┐
     ▼                                                 ▼
[ DB latency / retries spiking? ] ─ YES ─► ↓ Concurrency
     │                                               (protect DB)
     ▼ NO
     │
[ Lock duration near max? ] ─────── YES ─► ↓ Concurrency
     │                                               (reduce pressure)
     ▼ NO
     │
[ Prefetch hits zero? ] ─────────── YES ─► ↓ Batch Size
     │                                               (fix starvation)
     ▼ NO
     │
[ Queue depth increasing? ] ─────── YES ─► ↑ Concurrency
     │                                               (scale out)
     ▼ NO
     │
   ✅ OPTIMAL STATE
```

---

# ⚙️ Tuning Levers Explained

---

## 🔻 Decrease Concurrency (MaxConcurrentCalls)

### Purpose
Protect downstream systems.

### Effect
- Fewer parallel DB/API calls
- Reduced contention
- Stabilizes failures

---

## 🔻 Decrease Batch Size

### Purpose
Fix starvation cycles.

### Effect
- Smaller consumption bursts
- Keeps buffer from hitting zero
- Improves pipelining

---

## 🔺 Increase Concurrency

### Purpose
Increase throughput.

### When to use
Only when:

- DB is healthy
- No retries spiking
- Network is stable
- Backlog exists

---

# 🧠 Core Insight

> Prefetch controls *flow*, not batching.

> Concurrency controls *pressure*, not speed.

---

# 🚀 Final Principle

A well-tuned system ensures:

- CPU never waits for network
- Network never waits for CPU
- DB is never overwhelmed
- Buffer never oscillates between empty/full

---

# 📌 Summary

| State | Behavior |
|------|--------|
| Low Prefetch = Batch Size | Stop-and-Go starvation |
| High Prefetch > Batch Size | Continuous pipelining |
| Balanced tuning | Maximum throughput |

---

# 🎯 Final Insight

If your system alternates between:

> "fast processing" → "idle waiting"

You are not compute-bound.

You are **pipeline-bound**.
```