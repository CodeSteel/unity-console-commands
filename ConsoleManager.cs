using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Steelbox.Console
{
    public class ConsoleCommandMeta
    {
        public string Description;
        public Func<string[], string> Handler;
    }

    public enum ConsoleLogType
    {
        User,
        System,
        Warning,
        Error
    }
    
    public class ConsoleManager : MonoBehaviour
    {
        public static ConsoleManager Instance; 
        public static Action<string, ConsoleLogType> OnLogToConsole;
        public static Action OnClear;
        
        public bool LogUnityDebugs { get; set; }
        public IEnumerable<string> CommandKeys => _commandMap.Keys;
        public IReadOnlyDictionary<string, ConsoleCommandMeta> CommandMetadata => _commandMap;
        
        private Dictionary<string, ConsoleCommandMeta> _commandMap;
        
        private void Awake()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(this);
            Instance = this;

            Application.logMessageReceived += OnUnityLog;
            
            GetCommands();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLog;
        }

        public void ExecuteCommand(string commandName, string[] args)
        {
            string loweredCommandName = commandName.ToLower();
            if (loweredCommandName == "clear")
            {
                OnClear?.Invoke();
                return;
            }

            LogToConsole(commandName + " " + string.Join(" ", args), ConsoleLogType.User);
            
            if (_commandMap.TryGetValue(loweredCommandName, out var meta))
            {
                string result = meta.Handler.Invoke(args);
                if (!string.IsNullOrEmpty(result))
                    LogToConsole(result);
            }
            else
            {
                LogToConsole($"Command '{loweredCommandName}' does not exist.");
            }
        }

        public void Log(string log, ConsoleLogType logType = ConsoleLogType.System) => LogToConsole(log, logType);
        
        private void LogToConsole(string log, ConsoleLogType logType = ConsoleLogType.System)
        {
            OnLogToConsole?.Invoke(log, logType);
        }
        
        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!LogUnityDebugs) return;
            
            ConsoleLogType logType = type switch
            {
                LogType.Log => ConsoleLogType.System,
                LogType.Warning => ConsoleLogType.Warning,
                LogType.Error => ConsoleLogType.Error,
                LogType.Exception => ConsoleLogType.Error,
                LogType.Assert => ConsoleLogType.Warning,
                _ => ConsoleLogType.System
            };
            
            string formatted = condition;

            if (type is LogType.Exception or LogType.Error)
            {
                formatted += $"\n{stackTrace}";
            }

            LogToConsole(formatted, logType);
        }
        
        private void GetCommands()
        {
            _commandMap = new Dictionary<string, ConsoleCommandMeta>();

            var methodInfos = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass)
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(method =>
                    method.GetCustomAttributes<ConsoleCommand>().Any() &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(string[]));

            foreach (var method in methodInfos)
            {
                var commandAttrs = method.GetCustomAttributes<ConsoleCommand>();
                foreach (var attr in commandAttrs)
                {
                    string key = attr.Command.ToLower();

                    if (_commandMap.ContainsKey(key))
                    {
                        Debug.LogWarning($"[Console] Duplicate command '{key}' found in {method.DeclaringType?.FullName}.{method.Name}");
                        continue;
                    }

                    Delegate del;
                    if (method.ReturnType == typeof(string))
                    {
                        del = Delegate.CreateDelegate(typeof(Func<string[], string>), method);
                        _commandMap[key] = new ConsoleCommandMeta
                        {
                            Description = attr.Description,
                            Handler = (Func<string[], string>)del
                        };
                    }
                    else if (method.ReturnType == typeof(void))
                    {
                        del = Delegate.CreateDelegate(typeof(Action<string[]>), method);
                        _commandMap[key] = new ConsoleCommandMeta
                        {
                            Description = attr.Description,
                            Handler = args =>
                            {
                                ((Action<string[]>)del)(args);
                                return string.Empty;
                            }
                        };
                    }

                    Debug.Log($"[Console] Registered command: '{key}' → {method.DeclaringType?.Name}.{method.Name}");
                }
            }
        }
    }
}