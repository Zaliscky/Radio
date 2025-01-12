using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Radio.Client;
using Radio.Receiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radio
{
    [MemoryDiagnoser]
    public class NetSdrBenchmarks
    {
        private INetSdrClient _client;
        [GlobalSetup]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            services.AddTransient<INetSdrMessageHandler, NetSdrMessageHandler>();
            services.AddTransient<INetSdrClient>(provider =>
            {
                return new NetSdrClient("198.168.0.99", 50000, provider.GetRequiredService<INetSdrMessageHandler>());
            });
            var serviceProvider = services.BuildServiceProvider();
            _client = serviceProvider.GetRequiredService<INetSdrClient>();
            await _client.Connect();
        }

        [Benchmark]
        public async Task BenchmarkEstablishConnection()
        {
            await _client.Connect();
        }

        [Benchmark]
        public async Task BenchmarkSetFrequency()
        {
            await _client.SetFrequency(14.01, 0x00); // Частота и канал
        }

        [Benchmark]
        public async Task BenchmarkStartIQDataTransfer()
        {
            await _client.StartIQDataTransfer();
        }

        [Benchmark]
        public async Task BenchmarkStopIQDataTransfer()
        {
            await _client.StopIQDataTransfer();
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _client.Disconnect();
        }
    }
}
