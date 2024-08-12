using System;
using System.Collections.Generic;
using System.Reflection;
public class CPHInline
{
    // These actions are meant to work with TheShiningOne's timout Roulette. Visit https://github.com/TheShining1/TimeoutRoulette
    // Adding TheShiningOne's prefix to match his project.
    static string LogPrefix = "TSO::Roulette::";
    static RouletteConfig config = new RouletteConfig();

    public bool Execute()
    {
        SetConfig();
        return RouletteLeaderboard();
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
                continue;
            }

            prop.SetValue(config, Convert.ChangeType(propValue, propType));
        }

        return noErrors;
    }

    public bool RouletteLeaderboard()
    {
        CPH.LogDebug($"{LogPrefix}RouletteLeaderboard - Method Start");

        try
        {
            // Load the leaderboard from the persisted global variable
            var leaderboard = CPH.GetGlobalVar<Dictionary<string, (int losses, int timeoutMinutes)>>("rouletteLeaderboard", true);

            if (leaderboard == null || leaderboard.Count == 0)
            {
                CPH.LogDebug("No players have participated in the roulette yet.");
                CPH.SendMessage(config.noPlayersMessage); // Use text retrieved from Config_Text
                return true;
            }

            // Convert the dictionary to a list for sorting
            var leaderboardList = new List<(string user, int losses, int timeoutMinutes)>();

            foreach (var entry in leaderboard)
            {
                leaderboardList.Add((entry.Key, entry.Value.losses, entry.Value.timeoutMinutes));
            }

            // Sort the leaderboard by losses, then by timeout minutes
            leaderboardList.Sort((a, b) => b.losses != a.losses ? b.losses.CompareTo(a.losses) : b.timeoutMinutes.CompareTo(a.timeoutMinutes));

            // Display the top 3 players with a 50ms delay between messages
            for (int i = 0; i < Math.Min(3, leaderboardList.Count); i++)
            {
                var entry = leaderboardList[i];

                // Format the message using the template from Config_Text
                var leaderboardMessage = String.Format(config.leaderboardMessageTemplate, i + 1, entry.user, entry.losses, entry.timeoutMinutes);
                CPH.SendMessage(leaderboardMessage);

                // Introduce a 50ms delay between messages
                CPH.Wait(50);
            }
        }
        catch (Exception ex)
        {
            CPH.LogDebug($"An error occurred: {ex.Message}");
        }

        CPH.LogDebug($"{LogPrefix}RouletteLeaderboard - Method End");
        return true;
    }
}

class RouletteConfig
{
    public string noPlayersMessage { get; set; }
    public string leaderboardMessageTemplate { get; set; }
}
