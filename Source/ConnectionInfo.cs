using System.IO;
using Newtonsoft.Json;

namespace BitsAndBops_AP_Client;

public class ConnectionInfo(string server, string slot, string password)
{
    public string Server { get; set; } = server;
    public string Slot { get; set; } = slot;
    public string Password { get; set; } = password;
}

public static class ConnectionInfoHandler
{
    private const string Path = "./ArchipelagoSaves/" + "connection_info.json";

    public static void Save(string server, string slot, string password)
    {
        var connectionInfo = new ConnectionInfo(server, slot, password);
        var text = JsonConvert.SerializeObject(connectionInfo);
        File.WriteAllText(Path, text);
    }

    public static bool Load(ref string server, ref string slotName, ref string password)
    {
        if (!File.Exists(Path))
            Save(server, slotName, password);
        var json = File.ReadAllText(Path);
        var connectionInfo = JsonConvert.DeserializeObject<ConnectionInfo>(json);
        if (connectionInfo == null) 
            return false;
        server = connectionInfo.Server;
        slotName = connectionInfo.Slot;
        password = connectionInfo.Password;
        return true;
    }
}