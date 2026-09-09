using System.Threading.Tasks;

public interface IClient : IChatConnection
{
    public bool isConnected { get; }
    public Task ConnectToServer(string ip, int port);
}
