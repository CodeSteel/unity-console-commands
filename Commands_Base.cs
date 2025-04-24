using System.Linq;
using UnityEngine;

namespace Steelbox.Console.Commands
{
    public static class Commands_Base
    {
        [ConsoleCommand("help", "List all available commands")]
        public static string Help(string[] args)
        {
            var lines = ConsoleManager.Instance.CommandMetadata
                .Select(kvp => $"- {kvp.Key}: {kvp.Value.Description}")
                .OrderBy(x => x)
                .ToArray();

            return string.Join("\n", lines);
        }
        
        [ConsoleCommand("time-scale", "Set the game time scale")]
        public static string TimeScale(string[] args)
        {
            if (args.Length is > 1 or 0) return "tooManyArguments";
            if (float.TryParse(args[0], out float timeValue))
            {
                Time.timeScale = timeValue;
                return "successful";
            }
            return string.Empty;
        }
    }
}