using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Radio.Receiver;

namespace Radio.Client
{
    public class NetSdrClient : INetSdrClient
    {
        private readonly string _host;
        private readonly int _port;
        private NetworkStream _stream;
        private readonly INetSdrMessageHandler _messageHandler;
        private TcpClient _client;
        public NetSdrClient(string host, int port, INetSdrMessageHandler messageHandler)
        {
            _host = host;
            _port = port;
            _messageHandler = messageHandler;
        }

        public async Task<bool> Connect()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port);
                _stream = _client.GetStream();
                Console.WriteLine($"Connection established to NetSDR  {_host}:{_port} with success !\n");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed {ex.Message}\n");
                return false;
            }
        }
        public async Task Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                await Task.CompletedTask;
                Console.WriteLine("Closed connection\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect {ex.Message}\n");
            }
        }
        private void CheckConnection()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Client is null\n");
            }
            if (_stream == null)
            {
                throw new InvalidOperationException("stream is null\n");
            }
            if (_client.Connected && _stream == null)
            {
                throw new InvalidOperationException("client is connected, but stream is null\n");
            }
            if (!_client.Connected && _stream != null)
            {
                throw new InvalidOperationException("stream exists, connection is missed\n");
            }
            if (!_client.Connected)
            {
                throw new InvalidOperationException("Client is not connected\n");
            }
        }
        //Согласно документации 4.2.1 Receiver State
        //Example: Request to start the NetSDR capturing data in the complex I/Q base band contiguous 24 bit mode.
        // The host sends:[08][00][18][00][80][02][80][00]
        public async Task StartIQDataTransfer()
        {
            CheckConnection();
            byte[] command = { 0x08, 0x00, 0x18, 0x00, 0x80, 0x02, 0x80, 0x00 };
            await CommandTransfer(command);
        }

        //Согласно документации 4.2.1 Receiver State параметры 1,3,4 игнорируются
        public async Task StopIQDataTransfer()
        {
            CheckConnection();
            byte[] command = { 0x08, 0x00, 0x18, 0x00, 0x00, 0x01, 0x00, 0x00 };
            await CommandTransfer(command);
        }

        public async Task SetChannelMode(byte mode)
        {
            CheckConnection();
            // По документации 4.2.2 Receiver Channel Setup режим канала до 6
            if (mode > 6)
                throw new ArgumentOutOfRangeException(nameof(mode), "Receiver Channel Setup supports 0-6 values\n");
            byte[] command = { 0x05, 0x00, 0x19, 0x00, mode };
            await CommandTransfer(command);
        }
        public async Task SetFrequency(double frequency, byte channelId = 0x00)
        {
            CheckConnection();
            //диапазон частот взят из 4.2.3 Receiver Frequence
            //Radio with a 100KHz to 34MHz and 140MHz to 150MHz capability responds with min and max frequency
            if (frequency < 0.1 || frequency > 34 && frequency < 140 || frequency > 150)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), "Invalid frequncy value, supports only Radio with a 100KHz to 34MHz and 140MHz to 150MHz capability\n");
            }
            //По документации 4.2.3 Receiver Frequency 
            //Selects which channel to set or set all the same frequency(0xFF)
            //Channel 1 ID == 0x00 and Channel 2 ID == 0x02
            if (channelId != 0x00 && channelId != 0x02 && channelId != 0xFF)
                throw new ArgumentOutOfRangeException(nameof(channelId), "Invalid channel ID\n");
            byte[] command = await _messageHandler.FrequencyCommand(frequency, channelId);
            await CommandTransfer(command);
        }

        private async Task CommandTransfer(byte[] command)
        {
            try
            {
                await _stream.WriteAsync(command, 0, command.Length);
                //по документации 4.5.1 NetSDR Output Data максимум 1024 
                byte[] response = new byte[1024];
                int getBytes = await _stream.ReadAsync(response, 0, response.Length);
                await _messageHandler.HandleResponse(response, getBytes);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error during command transfer {ex.Message}\n");
                throw new InvalidOperationException("Command transfer failed\n", ex);
            }
        }


    }
}
