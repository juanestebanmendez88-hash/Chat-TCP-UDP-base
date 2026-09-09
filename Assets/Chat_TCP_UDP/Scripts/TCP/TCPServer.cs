using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPServer : MonoBehaviour, IServer
{
    private TcpListener tcpListener;
    private TcpClient connectedClient;
    private NetworkStream networkStream;

    public bool isServerRunning { get; private set; }

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public async Task StartServer(int port)
    {
        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();

        Debug.Log("[Server] Server started, waiting for connections...");
        isServerRunning = true;

        connectedClient = await tcpListener.AcceptTcpClientAsync();
        Debug.Log("[Server] Client connected: " + connectedClient.Client.RemoteEndPoint);
        OnConnected?.Invoke();

        networkStream = connectedClient.GetStream();
        _ = ReceiveLoop();
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (connectedClient != null && connectedClient.Connected)
            {
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Debug.Log("[Server] Client disconnected");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("[Server] Received: " + message);
                OnMessageReceived?.Invoke(message);
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (networkStream == null || !connectedClient.Connected)
        {
            Debug.Log("[Server] No client connected");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);
        await networkStream.WriteAsync(data, 0, data.Length);

        Debug.Log("[Server] Sent: " + message);
    }

    public void Disconnect()
    {
        networkStream?.Close();
        connectedClient?.Close();

        networkStream = null;
        connectedClient = null;

        Debug.Log("[Server] Disconnected");
        OnDisconnected?.Invoke();
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}
