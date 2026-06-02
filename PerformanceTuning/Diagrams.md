# ⚙️ Azure Service Bus Prefetch & Concurrency Tuning Model  
## (Stop-and-Go vs Continuous Pipelining)

This document explains how **Prefetch Count, Batch Size, and Concurrency settings** affect throughput, starvation, and system efficiency in Azure Service Bus + Azure Functions.

---

# ❌ The Bad State: Stop-and-Go Starvation

## Configuration

```
Prefetch = 100
Batch Size = 100
```

---

## Problem

- Worker consumes entire buffer instantly
- Network thread becomes idle while refilling
- CPU and network operate sequentially instead of in parallel

---

## System Flow (Bad State)

```mermaid
flowchart TD

A[Azure Service Bus] -->|Fetch 100 msgs| B[Prefetch Buffer\nCapacity = 100]

B -->|Instant drain| C[Worker / Pump\nBatch Size = 100]

C --> D[Database Write]

B -.-> E[Idle CPU Waiting]
```

---

## Outcome

- CPU starvation cycles
- Poor throughput
- High latency variance
- No overlap between compute and network

---

# ✅ The Good State: Continuous Pipelining

## Configuration

```
Prefetch = 2000
Batch Size = 100
```

---

## Key Idea

> Prefetch becomes a sliding buffer, not a batch unit.

Network and CPU run in parallel.

---

## System Flow (Good State)

```mermaid
flowchart TD

A[Azure Service Bus] -->|Continuous background fetch| B[Prefetch Buffer<br/>Capacity = 2000]

B -->|Batch of 100| C[Worker / Pump]

C --> D[Database]

A -. refill while processing .-> B
C -. consumes gradually .-> B
```

---

## Result

- CPU always busy
- Network always refilling buffer
- No idle cycles
- Stable throughput

---

# ⚠️ Critical Misconception

## Prefetch is NOT a batch size

Many assume:

> Prefetch = work chunk size

❌ Wrong

---

## Correct Model

```mermaid
flowchart LR

A[Prefetch] --> B[Sliding Buffer / Credit Pool]
B --> C[Continuous Delivery Mechanism]
```

---

# 🔄 AMQP Credit-Based Flow Control

Azure Service Bus uses AMQP internally:

- SDK issues "credits"
- Broker sends messages based on available credit
- Credits are replenished continuously

---

## Flow Control Model

```mermaid
sequenceDiagram
participant SDK
participant Broker
participant Buffer

SDK->>Broker: Request 2000 credits
Broker->>Buffer: Send 2000 messages

SDK->>Buffer: Consume 100 messages
SDK->>Broker: Replenish +100 credits

Broker->>Buffer: Stream 100 new messages
```

---

## Float Valve Analogy

```mermaid
flowchart LR

A[Azure Service Bus] --> B[Reservoir]
B --> C[Prefetch Buffer / Tank]
C --> D[Worker / Pump]
D --> E[Database]

C -. float valve .-> B
```

---

# ⚠️ Edge Cases

---

## Case A: Slow Worker (Backpressure)

```mermaid
flowchart TD

A[Worker Slow Processing<br/>10 min batch] --> B[Prefetch stays ~full]

B --> C[SDK stops requesting credits]
C --> D[Broker pauses sending]

D --> E[Backpressure protects system]
```

### Outcome

- Prevents memory overflow
- Automatically throttles broker
- Stabilizes system

---

## Case B: Empty Queue

```mermaid
flowchart TD

A[Service Bus Queue] -->|Only 50 messages| B[Prefetch Buffer]

B --> C[Worker consumes 50]

C --> D[Idle but non-blocking state]
```

### Outcome

- No blocking
- Immediate processing of new messages

---

# 📊 Adaptive Decision Tree

```mermaid
flowchart TD

A[START MONITORING] --> B{DB latency or retries spiking?}

B -->|YES| C[Decrease Concurrency]
B -->|NO| D{Lock duration nearing max?}

D -->|YES| E[Decrease Concurrency]
D -->|NO| F{Prefetch hitting zero?}

F -->|YES| G[Decrease Batch Size]
F -->|NO| H{Queue depth increasing?}

H -->|YES| I[Increase Concurrency]
H -->|NO| J[System Optimal]
```

---

# ⚙️ Tuning Levers

---

## 🔻 Decrease Concurrency

```mermaid
flowchart LR

A[High DB Pressure] --> B[Reduce MaxConcurrentCalls]
B --> C[Lower system load]
```

---

## 🔻 Decrease Batch Size

```mermaid
flowchart LR

A[Prefetch starvation] --> B[Smaller consumption units]
B --> C[Smooth pipeline refill]
```

---

## 🔺 Increase Concurrency

```mermaid
flowchart LR

A[Healthy DB + backlog] --> B[Increase parallel workers]
B --> C[Higher throughput]
```

---

# 🧠 Core Insight

```mermaid
flowchart LR

A[Prefetch] --> B[Flow Control]
C[Concurrency] --> D[Pressure Control]

B --> E[Network efficiency]
D --> F[System stability]
```

---

# 🚀 Final Principle

A correctly tuned system ensures:

```mermaid
flowchart LR

CPU -->|Always busy| DB
Network -->|Always refilling| Buffer
Buffer -->|Never empty| CPU
DB -->|Never overwhelmed| System
```

---

# 📌 Summary

| Configuration | Behavior |
|--------------|----------|
| Prefetch = Batch Size | Stop-and-go starvation |
| Prefetch >> Batch Size | Continuous pipelining |
| Poor tuning | Idle CPU cycles |
| Optimal tuning | Full resource utilization |

---

# 🎯 Final Insight

> If CPU and network are taking turns, your system is not compute-bound — it is pipeline-bound.
