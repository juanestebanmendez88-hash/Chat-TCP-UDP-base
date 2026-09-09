using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;

public class VideoReceiver : MonoBehaviour
{
    public UdpVideoClient udpClient;
    public RawImage videoDisplay;

    public string serverIp = "127.0.0.1";
    public int serverPort = 5000;

    public Texture2D texture;

    private readonly ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();

    public void ConnectToServer()
    {
        texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        udpClient.StartUDPClient(serverIp, serverPort);
        udpClient.OnImageReceived += EnqueueFrame;
    }

    private void Update()
    {
        if (frameQueue.TryDequeue(out var bytes))
        {
            if (texture == null)
                texture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            if (texture.LoadImage(bytes, true))
                videoDisplay.texture = texture;
        }
    }

    private void EnqueueFrame(byte[] imageInfo)
    {
        frameQueue.Enqueue(imageInfo);
    }
}
