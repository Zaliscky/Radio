using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radio.Receiver
{
    public class NetSdrMessageHandler : INetSdrMessageHandler
    {
        public async Task<byte[]> FrequencyCommand(double frequency, byte channelId)
        {
            return await Task.Run(() =>
            {
                // Перевод частоты в 40-битное число Гц
                // документация NetSDR говорит, что частота передаётся в герцах Hz)
                // а в методе SetFrequency частота передаётся MHz
                // Followed by a 5 byte frequency value in Hz (40 bit unsigned integer LSB first)
                long freqHz = (long)(frequency * 1e6);

                // Метод BitConverter.GetBytes(freqHz)  возвращает число в Little Endian формате.
                // Но он возвращает 8 байтов, из которых нужно оставить только первые 5.
                //  Little Endian потому что документация требует, чтобы младший значащий байт шёл первым LSB first
                byte[] freqBytes = BitConverter.GetBytes(freqHz);

                // Приводим к Little Endian (LSB первым)
                // Оставляем до (40 бит) согласно документации:
                // 4.2.3 Receiver Frequency 40 bit unsigned integer LSB first
                // Followed by a 5 byte frequency value in Hz (40 bit unsigned integer LSB first)
                Array.Resize(ref freqBytes, 5);

                // из документации 4.2.3 Receiver Frequency The host sends this: [0A][00] [20][00] [00] [90][C6][D5][00][00]
                byte[] command = new byte[10];
                command[0] = 0x0A;
                command[1] = 0x00;
                command[2] = 0x20;
                command[3] = 0x00;
                command[4] = channelId;
                Array.Copy(freqBytes, 0, command, 5, 5);
                return command;
            });
        }

        public async Task HandleResponse(byte[] response, int bytesRead)
        {
            await Task.Run(() =>
            {
                //В документации (пункт "3.2. The ACK and NAK Messages and Their Purpose") NAK формат [02][00] 
                //Из документации
                //A "NAK" message is a 16 bit header without a Control Item or parameters (Message length of 2) [02][00] 
                if (bytesRead == 2 && response[0] == 0x02 && response[1] == 0x00)
                {
                    Console.WriteLine("NAK : y Host message requesting an unimplemented Control Item\n");
                }
                //В документации (пункт "3.2. The ACK and NAK Messages and Their Purpose") ACK формат [03][60] [02]
                //Из документации
                //A Data Item "ACK" message is a 16 bit header with a Message Type = 011b
                //with a single parameter byte specifying the Data Item(0 to 3)
                //For example if the Target received a block of Data Item 2 data correctly 
                //it could send the following back to the Host: [03][60] [02]

                else if (bytesRead == 3 && response[0] == 0x03 && response[1] == 0x60)
                {
                    byte dataItem = response[2];
                    //в документации сказано что значение data item от 0 до 3
                    // a single parameter byte specifying  the Data Item(0 to 3).
                    if (dataItem >= 0x00 && dataItem <= 0x03)
                    {
                        Console.WriteLine($"ACK message received with Data Item {dataItem}\n");
                    }
                    else
                    {
                        Console.WriteLine($" Wrong format for ACK data {dataItem}");
                    }
                }
                // Из документации младшие 3 бита второго байта содержат значение поля Msg Type field.
                // младшие 3 бита второго байта (Msg Type field) определяют тип сообщения.
                // что б извлечь эти 3 бита мы используем '& 0x07' 
                // The message type field is used by the receiving side to determine how to process this message block.
                // It has a different meaning depending upon whether the message is from the Host or Target.
                // 001 ------ Unsolicited Control Item
                else if (bytesRead > 2 && (response[1] & 0x07) == 0x01)
                {
                    HandleUnsolicitedControlItem(response, bytesRead);
                }
                else
                {
                    Console.WriteLine($"{BitConverter.ToString(response, 0, bytesRead)}\n");
                }
            });
        }

        public async Task HandleUnsolicitedControlItem(byte[] response, int length)
        {
            await Task.Run(() => { 
            try
            {
                if (length < 4)
                {
                    //по документации минимальная длина Unsolicited Control Item 4 байта
                    Console.WriteLine(" Minimal length of Unsolicited Control Item is 4!!!");
                    return;
                }

                //что б извлечь Код Unsolicited Control Item, вытаскиваем после загаловка начиная с байта с индексом 2 в Little Endian формате
                //The basic message structure starts with a 16 bit header that contains the length of the block in bytes
                //If the message is a Control Item, then a 16 bit Control Item code follows the header
                // The byte order for all fields greater than 8 bits is "Little Endian" or least significant byte first.
                var controlItemCode = BitConverter.ToUInt16(response, 2);
                byte[] parameters = new byte[length - 4];
                Array.Copy(response, 4, parameters, 0, parameters.Length);
                Console.WriteLine($"Extracted Unsolicited Control Item: {controlItemCode}\n");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Handle Unsolicited Control Item unexpected error\n");
            }
        });
        }
    }
}
