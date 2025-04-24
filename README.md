# unity-console-commands
An in-game console &amp; attribute-based command system.

## Supports
✅ Command history

✅ Command suggestions

✅ Arrow key navigation

✅ Multiple arguments

✅ C# attributes


## Easy to use!
```cs
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
```

## Showcase
https://github.com/user-attachments/assets/16ccd5b2-332a-4480-806f-563e087558fd
