using System.Text.Json;
using SimConnect.NET;
using SimConnect.NET.InputEvents;

const string outputFileName = "input-events.json";
var filters = args.Length == 0
    ? ["CIVA", "INS", "CDU", "WAYPOINT", "WYPT"]
    : args;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

await using var client = new SimConnectClient
{
    AutoReconnectEnabled = false,
};

try
{
    Console.WriteLine("Connecting to MSFS 2024...");
    await client.ConnectAsync(IntPtr.Zero, 0, 0, cancellation.Token);

    Console.WriteLine("Connected. Enumerating input events...");
    var events = await PumpUntilCompleteAsync(
        client,
        client.InputEvents.EnumerateInputEventsAsync(cancellation.Token),
        cancellation.Token);

    var matches = events
        .Where(inputEvent => filters.Any(filter =>
            inputEvent.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || inputEvent.NodeNames.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(inputEvent => inputEvent.Name, StringComparer.OrdinalIgnoreCase)
        .Select(inputEvent => new EventReport(
            inputEvent.Name,
            inputEvent.Hash,
            inputEvent.Type.ToString(),
            inputEvent.NodeNames))
        .ToArray();

    Console.WriteLine($"Found {events.Count()} total input events; {matches.Length} matched: {string.Join(", ", filters)}");
    foreach (var inputEvent in matches)
    {
        Console.WriteLine($"{inputEvent.Name,-40} 0x{inputEvent.Hash:X16} {inputEvent.Type,-12} {inputEvent.NodeNames}");
    }

    var outputPath = Path.GetFullPath(outputFileName);
    await File.WriteAllTextAsync(
        outputPath,
        JsonSerializer.Serialize(matches, new JsonSerializerOptions { WriteIndented = true }),
        cancellation.Token);
    Console.WriteLine($"Saved {outputPath}");
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("The scan timed out. Keep MSFS 2024 running with the FSS 727 loaded, then try again.");
    return 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Scan failed: {exception.Message}");
    return 1;
}

static async Task<T> PumpUntilCompleteAsync<T>(
    SimConnectClient client,
    Task<T> operation,
    CancellationToken cancellationToken)
{
    while (!operation.IsCompleted)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var processed = await client.ProcessNextMessageAsync(cancellationToken);
        if (!processed)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    return await operation;
}

internal sealed record EventReport(string Name, ulong Hash, string Type, string NodeNames);
