namespace PipesAndFiltersFunctionApp.Services;

public class FakeApiClient
{
    public async Task<string> CallExternalApiAsync(string payload)
    {
        await Task.Delay(12000); // simulate 12s API latency
        return $"API_RESULT_FOR_{payload}";
    }
}