using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;

public class UdpVideoClient : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    public bool isServerConnected = false;

    public Action<byte[]> OnImageReceived;

    public void StartUDPClient(string ipAddress, int port)
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        udpClient.BeginReceive(ReceiveImage, null);
        SendHandshake();
        isServerConnected = true;
    }

    private void ReceiveImage(IAsyncResult result)
    {
        Debug.Log("Receiving image...");
        byte[] receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        Debug.Log("Received image: " + receivedBytes.Length);

        if (receivedBytes != null && receivedBytes.Length > 0)
        {
            OnImageReceived?.Invoke(receivedBytes);
        }
        udpClient.BeginReceive(ReceiveImage, null);
    }

    public void SendHandshake()
    {
        byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes("Hi");
        udpClient.Send(sendBytes, sendBytes.Length, remoteEndPoint);
    }

}
