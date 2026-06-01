using IdempotencyShieldDemo.Services;

namespace IdempotencyShieldDemo.Services;

public class MessageProcessor
{
    private readonly AccountService _accountService;

    public MessageProcessor(AccountService accountService)
    {
        _accountService = accountService;
    }

    public void Process(string messageId)
    {
        Console.WriteLine($"\n📩 Processing Message: {messageId}");

        var success = _accountService.ProcessPayment(messageId, 100);

        if (!success)
        {
            Console.WriteLine("🛑 Processing failed → will retry later");
            return;
        }

        Console.WriteLine("🎯 Message completed successfully");
    }
}