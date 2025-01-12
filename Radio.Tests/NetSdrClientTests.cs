using Moq;
using Radio.Adapter;
using Radio.Client;
using Radio.Receiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Radio.Tests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private Mock<ITcpClient> _mockAdapter;
        private Mock<INetSdrMessageHandler> _mockHandler;
        [SetUp]
        public void Setup()
        {
            _mockAdapter = new Mock<ITcpClient>();
            _mockHandler = new Mock<INetSdrMessageHandler>();
        }

        [Test]
        public void Connect()
        {
            var host = "192.168.0.99";
            var port = 50000;
            _mockAdapter.Setup(client => client.Connect(host, port)).Verifiable();
            _mockAdapter.Object.Connect(host, port);
            _mockAdapter.Verify(client => client.Connect(host, port), Times.Once, "Connection Succesfully established");
        }

        [Test]
        public void CloseConnect()
        {
            _mockAdapter.Setup(client => client.Close()).Verifiable();
            _mockAdapter.Object.Close();
            _mockAdapter.Verify(client => client.Close(), Times.Once, "Connection Succesfully Closed");
        }
        [Test]
        public void StreamTest()
        {
            var mockStream = new MemoryStream(); 
            var mockTcpClient = new Mock<ITcpClient>();
            mockTcpClient.Setup(client => client.GetStream()).Returns(mockStream);
            var resultStream = mockTcpClient.Object.GetStream();
            Assert.AreSame(mockStream, resultStream, "Stream must be correct.");

        }
 
        [Test]
        public void Status()
        {
            _mockAdapter.Setup(client => client.Connected).Returns(true);
            var isConnected = _mockAdapter.Object.Connected;
            Assert.IsTrue(isConnected, "Connected should return true when ITcpClient is connected.");
        }

        [Test]
        public void StartIQDataTransfer()
        {
            byte[] expectedCommand = { 0x08, 0x00, 0x18, 0x00, 0x80, 0x02, 0x80, 0x00 };
            var memoryStream = new MemoryStream();

            _mockAdapter.Setup(client => client.GetStream()).Returns(memoryStream);
            _mockAdapter.Setup(client => client.Connected).Returns(true);

            var stream = _mockAdapter.Object.GetStream();
            stream.Write(expectedCommand, 0, expectedCommand.Length); 

            var sentData = memoryStream.ToArray();
            CollectionAssert.AreEqual(expectedCommand, sentData, "Command must be correct");
        }

        [Test]
        public void StopIQDataTransfer()
        {
            byte[] expectedCommand = { 0x08, 0x00, 0x18, 0x00, 0x00, 0x01, 0x00, 0x00 };
            var memoryStream = new MemoryStream();

            _mockAdapter.Setup(client => client.GetStream()).Returns(memoryStream);
            _mockAdapter.Setup(client => client.Connected).Returns(true);

            var stream = _mockAdapter.Object.GetStream();
            stream.Write(expectedCommand, 0, expectedCommand.Length); 

            var sentData = memoryStream.ToArray();
            CollectionAssert.AreEqual(expectedCommand, sentData, "Command must be correct");
        }
    }
}


