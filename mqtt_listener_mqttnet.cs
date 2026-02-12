#:package MQTTnet@5.1.0.1559

using MQTTnet;
using MQTTnet.Protocol;

var host = "localhost";
var port = 1883;
var topic = "stresstest";

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var factory = new MqttClientFactory();
using var client = factory.CreateMqttClient();

long total = 0;
client.ApplicationMessageReceivedAsync += _ => { Interlocked.Increment(ref total); return Task.CompletedTask; };

var ob = new MqttClientOptionsBuilder()
    .WithTcpServer(host, port)
    .WithClientId($"mqttnet-{Guid.NewGuid():N}")
    .WithCleanStart(true)
    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);

var opts = ob.Build();

await client.ConnectAsync(opts, cts.Token);

var sub = factory.CreateSubscribeOptionsBuilder();
sub.WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce));
await client.SubscribeAsync(sub.Build(), cts.Token);

Console.WriteLine($"connected {host}:{port} topic={topic}");

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
    try { await client.DisconnectAsync(); } catch { }
    Console.WriteLine($"final total={Interlocked.Read(ref total)}");
}
