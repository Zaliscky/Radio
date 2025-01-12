using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Radio.Adapter
{
    public class TcpClientAdapter : ITcpClient
    {
        private readonly TcpClient _tcpClient;
        public TcpClientAdapter(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public bool Connected => _tcpClient.Connected;
        public void Close() => _tcpClient.Close();
        public Stream GetStream() => _tcpClient.GetStream(); 
        public void Connect(string host, int port)
        {
            _tcpClient.Connect(host, port);
        }
    }
}
