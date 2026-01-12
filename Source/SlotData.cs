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
    public readonly bool DeathLink;
    public readonly Rank RequiredRank;
    public readonly int RequiredLevelCompletions;
    public readonly int Required16RpmCompletions;
    public readonly int Required45RpmCompletions;
    public readonly int Required78RpmCompletions;
    
    public SlotData(Dictionary<string, object> slotDict)
    {
        foreach (var x in slotDict) APConsole.Instance.DebugLog($"{x.Key} {x.Value}");
        RequiredRank = (Rank)(long)slotDict["Required Rank"];
        RequiredLevelCompletions = (int)(long)slotDict["Required Level Completions"];
        Required16RpmCompletions = (int)(long)slotDict["Required 16RPM Completions"];
        Required45RpmCompletions = (int)(long)slotDict["Required 45RPM Completions"];
        Required78RpmCompletions = (int)(long)slotDict["Required 78RPM Completions"];
        DeathLink = (long)slotDict["DeathLink"] == 1;
        if (DeathLink)
            PluginMain.ArchipelagoHandler.UpdateTags(["DeathLink"]);
    }
}