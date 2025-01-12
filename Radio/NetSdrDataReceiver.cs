using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Radio
{
    public class NetSdrDataReceiver
    {
        // 4.5.1 NetSDR Output Data
        //Large MTU packet 1024 bytes.
        //Small MTU packet 512 bytes.
        private int _port = 60000;
        private int _largePacket = 1024;
        private int _smallPacket = 512;

        public async Task StartReceiving(string outputFilePath, int timeoutMilliseconds = 5000, CancellationToken cancellationToken = default)
        {
            using (var udpClient = new UdpClient(_port))
            using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                udpClient.Client.ReceiveTimeout = timeoutMilliseconds;
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var receiveTask = udpClient.ReceiveAsync();
                            var completedTask = await Task.WhenAny(receiveTask, Task.Delay(timeoutMilliseconds, cancellationToken));

                            if (completedTask == receiveTask)
                            {
                                var result = await receiveTask;
                                byte[] receivedData = result.Buffer;
                                IPEndPoint remoteEndPoint = result.RemoteEndPoint;


                                //4.5.1.2 Complex 16 Bit Data  If the small MTU packet is specified and If the large MTU packet is specified
                                if (receivedData.Length == _largePacket || receivedData.Length == _smallPacket)
                                {
                                    byte header1 = receivedData[0];
                                    byte header2 = receivedData[1];

                                    // Проверяем, что данные в формате Complex 16 Bit Data
                                    if (header1 == 0x04 && header2 == 0x84 || header1 == 0x04 && header2 == 0x82)
                                    {
                                        int dataOffset = 4;
                                        int dataLength = receivedData.Length - dataOffset;
                                        byte[] iqData = new byte[dataLength];
                                        Array.Copy(receivedData, dataOffset, iqData, 0, dataLength);

                                        // recording I/Q 
                                        await fileStream.WriteAsync(iqData, 0, iqData.Length, cancellationToken);
                                        Console.WriteLine($"Received {dataLength / 2} I/Q samples from {remoteEndPoint}\n");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Wrong format: {BitConverter.ToString(receivedData, 0, 2)}\n");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Wrong size: {receivedData.Length}\n");
                                }
                            }
                        }
                        catch (SocketException ex)
                        {
                            if (ex.SocketErrorCode == SocketError.TimedOut)
                            {
                                Console.WriteLine(" I/Q Data Transfer TimedOut");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"Unpredicted Error: {ex.Message}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Data trasfer error: {ex.Message}");
                }
            }
        }
    }
}

