using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class VideoSender : MonoBehaviour
{
    public UdpVideoServer udpServer;

    public int captureWidth = 480;
    public int captureHeight = 270;
    [Range(10, 90)] public int jpegQuality = 30;

    private WebCamTexture webcamTexture;
    public Texture2D captureTexture;
    private Color32[] capturePixels;
    private bool sending = false;

    public RawImage videoDisplay;

    private void Start()
    {
        udpServer.StartUDPServer(5000);
        StartCoroutine(CaptureLoop());
    }

    public void StartSending()
    {
        if (sending) return;
        sending = true;
        StartCoroutine(CaptureLoop());
    }

    public void StopSending()
    {
        sending = false;
        if (webcamTexture != null && webcamTexture.isPlaying) webcamTexture.Stop();
        webcamTexture = null;
    }

    private IEnumerator CaptureLoop()
    {
        if (webcamTexture == null)
        {
            webcamTexture = new WebCamTexture(captureWidth, captureHeight);
            webcamTexture.Play();
            yield return new WaitUntil(() => webcamTexture.width > 16);
            videoDisplay.texture = webcamTexture;
            captureTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
            capturePixels = new Color32[webcamTexture.width * webcamTexture.height];
        }

        while (sending)
        {
            webcamTexture.GetPixels32(capturePixels);
            captureTexture.SetPixels32(capturePixels);
            captureTexture.Apply(false);

            udpServer.SendImage(captureTexture, jpegQuality);
            yield return new WaitForSeconds(.1f);
        }
    }

    private void OnDisable() => StopSending();
}
