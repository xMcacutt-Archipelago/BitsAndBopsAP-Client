using System.Collections.Generic;

namespace BitsAndBops_AP_Client;

public enum Rank
{
    Cool,
    Amazing,
    Perfect
}

public class SlotData
{
    public bool DeathLink = false;
    public Rank RequiredRank = Rank.Cool;
    public int RequiredLevelCompletions = 0;
    public int Required16RPMCompletions = 0;
    public int Required45RPMCompletions = 0;
    public int Required78RPMCompletions = 0;
    
    public SlotData(Dictionary<string, object> slotDict)
    {
        foreach (var x in slotDict) APConsole.Instance.DebugLog($"{x.Key} {x.Value}");
        RequiredRank = (Rank)(long)slotDict["Required Rank"];
        RequiredLevelCompletions = (int)(long)slotDict["Required Level Completions"];
        Required16RPMCompletions = (int)(long)slotDict["Required 16RPM Completions"];
        Required45RPMCompletions = (int)(long)slotDict["Required 45RPM Completions"];
        Required78RPMCompletions = (int)(long)slotDict["Required 78RPM Completions"];
        DeathLink = (long)slotDict["DeathLink"] == 1;
        if (DeathLink)
            PluginMain.ArchipelagoHandler.UpdateTags(["DeathLink"]);
    }
}