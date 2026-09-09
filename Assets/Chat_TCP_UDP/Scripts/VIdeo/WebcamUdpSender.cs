using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WebcamUdpSender : MonoBehaviour
{
    [Header("Red")]
    public int port = 5556;

    [Header("Captura")]
    public int requestedWidth = 640;
    public int requestedHeight = 480;
    public int fps = 20;
    [Range(5, 90)] public int jpegQuality = 40;

    [Header("UI (opcional)")]
    public RawImage localPreview;
    public TMP_Text streamButtonLabel;
    public TMP_Text statusLabel;

    [Header("Debug")]
    [SerializeField] private bool streaming;

    private UdpClient socket;
    private volatile IPEndPoint clientEndPoint;
    private WebCamTexture webcam;
    private Texture2D frameTexture;
    private Color32[] pixelBuffer;

    private int frameId;
    private float sendTimer;
    private float statusTimer;

    private const int HeaderSize = 8;
    private const int MaxPayload = 1200;

    public void StartServer()
    {
        if (socket != null) return;

        socket = new UdpClient(port);
        IgnoreUdpConnReset(socket);
        socket.BeginReceive(OnHello, null);
        StartCoroutine(SetupCamera());
        Debug.Log("[VideoTX] Servidor de video en UDP " + port);
    }

    private static void IgnoreUdpConnReset(UdpClient client)
    {
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            client.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch { }
    }

    private IEnumerator SetupCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError("[VideoTX] Sin permiso para usar la camara.");
            yield break;
        }
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("[VideoTX] No se detecto ninguna webcam.");
            yield break;
        }

        webcam = new WebCamTexture(requestedWidth, requestedHeight, fps);
        webcam.Play();
        if (localPreview != null) localPreview.texture = webcam;

        while (webcam.width <= 16) yield return null;

        frameTexture = new Texture2D(webcam.width, webcam.height, TextureFormat.RGB24, false);
        pixelBuffer = new Color32[webcam.width * webcam.height];
        Debug.Log($"[VideoTX] Webcam lista {webcam.width}x{webcam.height}");
    }

    private void OnHello(IAsyncResult ar)
    {
        try
        {
            IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
            socket.EndReceive(ar, ref from);
            clientEndPoint = from;
            Debug.Log("[VideoTX] Cliente conectado: " + from);
        }
        catch (ObjectDisposedException) { return; }
        catch (Exception e) { Debug.LogWarning("[VideoTX] " + e.Message); }

        try { socket.BeginReceive(OnHello, null); }
        catch (ObjectDisposedException) { }
    }

    public void ToggleStreaming() { SetStreaming(!streaming); }
    public void StartStreaming()  { SetStreaming(true); }
    public void StopStreaming()   { SetStreaming(false); }

    private void SetStreaming(bool on)
    {
        streaming = on;
        if (streamButtonLabel != null)
            streamButtonLabel.text = on ? "Detener transmision" : "Iniciar transmision";
        statusTimer = 1f;
    }

    void Update()
    {
        UpdateStatus();

        if (!streaming || frameTexture == null || clientEndPoint == null) return;

        sendTimer += Time.deltaTime;
        if (sendTimer < 1f / fps) return;
        sendTimer = 0f;

        SendCurrentFrame();
    }

    private void UpdateStatus()
    {
        if (statusLabel == null) return;

        statusTimer += Time.deltaTime;
        if (statusTimer < 0.5f) return;
        statusTimer = 0f;

        if (socket == null)
            statusLabel.text = "Servidor de video detenido";
        else if (clientEndPoint == null)
            statusLabel.text = "Servidor activo - sin cliente";
        else if (streaming)
            statusLabel.text = "Transmitiendo";
        else
            statusLabel.text = "Cliente conectado - transmision detenida";
    }

    private void SendCurrentFrame()
    {
        webcam.GetPixels32(pixelBuffer);
        frameTexture.SetPixels32(pixelBuffer);
        frameTexture.Apply(false);

        byte[] jpg = frameTexture.EncodeToJPG(jpegQuality);

        int chunkCount = Mathf.CeilToInt(jpg.Length / (float)MaxPayload);
        frameId++;

        for (int i = 0; i < chunkCount; i++)
        {
            int offset = i * MaxPayload;
            int size = Mathf.Min(MaxPayload, jpg.Length - offset);

            byte[] packet = new byte[HeaderSize + size];
            BitConverter.GetBytes(frameId).CopyTo(packet, 0);
            BitConverter.GetBytes((ushort)i).CopyTo(packet, 4);
            BitConverter.GetBytes((ushort)chunkCount).CopyTo(packet, 6);
            Buffer.BlockCopy(jpg, offset, packet, HeaderSize, size);

            try { socket.Send(packet, packet.Length, clientEndPoint); }
            catch (SocketException) {  }
        }
    }

    void OnDestroy()
    {
        streaming = false;
        if (webcam != null) webcam.Stop();
        socket?.Close();
    }
}
