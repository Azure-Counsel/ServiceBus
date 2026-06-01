var connectionString =
    builder.Configuration["ServiceBus"];

var client =
    new ServiceBusClient(connectionString);

var sender =
    client.CreateSender("ledger");

await Producer.SeedAsync(sender);

Console.WriteLine(
    "Messages seeded");

var workerATask =
    WorkerA.Run(client);

var workerBTask =
    WorkerB.Run(client);

await Task.WhenAll(
    workerATask,
    workerBTask);

Console.WriteLine();

Console.WriteLine(
    "EXPECTED = 918");

Console.WriteLine(
    $"ACTUAL   = {LedgerStore.Balance}");