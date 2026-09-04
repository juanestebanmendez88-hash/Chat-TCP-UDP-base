using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Captura la webcam, la comprime a JPG y la envia por UDP partida en trozos.
// El chat va aparte por TCP: esto usa su propio socket y su propio puerto.
public class WebcamUdpSender : MonoBehaviour
{
    [Header("Red")]
    public int port = 5556;                 // puerto UDP solo para el video (el chat usa TCP en 5555)

    [Header("Captura")]
    public int requestedWidth = 640;
    public int requestedHeight = 480;
    public int fps = 20;                    // frames por segundo que se envian
    [Range(5, 90)] public int jpegQuality = 40;

    [Header("UI (opcional)")]
    public RawImage localPreview;           // para verte a ti mismo en el panel del servidor
    public TMP_Text streamButtonLabel;      // texto del boton Iniciar / Detener
    public TMP_Text statusLabel;            // estado: sin cliente / cliente conectado / transmitiendo

    [Header("Debug")]
    [SerializeField] private bool streaming; // ponlo en true a mano para probar antes de tener boton

    private UdpClient socket;
    private volatile IPEndPoint clientEndPoint;  // se llena cuando el cliente manda su "HELLO"
    private WebCamTexture webcam;
    private Texture2D frameTexture;              // textura temporal para poder hacer EncodeToJPG
    private Color32[] pixelBuffer;

    private int frameId;
    private float sendTimer;
    private float statusTimer;

    private const int HeaderSize = 8;    // 4 bytes frameId + 2 chunkIndex + 2 chunkCount
    private const int MaxPayload = 1200; // bytes de imagen por datagrama

    // Se dispara desde el mismo boton "Start Server" del chat (segunda entrada en On Click).
    public void StartServer()
    {
        if (socket != null) return;            // ya iniciado

        socket = new UdpClient(port);
        IgnoreUdpConnReset(socket);
        socket.BeginReceive(OnHello, null);
        StartCoroutine(SetupCamera());
        Debug.Log("[VideoTX] Servidor de video en UDP " + port);
    }

    // Windows: evita que un ICMP "port unreachable" haga que el proximo Receive lance excepcion.
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

        // La webcam reporta width = 16 hasta que el dispositivo arranca de verdad.
        while (webcam.width <= 16) yield return null;

        frameTexture = new Texture2D(webcam.width, webcam.height, TextureFormat.RGB24, false);
        pixelBuffer = new Color32[webcam.width * webcam.height];
        Debug.Log($"[VideoTX] Webcam lista {webcam.width}x{webcam.height}");
    }

    // Handshake: al primer datagrama que llega, aprendemos la direccion del cliente.
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

        try { socket.BeginReceive(OnHello, null); } // seguir escuchando por si el cliente reinicia
        catch (ObjectDisposedException) { }
    }

    // Para los botones de la UI.
    public void ToggleStreaming() { SetStreaming(!streaming); }
    public void StartStreaming()  { SetStreaming(true); }
    public void StopStreaming()   { SetStreaming(false); }

    private void SetStreaming(bool on)
    {
        streaming = on;
        if (streamButtonLabel != null)
            streamButtonLabel.text = on ? "Detener transmision" : "Iniciar transmision";
        statusTimer = 1f;   // forzar refresco del estado en el proximo Update
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
            BitConverter.GetBytes(frameId).CopyTo(packet, 0);             // 4 bytes
            BitConverter.GetBytes((ushort)i).CopyTo(packet, 4);           // 2 bytes
            BitConverter.GetBytes((ushort)chunkCount).CopyTo(packet, 6);  // 2 bytes
            Buffer.BlockCopy(jpg, offset, packet, HeaderSize, size);

            try { socket.Send(packet, packet.Length, clientEndPoint); }
            catch (SocketException) { /* cliente no disponible: se reintenta con el proximo frame */ }
        }
    }

    void OnDestroy()
    {
        streaming = false;
        if (webcam != null) webcam.Stop();
        socket?.Close();
    }
}
