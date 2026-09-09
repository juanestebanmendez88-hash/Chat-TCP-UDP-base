using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class UdpVideoServer : MonoBehaviour
{
    private UdpClient udpServer;
    private IPEndPoint remoteEndPoint;

    public bool isServerRunning = false;

    public void StartUDPServer(int port)
    {
        udpServer = new UdpClient(port);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        Debug.Log("Server started. Waiting for client Handshake");
        udpServer.BeginReceive(ReceiveHandshake, null);
        isServerRunning = true;
    }

    private void ReceiveHandshake(IAsyncResult result)
    {
        byte[] receivedBytes = udpServer.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = System.Text.Encoding.UTF8.GetString(receivedBytes);
        Debug.Log("Received handshake from client: " + remoteEndPoint);
    }

    public void SendImage(Texture2D texture, int jpgQuality = 30)
    {
        byte[] jpgBytes = texture.EncodeToJPG(jpgQuality);
        try
        {
            udpServer.Send(jpgBytes, jpgBytes.Length, remoteEndPoint);
            Debug.Log($"[TX] enviado {jpgBytes.Length} bytes");
        }
        catch (SocketException ex)
        {
            Debug.LogError($"Error al enviar UDP: {ex.SocketErrorCode} - {ex.Message}");
        }
    }
}
