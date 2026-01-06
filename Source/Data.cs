using System.Collections.Generic;

namespace BitsAndBops_AP_Client;

public class Data
{
    public static Dictionary<Stage, int> StageToId = new()
    {
        {Stage.FlipperSnapper, 0},
        {Stage.SweetTooth, 1},
        {Stage.RockPaperShowdown, 2},
        {Stage.PantryParade, 3},
        {Stage.Mixtape1, 4},
        {Stage.BBot, 5},
        {Stage.FlowWorms, 6},
        {Stage.MeetAndTweet, 7},
        {Stage.SteadyBears, 8},
        {Stage.Mixtape2, 9},
        {Stage.PopUpKitchen, 10},
        {Stage.FireworkFestival, 11},
        {Stage.HammerTime, 12},
        {Stage.Molecano, 13},
        {Stage.Mixtape3, 14},
        {Stage.PresidentBird, 15},
        {Stage.Snakedown, 16},
        {Stage.Octeaparty, 17},
        {Stage.GlobeTrotters, 18},
        {Stage.Mixtape4, 19},
    };

    public static Dictionary<float, int> SpeedToId = new()
    {
        { RecordPlayerScript.speed33, 0 },
        { RecordPlayerScript.speed16, 1 },
        { RecordPlayerScript.speed45, 2 },
        { RecordPlayerScript.speed78, 3 }
    };
}