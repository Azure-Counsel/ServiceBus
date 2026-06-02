using AsbHybridIdempotencyDemo.Models;

namespace AsbHybridIdempotencyDemo.Services;

public class HybridIdempotencyService
{
    private readonly RedisCacheService _redis;
    private readonly SqlLedgerService _sql;
    private readonly PaymentGateway _payment;

    public HybridIdempotencyService(
        RedisCacheService redis,
        SqlLedgerService sql,
        PaymentGateway payment)
    {
        _redis = redis;
        _sql = sql;
        _payment = payment;
    }

    public void Process(OrderMessage order)
    {
        Console.WriteLine($"\n📩 Processing {order.OrderId}");

        // 1. FAST PATH (Redis)
        if (_redis.Exists(order.OrderId))
        {
            Console.WriteLine("⚡ Redis HIT → Short-circuit");
            return;
        }

        // 2. STRONG PATH (SQL)
        if (_sql.Exists(order.OrderId))
        {
            Console.WriteLine("🗄️ SQL HIT → Duplicate blocked");
            return;
        }

        // 3. BUSINESS LOGIC
        _payment.Charge(order.OrderId, order.Amount);

        _sql.Insert(order.OrderId);

        _redis.Set(order.OrderId);

        Console.WriteLine($"✅ Completed {order.OrderId}");
    }
}