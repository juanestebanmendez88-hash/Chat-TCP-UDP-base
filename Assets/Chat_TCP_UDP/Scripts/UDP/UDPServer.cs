using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UDPServer : MonoBehaviour, IServer
{
    private UdpClient udpServer;
    private IPEndPoint remoteEndPoint;

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public bool isServerRunning { get; private set; }

    public Task StartServer(int port)
    {
        udpServer = new UdpClient(port);
        Debug.Log("[Server] Server started. Waiting for messages...");
        isServerRunning = true;

        _ = ReceiveLoop();
        return Task.CompletedTask;
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (isServerRunning)
            {
                UdpReceiveResult result = await udpServer.ReceiveAsync();
                string message = Encoding.UTF8.GetString(result.Buffer);

                if (message == "CONNECT")
                {
                    Debug.Log("[Server] Client connected: " + result.RemoteEndPoint);
                    remoteEndPoint = result.RemoteEndPoint;
                    await SendMessageAsync("CONNECTED");
                    OnConnected?.Invoke();
                    continue;
                }

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
        if (!isServerRunning)
        {
            Debug.Log("[Server] The server isn´t running");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);
        await udpServer.SendAsync(data, data.Length, remoteEndPoint);

        Debug.Log("[Server] Sent: " + message);
    }

    public void Disconnect()
    {
        if (!isServerRunning)
        {
            Debug.Log("[Server] The server is not running");
            return;
        }

        isServerRunning = false;

        udpServer?.Close();
        udpServer?.Dispose();
        udpServer = null;

        Debug.Log("[Server] Disconnected");
        OnDisconnected?.Invoke();

    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}
