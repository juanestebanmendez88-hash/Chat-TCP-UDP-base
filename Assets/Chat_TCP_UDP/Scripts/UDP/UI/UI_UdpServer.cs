using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UdpServerUI : MonoBehaviour
{
    public int serverPort = 5555;
    [SerializeField] private UDPServer serverReference;
    [SerializeField] private TMP_InputField messageInput;

    private IServer _server;
    void Awake()
    {
        _server = serverReference;
    }
    void Start()
    {
        _server.OnMessageReceived += HandleMessageReceived;
        _server.OnConnected += HandleConnection;
        _server.OnDisconnected += HandleDisconnection;
    }
    public void StartServer()
    {
        _server.StartServer(serverPort);
    }
    public void SendServerMessage()
    {
        if(!_server.isServerRunning){
            Debug.Log("The server is not running");
            return;
        }

        if(messageInput.text == ""){
            Debug.Log("The chat entry is empty");
            return;
        }

        string message = messageInput.text;
        _server.SendMessageAsync(message);
    }

    void HandleMessageReceived(string text)
    {
        Debug.Log("[UI-Server] Message received from client: " + text);
    }

    void HandleConnection()
    {
        Debug.Log("[UI-Server] Client Connected to Server");
    }
    void HandleDisconnection()
    {
        Debug.Log("[UI-Server] Client Disconnect from Server");
    }
}
