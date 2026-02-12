#:package HiveMQtt@0.39.0

using HiveMQtt.Client;
using HiveMQtt.MQTT5.ReasonCodes;
using HiveMQtt.MQTT5.Types;

var host = "localhost";
var port = 1883;
var topic = "stresstest";

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

long total = 0;

var b = new HiveMQClientOptionsBuilder()
    .WithBroker(host)
    .WithPort(port)
    .WithClientId($"hivemqtt-{Guid.NewGuid():N}")
    .WithAutomaticReconnect(true);

using var client = new HiveMQClient(b.Build());
client.OnMessageReceived += (_, __) => Interlocked.Increment(ref total);

var cr = await client.ConnectAsync().ConfigureAwait(false);
if (cr.ReasonCode != ConnAckReasonCode.Success)
    return 2;

var sub = new SubscribeOptionsBuilder();
sub.WithSubscription(topic, QualityOfService.ExactlyOnceDelivery);
await client.SubscribeAsync(sub.Build()).ConfigureAwait(false);

Console.WriteLine($"Connected {host}:{port} topic={topic}");

var last = DateTime.UtcNow;
var lastN = 0L;
try
{
    while (!cts.IsCancellationRequested)
    {
        await Task.Delay(1000, cts.Token);
        var n = Interlocked.Read(ref total);
        var dt = Math.Max(1e-9, (DateTime.UtcNow - last).TotalSeconds);
        Console.WriteLine($"total={n} +{n - lastN} rate={(n - lastN) / dt:0} msg/s");
        last = DateTime.UtcNow;
        lastN = n;
    }
}
catch (OperationCanceledException) { }
finally
{
    try { await client.DisconnectAsync().ConfigureAwait(false); } catch { }
    Console.WriteLine($"final total={Interlocked.Read(ref total)}");
}

return 0;
