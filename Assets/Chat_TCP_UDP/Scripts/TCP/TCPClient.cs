using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPClient : MonoBehaviour, IClient
{
    private TcpClient tcpClient;
    private NetworkStream networkStream;

    public bool isConnected { get; private set; }

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public async Task ConnectToServer(string ip, int port)
    {
        tcpClient = new TcpClient();

        await tcpClient.ConnectAsync(ip, port);
        networkStream = tcpClient.GetStream();

        isConnected = true;
        Debug.Log("[Client] Connected to server");
        OnConnected?.Invoke();

        _ = ReceiveLoop();
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (tcpClient != null && tcpClient.Connected)
            {
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Debug.Log("[Client] Server disconnected");
                    break;
                }
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OnMessageReceived?.Invoke(message);
                Debug.Log("[Client] Received from server: " + message);
            }
        }
        finally
        {
            Disconnect();
        }
    }
    public async Task SendMessageAsync(string message)
    {
        if (!isConnected || networkStream == null)
        {
            Debug.Log("[Client] Not connected to server");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);
        await networkStream.WriteAsync(data, 0, data.Length);

        Debug.Log("[Client] Sent: " + message);
    }

    public void Disconnect()
    {
        isConnected = false;

        networkStream?.Close();
        tcpClient?.Close();

        networkStream = null;
        tcpClient = null;

        OnDisconnected?.Invoke();
        Debug.Log("[Client] Disconnected");
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}
