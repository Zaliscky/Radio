using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radio.Receiver
{
    public interface INetSdrMessageHandler
    {
        Task HandleUnsolicitedControlItem(byte[] response, int length);
        Task HandleResponse(byte[] response, int bytesRead);
        Task<byte[]> FrequencyCommand(double frequency, byte channelId);
    }
}
