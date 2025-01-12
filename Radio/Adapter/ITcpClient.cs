using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Radio.Adapter
{
    public interface ITcpClient
    {
        void Connect(string host, int port);
        Stream GetStream(); 
        bool Connected { get; }
        void Close();
    }
}
