using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public static class Terminal
    {
        public class UpdatableLog: IDisposable
        {
            private Observable<UITerminal.TextRow> m_TextRow = new ();
            private bool m_PersistOnDispose;
            private readonly string m_Pattern;
            private object[] m_Args;
            private CancellationSignalSource m_LifecycleTokenSource = CancellationSignalSource.Create();
            private Observable<bool> m_UpdateInProgress = new ();
            private bool m_IsDisposed;
            private bool m_Peristed;
            
            public UpdatableLog(string pattern = "{0}", bool persistOnDispose = false)
            {
                m_PersistOnDispose = persistOnDispose;
                m_Pattern = pattern;
                Init();
            }
            
            ~UpdatableLog()
            {
                Dispose();
            }

            private async void Init()
            {
                try
                {
                    var ui = await WaitForUITerminal(m_LifecycleTokenSource.Signal);
                    m_TextRow.value = ui.AddTextRow("");
                }
                catch (OperationCanceledException)
                {
                    // Ingore
                }
            }
            
            private void Update(string[] values)
            {
                
                if (m_IsDisposed) return;
                m_Args = values;
                NeedsUpdate();
            }

            public void Update(string a1)
            {
                Update(new string[] {a1});
            }
            
            public void Update(string a1, string a2)
            {
                Update(new string[] {a1, a2});
            }

            private async void NeedsUpdate()
            {
                if (m_UpdateInProgress.value)
                {
                    return;
                }

                try
                {
                    m_UpdateInProgress.value = true;
                    var row = await m_TextRow.WaitUntilValueIsNotNull(m_LifecycleTokenSource.Signal);
                    Debug.Log($"Set new text: {GetFormattedText()}");
                    row.SetText(GetFormattedText());
                }
                catch (OperationCanceledException)
                {
                    // Skip
                }
                catch (Exception e)
                {
                    LogError(e.Message);
                }
                finally
                {
                    m_UpdateInProgress.value = false;
                }
            }

            private string GetFormattedText()
            {
                return string.Format(m_Pattern, m_Args);
            }

            public async void Persist()
            {
                if (m_IsDisposed || m_Peristed) return;
                m_Peristed = true;
                if (m_UpdateInProgress.value)
                {
                    await m_UpdateInProgress.WaitUntilValueIs(false);
                }
                m_TextRow.value?.Persist();
                Dispose();
            }

            public void Persist(string[] texts)
            {
                if (m_IsDisposed) return;
                m_Args = texts;
                NeedsUpdate();
                Persist();
            }

            public void Persist(string t1)
            {
                Persist(new string[] {t1});
            }

            public void Dispose()
            {
                if (m_IsDisposed) return;
                if (m_PersistOnDispose)
                {
                    Persist();
                    return;
                }
                m_IsDisposed = true;
                m_TextRow.value.Dispose();
                m_TextRow.Dispose();
                m_LifecycleTokenSource.TryCancel();
                m_LifecycleTokenSource.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public class Command: IDisposable
        {
            public readonly string name;
            public readonly string description;
            public readonly string usage;
            public readonly string[] aliases;
            public readonly Action<string[]> action;
            private readonly Action<Command> m_OnRelease;
            public event Action<string[]> m_OnPerform;
            private bool m_Disposed;
            
            public Command(string name, string description, string usage, string[] aliases, Action<Command> onRelease)
            {
                this.name = name;
                this.description = description;
                this.usage = usage;
                this.aliases = aliases;
                m_OnRelease = onRelease;
            }

            public void Perform(string[] args)
            {
                m_OnPerform?.Invoke(args);
            }

            public void Dispose()
            {
                if (m_Disposed) return;
                m_Disposed = true;
                m_OnRelease(this);
            }
            
            public DisposableAction Subscribe(Action<string[]> action)
            {
                m_OnPerform += action;
                return new DisposableAction(() => m_OnPerform -= action);
            }
            
            public UniTask<string[]> WhenPerform(CancellationToken cancellationToken)
            {
                var source = new UniTaskCompletionSource<string[]>();
                m_OnPerform += args =>
                {
                    source.TrySetResult(args);
                };
                cancellationToken.Register(() =>
                {
                    source.TrySetCanceled();
                });
                return source.Task;
            }
        }
        
        delegate void CommandHandler(CommandRequest request);

        private static Dictionary<string, Command> m_CommandListeners = new();
        private static event Action<string, string[]> OnCommand;

        public readonly struct CommandRequest
        {
            public readonly string command;
            public readonly string[] args;
        }

        private static Observable<UITerminal> m_CachedUITerminal = new ();
        public static UniTask<UITerminal> WaitForUITerminal(CancellationSignal signal)
        {
            return m_CachedUITerminal.WaitUntilValueIsNotNull(signal);
        }
        
        /* Execute method on initialization */
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            CancellationTokenSource cancellationTokenSource = null;
            Service.SubscribeService<TerminalService>(service =>
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                m_CachedUITerminal.value = service.terminal;
                service.terminal.OnCommand += HandleCommand;
                cancellationTokenSource.Token.Register(() =>
                {
                    service.terminal.OnCommand -= HandleCommand;
                });
            });
        }
        
        private static void HandleCommand(string rawCommand)
        {
            /* Split command string by space to get command and arguments */
            var parts = rawCommand.Split(' ');
            var command = parts[0];
            var args = parts.Length > 1 ? parts[1..] : new string[0];
            Log(rawCommand);
            OnCommand?.Invoke(command, args);
            if (m_CommandListeners.TryGetValue(command, out var commandInstance))
            {
                commandInstance.Perform(args);
            }
        }

        public static UpdatableLog CreateUpdatableLog(string pattern = "{0}", bool persistOnDispose = false)
        {
            return new UpdatableLog(pattern, persistOnDispose);
        }
        
        public static async void Log(string text)
        {
            var terminalService = await Service.Resolve<TerminalService>();
            terminalService.terminal.AddTextRow(text).Persist();
        }
        
        public static void LogError(string text)
        {
            Log($"<color=red>{text}</color>");
        }
        
        public static Command RegisterCommand(string name, string description = "", string usage = "", string[] aliases = null)
        {
            var command = new Command(name, description, usage, aliases, InternalReleaseCommand);
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    m_CommandListeners.TryAdd(alias, command);
                }
            }
            m_CommandListeners.TryAdd(name, command);
            return command;
        }
        
        public static UniTask PressEnterToContinue(string message, CancellationToken cancellationToken)
        {
            var source = new UniTaskCompletionSource();
            Action<string, string[]> commandHandler = null;
            commandHandler = (command, args) =>
            {
                Debug.Log($"Entered command: {command}");
                OnCommand -= commandHandler;
                source.TrySetResult();
            };
            OnCommand += commandHandler;
            cancellationToken.Register(() =>
            {
                OnCommand -= commandHandler;
                source.TrySetCanceled();
            });
            Log(message);
            return source.Task;
        }

        private static void InternalReleaseCommand(Command command)
        {
            foreach (var alias in command.aliases)
            {
                m_CommandListeners.Remove(alias);
            }
            m_CommandListeners.Remove(command.name);
        }
    }
}