using System;

namespace Steelbox.Console
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommand : Attribute
    {
        public string Command { get; }
        
        public string Description { get; }

        public ConsoleCommand(string command, string description = "")
        {
            Command = command;
            Description = description;
        }
    }
}