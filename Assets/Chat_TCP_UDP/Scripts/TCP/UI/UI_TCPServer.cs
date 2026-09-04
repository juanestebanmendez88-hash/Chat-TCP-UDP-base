using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TCPServerUI : MonoBehaviour
{
    public int serverPort = 5555;
    [SerializeField] private TCPServer serverReference;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private ChatLogUI chat;

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

        chat.AddMessage(message, true);
        messageInput.text = "";
    }

    void HandleMessageReceived(string text)
    {
        Debug.Log("[UI-Server] Message received from client: " + text);
        chat.AddMessage(text, false);
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
