using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radio.Client
{
    public interface INetSdrClient
    {
        Task<bool> Connect();
        Task Disconnect();
        Task StartIQDataTransfer();
        Task StopIQDataTransfer();
        Task SetChannelMode(byte mode);
        Task SetFrequency(double frequency, byte channelId = 0x00);
    }
}
