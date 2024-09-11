using System;
using System.Collections.Generic;

namespace Metamatic
{
    public class Command
    {
        public readonly string Name;
        public readonly Action<string> Action;
        public Command(string name, Action<string> action)
        {
            Name = name;
            Action = action;
        }
        public void Execute(string args)
        {
            Action(args);
        }
    }
    public class CommandCommander : Command
    {
        public readonly Commander Commander;
        public CommandCommander(string name, Commander commander)
            : base(name, args =>
            {
                var parts = args.Split(' ', 2);
                commander.ExecuteCommand(parts[0], parts.Length > 1 ? parts[1] : "");
            })
        {
            Commander = commander;
        }
    }
    public class Commander: IDisposable
    {
        private readonly Dictionary<string, Command> m_Commands = new Dictionary<string, Command>();
        public DisposableAction RegisterCommand(string name, Action<string> action)
        {
            if (m_Commands.ContainsKey(name))
            {
                Console.Error("CommandAlreadyRegistered", $"Command '{name}' is already registered.");
            }
            m_Commands[name] = new Command(name, action);
            return new DisposableAction(() => m_Commands.Remove(name));
        }

        public DisposableAction RegisterCommand(string name, Commander commander)
        {
            if (m_Commands.ContainsKey(name))
            {
                Console.Error("CommandAlreadyRegistered", $"Command '{name}' is already registered.");
            }
            m_Commands[name] = new CommandCommander(name, commander);
            return new DisposableAction(() => m_Commands.Remove(name));
        }
        
        public void ExecuteCommand(string command, string args)
        {
            if (m_Commands.TryGetValue(command, out var cmd))
            {
                cmd.Execute(args);
            }
            else
            {
                Console.Exception("CommandNotFound", $"Command '{command}' not found.");
            }
        }

        public void Dispose()
        {
            m_Commands.Clear();
        }
    }
}