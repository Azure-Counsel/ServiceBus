# Azure Service Bus Backpressure Simulator (C#)

A lightweight console-based simulation that demonstrates how Prefetch, Batch Size, and Concurrency interact under downstream pressure, and how improper tuning leads to receiver starvation and backpressure collapse.

---

# What This Project Demonstrates

- Azure Service Bus Queue ingestion
- Prefetch buffer (AMQP sliding window)
- Batch processing behavior
- Downstream DB/API latency pressure
- Backpressure feedback loops

---

# Core Problem

Under load:
- Prefetch drains faster than refill
- Batch processing creates bursts
- Downstream latency increases occupancy
- Concurrency amplifies pressure

Result:
Receiver starvation + throughput instability

---

# Key Learnings

1. Prefetch is NOT a batch fetch (it is a sliding window buffer)
2. Batch size controls burstiness, not capacity
3. Concurrency is the real downstream throttle
4. Backpressure is a feedback loop, not a single bottleneck
5. Failures appear as oscillations, not crashes

---

# Architecture

Service Bus Queue → Prefetch Buffer → Worker (Batch Processor) → DB/API

---

# Operating Modes

## Problem Mode
- Downstream latency increases under load
- Buffer drains unpredictably
- Queue grows continuously
- Throughput becomes unstable

## Fix Mode
- Stable latency
- Controlled concurrency
- Smooth buffer behavior
- Predictable throughput

---

# Control Levers

## Concurrency
Controls parallel processing pressure.

## Batch Size
Controls burst intensity.

## Prefetch Count
Controls buffer continuity and starvation prevention.

---

# How to Run

```bash
dotnet new console -n ServiceBusBackpressureSim
cd ServiceBusBackpressureSim
```

Replace Program.cs with simulator code.

Then run:

```bash
dotnet run
```

---

# Controls

- P = Problem Mode
- F = Fix Mode
- R = Reset
- Q = Quit

---

# Key Insight

Most Service Bus performance issues are NOT queue issues.

They are downstream pressure + buffer misconfiguration issues.

---

# Limitation

If incoming rate exceeds downstream capacity:
- backlog will still grow
- latency will increase
- system stabilizes only via buffering

---

# Mental Model

Queue = Reservoir  
Prefetch = Buffer Tank  
Worker = Pump  
DB/API = Output Pipe
