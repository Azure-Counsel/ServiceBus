namespace PipesAndFiltersFunctionApp.Models;

public class OrderMessage
{
    public string OrderId { get; set; } = default!;
    public string Step { get; set; } = default!;
    public string Payload { get; set; } = default!;
}