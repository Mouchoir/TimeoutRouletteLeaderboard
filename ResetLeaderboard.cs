using System;
using System.Collections.Generic;
using System.Reflection;

public class CPHInline
{
    static string LogPrefix = "TSO::Roulette::";
    static RouletteConfig config = new RouletteConfig();

    public bool Execute()
    {
        // Load the configuration
        SetConfig();

        // Check if a username was passed as an argument
        string targetUser;
        if (CPH.TryGetArg("input0", out targetUser) && !string.IsNullOrWhiteSpace(targetUser))
        {
            // Reset data for a specific user
            ResetUserLeaderboard(targetUser);
        }
        else
        {
            // Reset the leaderboard globally
            ResetGlobalLeaderboard();
        }

        return true;
    }

    public bool SetConfig()
    {
        bool noErrors = true;
        Type t = config.GetType();
        PropertyInfo[] props = t.GetProperties();
        foreach (var prop in props)
        {
            Type propType = prop.PropertyType;
            object propValue;
            if (!CPH.TryGetArg(prop.Name, out propValue))
            {
                noErrors = false;
                CPH.LogWarn(String.Format("{0}Config argument {1} is not set, check roulette_Config actions", LogPrefix, prop.Name));
                continue;
            }

            prop.SetValue(config, Convert.ChangeType(propValue, propType));
            CPH.LogVerbose(String.Format("{0}Config argument {1} is set to value {2} of type {3}", LogPrefix, prop.Name, propValue, propType));
        }

        return noErrors;
    }

    public void ResetGlobalLeaderboard()
    {
        // Log for debugging
        CPH.LogDebug($"{LogPrefix}ResetGlobalLeaderboard - Resetting the entire leaderboard.");

        // Clear the global leaderboard by removing the global variable
        CPH.SetGlobalVar("rouletteLeaderboard", new Dictionary<string, (int losses, int timeoutMinutes)>(), true);

        // Send the message for global reset
        CPH.SendMessage(config.resetLeaderboardMessage);
    }

    public void ResetUserLeaderboard(string targetUser)
    {
        // Log for debugging
        CPH.LogDebug($"{LogPrefix}ResetUserLeaderboard - Resetting leaderboard for user {targetUser}.");

        // Load the existing leaderboard
        var leaderboard = CPH.GetGlobalVar<Dictionary<string, (int losses, int timeoutMinutes)>>("rouletteLeaderboard", true);

        if (leaderboard != null && leaderboard.ContainsKey(targetUser))
        {
            // Remove the specified user's data
            leaderboard.Remove(targetUser);
            CPH.SetGlobalVar("rouletteLeaderboard", leaderboard, true);

            // Send the reset message for a specific user
            CPH.SendMessage(string.Format(config.resetUserMessage, targetUser));
        }
        else
        {
            // Send a message indicating that the user has never participated
            CPH.SendMessage(string.Format(config.userNotParticipatedMessage, targetUser));
        }
    }
}

class RouletteConfig
{
    public string resetLeaderboardMessage { get; set; }
    public string resetUserMessage { get; set; }
    public string userNotParticipatedMessage { get; set; }
}
