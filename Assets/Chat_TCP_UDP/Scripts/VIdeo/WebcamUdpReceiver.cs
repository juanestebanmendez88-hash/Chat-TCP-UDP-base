using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Recibe los trozos UDP del servidor, rearma cada frame (JPG) y lo muestra como
// video en un RawImage. El chat sigue por TCP sin enterarse de esto.
public class WebcamUdpReceiver : MonoBehaviour
{
    [Header("Red")]
    public string serverIp = "127.0.0.1";
    public int serverPort = 5556;

    [Header("UI")]
    public RawImage videoDisplay;
    public TMP_Text infoLabel;             // opcional: "UDP 5556 - 18 FPS - 640x480"

    private UdpClient socket;
    private IPEndPoint serverEndPoint;

    // Trozos que se estan juntando, indexados por numero de frame.
    private readonly Dictionary<int, FrameAssembly> assembling = new Dictionary<int, FrameAssembly>();
    private readonly List<int> toRemove = new List<int>();
    private int lastShownFrame;
    private volatile int framesReceived;

    private readonly object gate = new object();
    private byte[] pendingJpg;              // ultimo frame completo, esperando a pintarse

    private Texture2D texture;

    private int framesThisSecond;
    private int currentFps;
    private float fpsTimer;
    private bool connected;

    private const int HeaderSize = 8;      // 4 bytes frameId + 2 chunkIndex + 2 chunkCount

    void Start()
    {
        texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        UpdateInfoLabel();
    }

    // Se dispara desde el mismo boton "Connect to Server" del chat (segunda entrada en On Click).
    public void Connect()
    {
        if (socket != null) return;           // ya conectado

        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        socket = new UdpClient();             // puerto local automatico
        IgnoreUdpConnReset(socket);
        socket.BeginReceive(OnPacket, null);

        connected = true;
        UpdateInfoLabel();
        StartCoroutine(SayHelloUntilConnected());
        Debug.Log("[VideoRX] Conectando al video en " + serverEndPoint);
    }

    // Windows: sin esto, si un datagrama "rebota" (el puerto destino aun no escucha),
    // el siguiente Receive lanza SocketException 10054 y mata el bucle de recepcion.
    private static void IgnoreUdpConnReset(UdpClient client)
    {
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            client.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch { /* otra plataforma: no aplica */ }
    }

    // UDP no garantiza entrega: repetimos el saludo hasta que empiecen a llegar frames.
    private IEnumerator SayHelloUntilConnected()
    {
        byte[] hello = System.Text.Encoding.UTF8.GetBytes("HELLO");
        while (framesReceived == 0)
        {
            socket.Send(hello, hello.Length, serverEndPoint);
            yield return new WaitForSeconds(0.5f);
        }
        Debug.Log("[VideoRX] Recibiendo video");
    }

    private void OnPacket(IAsyncResult ar)
    {
        try
        {
            IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = socket.EndReceive(ar, ref from);
            HandlePacket(data);
        }
        catch (ObjectDisposedException) { return; }   // socket cerrado: no re-armar
        catch (Exception e) { Debug.LogWarning("[VideoRX] " + e.Message); }

        try { socket.BeginReceive(OnPacket, null); }
        catch (ObjectDisposedException) { }
    }

    private void HandlePacket(byte[] data)
    {
        if (data == null || data.Length < HeaderSize) return;

        int frameId    = BitConverter.ToInt32(data, 0);
        int chunkIndex = BitConverter.ToUInt16(data, 4);
        int chunkCount = BitConverter.ToUInt16(data, 6);
        if (chunkCount == 0 || chunkIndex >= chunkCount) return;
        if (frameId <= lastShownFrame) return;             // frame viejo, ya mostramos uno mas nuevo

        if (!assembling.TryGetValue(frameId, out FrameAssembly f))
        {
            f = new FrameAssembly(chunkCount);
            assembling[frameId] = f;
            Prune(frameId - 4);                            // no acumular incompletos viejos
        }

        if (f.parts[chunkIndex] == null)
        {
            int payloadLen = data.Length - HeaderSize;
            byte[] payload = new byte[payloadLen];
            Buffer.BlockCopy(data, HeaderSize, payload, 0, payloadLen);
            f.parts[chunkIndex] = payload;
            f.filled++;
        }

        if (f.filled == f.total)
        {
            byte[] full = f.ToArray();
            assembling.Remove(frameId);
            lastShownFrame = frameId;
            framesReceived++;
            Prune(frameId);

            lock (gate) { pendingJpg = full; }
        }
    }

    // Bota del diccionario los frames con numero menor al umbral.
    private void Prune(int minKeep)
    {
        toRemove.Clear();
        foreach (KeyValuePair<int, FrameAssembly> kv in assembling)
            if (kv.Key < minKeep) toRemove.Add(kv.Key);
        foreach (int k in toRemove) assembling.Remove(k);
    }

    void Update()
    {
        fpsTimer += Time.deltaTime;
        if (fpsTimer >= 1f)
        {
            currentFps = framesThisSecond;
            framesThisSecond = 0;
            fpsTimer = 0f;
            UpdateInfoLabel();
        }

        byte[] jpg = null;
        lock (gate)
        {
            if (pendingJpg != null) { jpg = pendingJpg; pendingJpg = null; }
        }
        if (jpg == null) return;

        if (texture.LoadImage(jpg) && videoDisplay != null)
        {
            videoDisplay.texture = texture;
            framesThisSecond++;
        }
    }

    // Estado que ve el usuario en el panel del cliente.
    private void UpdateInfoLabel()
    {
        if (infoLabel == null) return;

        if (!connected)
            infoLabel.text = "Desconectado";
        else if (currentFps > 0)
            infoLabel.text = "Transmitiendo - " + currentFps + " FPS";
        else if (framesReceived > 0)
            infoLabel.text = "Transmision detenida";
        else
            infoLabel.text = "Conectado - esperando video";
    }

    void OnDestroy()
    {
        connected = false;
        socket?.Close();
    }

    // Guarda los trozos de un frame hasta tenerlos todos.
    private class FrameAssembly
    {
        public readonly byte[][] parts;
        public readonly int total;
        public int filled;

        public FrameAssembly(int count)
        {
            total = count;
            parts = new byte[count][];
        }

        public byte[] ToArray()
        {
            int size = 0;
            for (int i = 0; i < parts.Length; i++) size += parts[i].Length;

            byte[] result = new byte[size];
            int offset = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                Buffer.BlockCopy(parts[i], 0, result, offset, parts[i].Length);
                offset += parts[i].Length;
            }
            return result;
        }
    }
}
