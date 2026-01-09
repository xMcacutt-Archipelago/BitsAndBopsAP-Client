using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace BitsAndBops_AP_Client 
{
    public class ArchipelagoHandler : MonoBehaviour
    {
        private ArchipelagoSession Session { get; set; }
        private string Server { get; set; }
        private int Port { get; set; }
        private string Slot { get; set; }
        private string Password { get; set; }
        private string seed;
        public bool IsConnected => Session?.Socket.Connected ?? false;
        public event System.Action OnConnected;
        public event System.Action<string> OnConnectionFailed;
        public event System.Action OnDisconnected;
        private ConcurrentQueue<long> locationsToCheck = new();
        private string lastDeath;
        private DateTime lastDeathLinkTime = DateTime.Now;
        private readonly Random random = new();

        public volatile bool ConnectionFinished;
        public volatile bool ConnectionSucceeded;
        
        private readonly string[] deathMessages =
        [
            "had a skill issue (died)",
            "scratched their favorite vinyl",
            "dropped the ball",
            "parroted the wrong words",
            "threw rock instead of scissors",
            "dropped their streak",
            "dropped a hammer on their foot",
            "tripped and fell",
            "hit their thumb with a hammer",
            "bit their tongue instead of the cupcake",
            "mixed up their tapes",
            "pricked themself with a pin",
            "forgot to skewer the shrimp",
            "got punched by one of the Fly Girls",
            "held the camera the wrong way",
            "dove too deep",
            "forgot their parachute",
            "brushed up against poison ivy"
        ];
        
        private static string GetColorHex(PaletteColor? color)
        {
            return color switch
            {
                PaletteColor.Red => "#EE0000",
                PaletteColor.Green => "#00FF7F",
                PaletteColor.Yellow => "#FAFAD2",
                PaletteColor.Blue => "#6495ED",
                PaletteColor.Magenta => "#EE00EE",
                PaletteColor.Cyan => "#00EEEE",
                PaletteColor.Black => "#000000",
                PaletteColor.White => "#FFFFFF",
                PaletteColor.SlateBlue => "#6D8BE8",
                PaletteColor.Salmon => "#FA8072",
                PaletteColor.Plum => "#AF99EF",
                _ => "#FFFFFF" // Default to white
            };
        }
        
        public void CreateSession(string server, int port, string slot, string password)
        {
            Server = server;
            Port = port;
            Slot = slot;
            Password = password;
            locationsToCheck = new ConcurrentQueue<long>();
            Session = ArchipelagoSessionFactory.CreateSession(Server, Port);
            Session.MessageLog.OnMessageReceived += OnMessageReceived;
            Session.Socket.ErrorReceived += OnError;
            Session.Socket.SocketClosed += OnSocketClosed;
            Session.Socket.PacketReceived += PacketReceived;
            Session.Items.ItemReceived += ItemReceived;
        }
        
        public void Connect()
        {
            APConsole.Instance.Log($"Logging in to {Server}:{Port} as {Slot}...");
            seed = Session.ConnectAsync()?.Result?.SeedName;
            
            var result = Session.LoginAsync(
                PluginMain.GameName,
                Slot,
                ItemsHandlingFlags.AllItems,
                new System.Version(0, 6, 5),
                [],
                password: Password
            ).Result;

            if (result.Successful)
            {
                APConsole.Instance.Log($"Success! Connected to {Server}:{Port}");
                var successResult = (LoginSuccessful)result;
                PluginMain.SlotData = new SlotData(successResult.SlotData);
                
                if (seed != null)
                    PluginMain.SaveDataHandler!.GetSave(seed, Slot);
            
                PluginMain.ArchipelagoHandler.StartCoroutine(RunCheckQueue());
                ConnectionSucceeded = true;
                ConnectionFinished = true;
                OnConnected?.Invoke();
                return;
            }

            ConnectionSucceeded = false;
            ConnectionFinished = true;
            var failure = (LoginFailure)result;
            var errorMessage = $"Failed to Connect to {Server}:{Port} as {Slot}:";
            errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
            errorMessage = failure.ErrorCodes.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
            OnConnectionFailed?.Invoke(errorMessage);
            APConsole.Instance.Log(errorMessage);
            APConsole.Instance.Log("Attempting reconnect...");
        }

        public void Disconnect()
        {
            if (Session == null)
                return;
            StopAllCoroutines();
            Session.Socket.DisconnectAsync();
            Session = null;
            APConsole.Instance.Log("Disconnected from Archipelago");
        }
        
        private void OnError(Exception ex, string message)
        {
            APConsole.Instance.Log($"Socket error: {message} - {ex.Message}");
        }

        private void OnSocketClosed(string reason)
        {
            StopAllCoroutines();
            APConsole.Instance.Log($"Socket closed: {reason}");
            OnDisconnected?.Invoke();
        }

        private void ItemReceived(ReceivedItemsHelper helper)
        {
            try
            {
                while (helper.Any())
                {
                    var itemIndex = helper.Index;
                    var item = helper.DequeueItem();
                    PluginMain.ItemHandler.HandleItem(itemIndex, item);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ItemReceived Error: {ex}");
                throw;
            }
        }

        public void ResyncItems()
        {
            if (!IsConnected)
            {
                APConsole.Instance.DebugLog("Cannot resync items: Not connected to Archipelago");
                return;
            }

            APConsole.Instance.DebugLog("Resyncing items from server...");
            var items = Session.Items.AllItemsReceived;
            for (int i = 0; i < items.Count; i++)
            {
                PluginMain.ItemHandler.HandleItem(i, items[i], false);
            }
            PluginMain.SaveDataHandler.SaveGame();
            APConsole.Instance.DebugLog($"Resync complete. Processed up to item {items.Count}");
        }

        public void Release()
        {
            Session.SetGoalAchieved();
            Session.SetClientState(ArchipelagoClientState.ClientGoal);
        }
        
        public void CheckLocations(long[] ids)
        {
            ids.ToList().ForEach(id => locationsToCheck.Enqueue(id));
        }
        
        public void CheckLocation(long id)
        {
            locationsToCheck.Enqueue(id);
        }
        
        private IEnumerator RunCheckQueue()
        {
            while (true)
            {
                if (locationsToCheck.TryDequeue(out var locationId))
                {
                    Session.Locations.CompleteLocationChecks(locationId);
                    APConsole.Instance.DebugLog($"Sent location check: {locationId}");
                } 
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        public bool IsLocationChecked(long id)
        {
            return Session.Locations.AllLocationsChecked.Contains(id);
        }

        public int CountLocationsCheckedInRange(long start, long end)
        {
            return Session.Locations.AllLocationsChecked.Count(loc => loc >= start && loc < end);
        }

        public int CountLocationsCheckedInRange(long start, long end, long delta)
        {
            return Session.Locations.AllLocationsChecked.Count(loc => loc >= start && loc < end && loc % delta == start % delta);
        }
        
        public void UpdateTags(List<string> tags)
        {
            var packet = new ConnectUpdatePacket
            {
                Tags = tags.ToArray(),
                ItemsHandling = ItemsHandlingFlags.AllItems
            };
            Session.Socket.SendPacket(packet);
        }
        
        private void OnMessageReceived(LogMessage message)
        {
            string messageStr;
            if (message.Parts.Length == 1)
            {
                messageStr = message.Parts[0].Text;
            }
            else
            {
                var builder = new StringBuilder();
                foreach (var part in message.Parts)
                {
                    string hexColor = GetColorHex(part.PaletteColor);
                    builder.Append($"<color={hexColor}>{part.Text}</color>");
                }
                messageStr = builder.ToString();
            }
            APConsole.Instance.Log(messageStr);
        }
        
        private void PacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet)
            {
                case BouncePacket bouncePacket:
                    BouncePacketReceived(bouncePacket);
                    break;
            }
        }
        
        public void SendDeath()
        {
            APConsole.Instance.DebugLog("SendDeath called");
            if (!PluginMain.SlotData.DeathLink)
                return;
            
            var packet = new BouncePacket();
            var now = DateTime.Now;

            if (now - lastDeathLinkTime < TimeSpan.FromSeconds(2))
                return;
            
            packet.Tags = ["DeathLink"];
            packet.Data = new Dictionary<string, JToken>
            {
                { "time", now.ToUnixTimeStamp() },
                { "source", Slot },
                { "cause", $"{Slot} {deathMessages[random.Next(deathMessages.Length)]}" }
            };

            if (packet.Data.TryGetValue("source", out var sourceObj))
            {
                var source = sourceObj?.ToString() ?? "Unknown";
                if (packet.Data.TryGetValue("cause", out var causeObj))
                {
                    var cause = causeObj?.ToString() ?? "Unknown";
                    if (packet.Data.TryGetValue("time", out var timeObj))
                    {
                        var time = timeObj?.ToString() ?? "Unknown";
                    }
                }
            }
            
            lastDeathLinkTime = now;
            Session.Socket.SendPacket(packet);
        }

        private void BouncePacketReceived(BouncePacket packet)
        {
            if (PluginMain.SlotData.DeathLink)
                ProcessBouncePacket(packet, "DeathLink", ref lastDeath, (source, data) =>
                    HandleDeathLink(source, data["cause"]?.ToString() ?? "Unknown"));
        }

        private static void ProcessBouncePacket(BouncePacket packet, string tag, ref string lastTime,
            Action<string, Dictionary<string, JToken>> handler)
        {
            if (!packet.Tags.Contains(tag)) return;
            if (!packet.Data.TryGetValue("time", out var timeObj))
                return;
            if (lastTime == timeObj.ToString())
                return;
            lastTime = timeObj.ToString();
            if (!packet.Data.TryGetValue("source", out var sourceObj))
                return;
            var source = sourceObj?.ToString() ?? "Unknown";
            if (packet.Data.TryGetValue("cause", out var causeObj))
            {
                var cause = causeObj?.ToString() ?? "Unknown";
                APConsole.Instance.DebugLog($"Received Bounce Packet with Tag: {tag} :: {cause}");
            }

            handler(source, packet.Data);
        }
        
        private void HandleDeathLink(string source, string cause)
        {
            if (!PluginMain.SlotData.DeathLink)
                return;
            APConsole.Instance.Log(cause);
            if (source == Slot)
                return;
            PluginMain.GameHandler.Kill();
        }

        public ScoutedItemInfo TryScoutLocation(long locationId)
        {
            return Session.Locations.ScoutLocationsAsync(locationId)?.Result?.Values.First();
        }
    }
}