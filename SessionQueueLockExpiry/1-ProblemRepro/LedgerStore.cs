public static class LedgerStore
{
    private static readonly object Sync = new();

    public static decimal Balance = 1000;

    public static bool Closed = false;

    public static void ApplyDebit(decimal amount)
    {
        lock (Sync)
        {
            Balance -= amount;

            Console.WriteLine(
                $"DEBIT APPLIED -{amount}");

            Print();
        }
    }

    public static void ApplyInterest(decimal percent)
    {
        lock (Sync)
        {
            var interest = Balance * percent;

            Balance += interest;

            Console.WriteLine(
                $"INTEREST APPLIED +{interest}");

            Print();
        }
    }

    public static void Close()
    {
        lock (Sync)
        {
            Closed = true;

            Console.WriteLine(
                $"ACCOUNT CLOSED");

            Print();
        }
    }

    public static void Print()
    {
        Console.WriteLine(
            $"BALANCE={Balance} CLOSED={Closed}");
    }
}