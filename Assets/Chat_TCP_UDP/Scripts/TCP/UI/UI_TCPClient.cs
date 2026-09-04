using System;
using TMPro;
using UnityEngine;

public class UI_TCPClient: MonoBehaviour
{
    public int serverPort = 5555;
    public string serverAddress = "127.0.0.1";
    [SerializeField] private TCPClient clientReference;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private ChatLogUI chat;

    private IClient _client;
    void Awake()
    {
        _client = clientReference;
    }
    void Start()
    {
        _client.OnMessageReceived += HandleMessageReceived;
        _client.OnConnected += HandleConnection;
        _client.OnDisconnected += HandleDisconnection;
    }

    public void ConnectClient()
    {
        _client.ConnectToServer(serverAddress, serverPort);
    }

    public void SendClientMessage()
    {
        if(!_client.isConnected)
        {
            Debug.Log("The client is not connected");
            return;
        }

        if(messageInput.text == ""){
            Debug.Log("The chat entry is empty");
            return;
        }

        string message = messageInput.text;
        _client.SendMessageAsync(message);

        chat.AddMessage(message, true);
        messageInput.text = "";
    }

    void HandleMessageReceived(string text)
    {
        Debug.Log("[UI-Client] Message received from server: " + text);
        chat.AddMessage(text, false);
    }

    void HandleConnection()
    {
        Debug.Log("[UI-Client] Client Connected to Server");
    }
    void HandleDisconnection()
    {
        Debug.Log("[UI-Client] Client Disconnect from Server");
    }
}
