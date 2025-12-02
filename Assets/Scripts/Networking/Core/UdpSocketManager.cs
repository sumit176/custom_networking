using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace CustomNetworking.Core
{
    /// <summary>
    /// Low-level UDP socket manager with thread-safe async I/O
    /// </summary>
    public class UdpSocketManager : IDisposable
    {
        private Socket socket;
        private Thread receiveThread;
        private bool isRunning;
        
        private ConcurrentQueue<ReceivedPacket> receiveQueue;
        private ConcurrentQueue<OutgoingPacket> sendQueue;
        
        public int Port { get; private set; }
        public bool IsRunning => isRunning;

        public struct ReceivedPacket
        {
            public byte[] Data;
            public int Length;
            public IPEndPoint RemoteEndPoint;
        }

        public struct OutgoingPacket
        {
            public byte[] Data;
            public int Length;
            public IPEndPoint RemoteEndPoint;
        }

        public UdpSocketManager()
        {
            receiveQueue = new ConcurrentQueue<ReceivedPacket>();
            sendQueue = new ConcurrentQueue<OutgoingPacket>();
        }

        public bool Start(int port)
        {
            if (isRunning)
                return false;

            try
            {
                Port = port;
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Blocking = false;
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                
                isRunning = true;
                receiveThread = new Thread(ReceiveThreadLoop);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"UdpSocketManager: Failed to start on port {port}: {ex.Message}");
                return false;
            }
        }

        public bool Connect(string host, int port)
        {
            if (isRunning)
                return false;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Blocking = false;
                socket.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind to any available port
                
                Port = ((IPEndPoint)socket.LocalEndPoint).Port;
                
                isRunning = true;
                receiveThread = new Thread(ReceiveThreadLoop);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"UdpSocketManager: Failed to create socket: {ex.Message}");
                return false;
            }
        }
        
        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000);
            }
            
            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch { }
                socket = null;
            }
            
            // Clear queues
            while (receiveQueue.TryDequeue(out _)) { }
            while (sendQueue.TryDequeue(out _)) { }
        }

        public void Send(byte[] data, int length, IPEndPoint remoteEndPoint)
        {
            if (!isRunning || socket == null)
                return;

            sendQueue.Enqueue(new OutgoingPacket
            {
                Data = data,
                Length = length,
                RemoteEndPoint = remoteEndPoint
            });
        }
        
        public bool TryReceive(out ReceivedPacket packet)
        {
            return receiveQueue.TryDequeue(out packet);
        }

        public void ProcessSendQueue()
        {
            if (!isRunning || socket == null)
                return;

            while (sendQueue.TryDequeue(out OutgoingPacket packet))
            {
                try
                {
                    socket.SendTo(packet.Data, 0, packet.Length, SocketFlags.None, packet.RemoteEndPoint);
                }
                catch (SocketException ex)
                {
                    // Ignore ICMP port unreachable and other common errors
                    if (ex.SocketErrorCode != SocketError.ConnectionReset &&
                        ex.SocketErrorCode != SocketError.MessageSize)
                    {
                        UnityEngine.Debug.LogWarning($"UdpSocketManager: Send error: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"UdpSocketManager: Send exception: {ex.Message}");
                }
            }
        }

        private void ReceiveThreadLoop()
        {
            byte[] buffer = new byte[2048];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning && socket != null)
            {
                try
                {
                    if (socket.Available > 0)
                    {
                        int receivedBytes = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEndPoint);
                        
                        if (receivedBytes > 0)
                        {
                            byte[] data = new byte[receivedBytes];
                            Array.Copy(buffer, 0, data, 0, receivedBytes);
                            
                            receiveQueue.Enqueue(new ReceivedPacket
                            {
                                Data = data,
                                Length = receivedBytes,
                                RemoteEndPoint = (IPEndPoint)remoteEndPoint
                            });
                        }
                    }
                    else
                    {
                        Thread.Sleep(1); // Prevent tight loop when no data
                    }
                }
                catch (SocketException ex)
                {
                    // Ignore common errors
                    if (ex.SocketErrorCode != SocketError.WouldBlock &&
                        ex.SocketErrorCode != SocketError.ConnectionReset)
                    {
                        UnityEngine.Debug.LogWarning($"UdpSocketManager: Receive error: {ex.Message}");
                    }
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"UdpSocketManager: Receive exception: {ex.Message}");
                    Thread.Sleep(10);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

