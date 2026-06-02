using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

class Program
{
    static Random rand = new Random();

    // =========================
    // SYSTEM STATE
    // =========================
    static int queueDepth = 5000;
    static Queue<int> prefetchBuffer = new Queue<int>();
    static int activeWorkers = 0;
    static int processed = 0;

    static string mode = "problem";

    // =========================
    // CONFIG (TUNABLE LEVERS)
    // =========================
    static int concurrency = 16;
    static int batchSize = 100;
    static int prefetchCount = 100;

    static int baseLatency = 120;

    static object lockObj = new object();

    static void Main()
    {
        Console.WriteLine("=== Azure Service Bus Backpressure Simulator ===");
        Console.WriteLine("Press P = Problem Mode | F = Fix Mode | R = Reset | Q = Quit");

        // background simulation loop
        new Thread(SimLoop) { IsBackground = true }.Start();

        // input loop
        while (true)
        {
            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.P)
                mode = "problem";

            if (key == ConsoleKey.F)
                mode = "fix";

            if (key == ConsoleKey.R)
                Reset();

            if (key == ConsoleKey.Q)
                break;
        }
    }

    // =========================
    // MAIN SIMULATION LOOP
    // =========================
    static void SimLoop()
    {
        while (true)
        {
            PrefetchStep();
            WorkerStep();
            DetectSignals();
            PrintStats();

            Thread.Sleep(200);
        }
    }

    // =========================
    // PREFETCH (AMQP BUFFER)
    // =========================
    static void PrefetchStep()
    {
        lock (lockObj)
        {
            if (queueDepth <= 0) return;

            if (prefetchBuffer.Count < prefetchCount)
            {
                int fill = Math.Min(10, prefetchCount - prefetchBuffer.Count);

                for (int i = 0; i < fill && queueDepth > 0; i++)
                {
                    prefetchBuffer.Enqueue(1);
                    queueDepth--;
                }
            }
        }
    }

    // =========================
    // WORKER (FUNCTION + DB CALL)
    // =========================
    static void WorkerStep()
    {
        lock (lockObj)
        {
            while (activeWorkers < concurrency && prefetchBuffer.Count > 0)
            {
                List<int> batch = new List<int>();

                for (int i = 0; i < batchSize && prefetchBuffer.Count > 0; i++)
                {
                    batch.Add(prefetchBuffer.Dequeue());
                }

                activeWorkers += batch.Count;

                foreach (var msg in batch)
                {
                    ThreadPool.QueueUserWorkItem(ProcessMessage);
                }
            }
        }
    }

    // =========================
    // MESSAGE PROCESSING
    // =========================
    static void ProcessMessage(object state)
    {
        int latency = GetLatency();

        Thread.Sleep(latency);

        lock (lockObj)
        {
            processed++;
            activeWorkers--;
        }
    }

    // =========================
    // DYNAMIC LATENCY MODEL
    // =========================
    static int GetLatency()
    {
        if (mode == "problem")
        {
            // downstream gets slower as load increases
            return baseLatency + (activeWorkers * 6);
        }

        // fixed stable latency in fix mode
        return baseLatency;
    }

    // =========================
    // SIGNAL DETECTION (OBSERVABILITY)
    // =========================
    static void DetectSignals()
    {
        lock (lockObj)
        {
            if (queueDepth > 4000)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (prefetchBuffer.Count < 50)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Green;
        }
    }

    // =========================
    // STATS PRINT
    // =========================
    static void PrintStats()
    {
        lock (lockObj)
        {
            Console.Clear();

            Console.WriteLine("=== MODE: " + mode.ToUpper() + " ===");
            Console.WriteLine();

            Console.WriteLine("QUEUE DEPTH        : " + queueDepth);
            Console.WriteLine("PREFETCH BUFFER    : " + prefetchBuffer.Count);
            Console.WriteLine("ACTIVE WORKERS     : " + activeWorkers);
            Console.WriteLine("PROCESSED          : " + processed);
            Console.WriteLine();

            Console.WriteLine("CONCURRENCY        : " + concurrency);
            Console.WriteLine("BATCH SIZE         : " + batchSize);
            Console.WriteLine("PREFETCH COUNT     : " + prefetchCount);
            Console.WriteLine();

            Console.WriteLine("PRESS:");
            Console.WriteLine("P = Problem Mode | F = Fix Mode | R = Reset | Q = Quit");
        }
    }

    // =========================
    // RESET
    // =========================
    static void Reset()
    {
        lock (lockObj)
        {
            queueDepth = 5000;
            prefetchBuffer.Clear();
            activeWorkers = 0;
            processed = 0;
        }
    }
}