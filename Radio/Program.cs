using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Radio;
using Radio.Client;
using Radio.Receiver;

BenchmarkRunner.Run<NetSdrBenchmarks>();
var services = new ServiceCollection();

services.AddTransient<INetSdrMessageHandler, NetSdrMessageHandler>();
services.AddTransient<INetSdrClient>(provider =>
{
    return new NetSdrClient("198.168.0.99", 50000, provider.GetRequiredService<INetSdrMessageHandler>());
});
services.AddTransient<NetSdrDataReceiver>();
var serviceProvider = services.BuildServiceProvider();
INetSdrClient client = null;
try
{

    client = serviceProvider.GetRequiredService<INetSdrClient>();
    var receiver = serviceProvider.GetRequiredService<NetSdrDataReceiver>();

    if (!await client.Connect())
    {
        Console.WriteLine("Error during connection to NetSDR.");
        return;
    }
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    await client.SetChannelMode(0x00); // Одноканальный режим
    await client.SetFrequency(14.01, 0x00); // Частота 14.01 МГц для канала 1
    await client.StartIQDataTransfer();

    string outputFilePath = "output_iq_samples.bin";
    Console.WriteLine($"Recording to file {outputFilePath}");

    var receiveTask = receiver.StartReceiving(outputFilePath, 10000, cancellationTokenSource.Token);

  
    await client.StopIQDataTransfer();

    cancellationTokenSource.Cancel();
    await receiveTask;
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
finally
{   
  await client?.Disconnect();
}