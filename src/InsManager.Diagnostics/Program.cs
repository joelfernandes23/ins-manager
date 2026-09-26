using System.Text.Json;
using SimConnect.NET;
using SimConnect.NET.InputEvents;

const string outputFileName = "input-events.json";
var showCivaState = args.Contains("--civa-state", StringComparer.OrdinalIgnoreCase);
var runWriteSelfTest = args.Contains("--write-self-test", StringComparer.OrdinalIgnoreCase);
var runInputWriteSelfTest = args.Contains("--input-write-self-test", StringComparer.OrdinalIgnoreCase);
var inputDetailsIndex = Array.FindIndex(args, argument =>
    argument.Equals("--input-details", StringComparison.OrdinalIgnoreCase));
var showInputDetails = inputDetailsIndex >= 0;
var inputDetailsFilter = showInputDetails && inputDetailsIndex + 1 < args.Length
    ? args[inputDetailsIndex + 1]
    : "INS";
var filters = showInputDetails
    ? [inputDetailsFilter]
    : args.Length == 0 || showCivaState || runWriteSelfTest || runInputWriteSelfTest
    ? ["CIVA", "INS", "CDU", "WAYPOINT", "WYPT"]
    : args;

using var cancellation = new CancellationTokenSource(
    showInputDetails ? TimeSpan.FromSeconds(120) : TimeSpan.FromSeconds(30));
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

    if (showCivaState)
    {
        await PrintCivaStateAsync(client, cancellation.Token);
        return 0;
    }

    if (runWriteSelfTest)
    {
        await RunWriteSelfTestAsync(client, cancellation.Token);
        return 0;
    }

    if (runInputWriteSelfTest)
    {
        await RunInputWriteSelfTestAsync(client, cancellation.Token);
        return 0;
    }

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
        if (showInputDetails)
        {
            var parameters = await TryReadAsync(
                client,
                token => client.InputEvents.EnumerateInputEventParametersAsync(inputEvent.Hash, token),
                cancellation.Token);
            var value = await TryReadAsync(
                client,
                async token => (await client.InputEvents.GetInputEventAsync(inputEvent.Hash, token)).ToString(),
                cancellation.Token);
            Console.WriteLine($"  Parameters: {parameters}");
            Console.WriteLine($"  Value:      {value}");
        }
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
    Console.Error.WriteLine("The scan timed out. Keep MSFS 2024 running with the target aircraft loaded, then try again.");
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

static async Task<string> TryReadAsync(
    SimConnectClient client,
    Func<CancellationToken, Task<string>> operationFactory,
    CancellationToken cancellationToken)
{
    using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    requestCancellation.CancelAfter(TimeSpan.FromSeconds(2));
    try
    {
        return await PumpUntilCompleteAsync(
            client,
            operationFactory(requestCancellation.Token),
            requestCancellation.Token);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return "Unavailable (request timed out)";
    }
    catch (Exception exception) when (exception is SimConnectException or InvalidOperationException)
    {
        return $"Unavailable ({exception.Message})";
    }
}

static async Task PrintCivaStateAsync(SimConnectClient client, CancellationToken cancellationToken)
{
    async Task<double> ReadAsync(string name) => await PumpUntilCompleteAsync(
        client,
        client.SimVars.GetAsync<double>(name, "Number", cancellationToken: cancellationToken),
        cancellationToken);

    var state = new Dictionary<string, double>
    {
        ["SIM_LAT"] = await ReadAsync("L:FSS_B727_CIVA_SIM_LAT"),
        ["SIM_LON"] = await ReadAsync("L:FSS_B727_CIVA_SIM_LON"),
        ["POS_LAT"] = await ReadAsync("L:FSS_B727_CIVA_POS_LAT"),
        ["POS_LON"] = await ReadAsync("L:FSS_B727_CIVA_POS_LON"),
        ["FROM"] = await ReadAsync("L:FSS_B727_CIVA_FROM"),
        ["TO"] = await ReadAsync("L:FSS_B727_CIVA_TO"),
    };

    for (var slot = 1; slot <= 9; slot++)
    {
        state[$"WP_{slot}_LAT"] = await ReadAsync($"L:FSS_B727_CIVA_WP_{slot}_LAT");
        state[$"WP_{slot}_LON"] = await ReadAsync($"L:FSS_B727_CIVA_WP_{slot}_LON");
    }

    Console.WriteLine(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
}

static async Task RunWriteSelfTestAsync(SimConnectClient client, CancellationToken cancellationToken)
{
    const string latitudeVariable = "L:FSS_B727_CIVA_WP_9_LAT";
    const string longitudeVariable = "L:FSS_B727_CIVA_WP_9_LON";

    async Task<double> ReadAsync(string name) => await PumpUntilCompleteAsync(
        client,
        client.SimVars.GetAsync<double>(name, "Number", cancellationToken: cancellationToken),
        cancellationToken);

    async Task WriteAsync(string name, double value) => await client.SimVars.SetAsync(
        name,
        "Number",
        value,
        cancellationToken: cancellationToken);

    var originalLatitude = await ReadAsync(latitudeVariable);
    var originalLongitude = await ReadAsync(longitudeVariable);

    try
    {
        await WriteAsync(latitudeVariable, 12.345678);
        await WriteAsync(longitudeVariable, -98.765432);
        var actualLatitude = await ReadAsync(latitudeVariable);
        var actualLongitude = await ReadAsync(longitudeVariable);
        var passed = Math.Abs(actualLatitude - 12.345678) < 0.000001
            && Math.Abs(actualLongitude - -98.765432) < 0.000001;
        Console.WriteLine(passed
            ? "CIVA waypoint write self-test passed."
            : $"CIVA waypoint write self-test failed: read {actualLatitude}, {actualLongitude}.");
        if (!passed) Environment.ExitCode = 1;
    }
    finally
    {
        await WriteAsync(latitudeVariable, originalLatitude);
        await WriteAsync(longitudeVariable, originalLongitude);
        Console.WriteLine("INS slot 9 restored to its original values.");
    }
}

static async Task RunInputWriteSelfTestAsync(SimConnectClient client, CancellationToken cancellationToken)
{
    var events = await PumpUntilCompleteAsync(
        client,
        client.InputEvents.EnumerateInputEventsAsync(cancellationToken),
        cancellationToken);
    var testNames = new[] { "INS_DATA_SELECTOR_1", "INS_OPERATING_MODE_1", "INS_THUMBWHEEL_1" };

    foreach (var name in testNames)
    {
        var descriptor = events.Single(inputEvent => inputEvent.Name.Equals(name, StringComparison.Ordinal));
        var before = await PumpUntilCompleteAsync(
            client,
            client.InputEvents.GetInputEventAsync(descriptor.Hash, cancellationToken),
            cancellationToken);
        if (!before.TryGetDoubleValue(out var value))
            throw new InvalidOperationException($"{name} did not return a numeric value.");

        await client.InputEvents.SetInputEventAsync(descriptor.Hash, value, cancellationToken);
        var after = await PumpUntilCompleteAsync(
            client,
            client.InputEvents.GetInputEventAsync(descriptor.Hash, cancellationToken),
            cancellationToken);
        if (!after.TryGetDoubleValue(out var confirmed) || Math.Abs(confirmed - value) > 0.000001)
            throw new InvalidOperationException($"{name} did not retain its existing value.");

        Console.WriteLine($"{name}: write/read passed at {confirmed}.");
    }
}

internal sealed record EventReport(string Name, ulong Hash, string Type, string NodeNames);
