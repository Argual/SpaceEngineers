using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{

    partial class Program
    {

        public class ArgCommandParser
        {
            #region Subclasses

            public class Command
            {
                private string commandName;

                /// <summary>
                /// Delegate for a command action which takes a <see cref="MyCommandLine"/> as a parameter and outputs a <see cref="CommandInvalidArgumentsException"/> if any occoured, or null, if no exception occoured.
                /// </summary>
                /// <param name="commandLine">The command line containing the arguments and switches to run the command.</param>
                /// <param name="exception">Null if no argument-related exception occoured, the exception otherwise.</param>
                public delegate void DelegateCommand(MyCommandLine commandLine, out CommandInvalidArgumentsException exception);

                /// <summary>
                /// The command to execute.
                /// </summary>
                public DelegateCommand CommandAction { get; set; }

                /// <summary>
                /// The name (identifier) of the command.
                /// </summary>
                public string CommandName
                {
                    get
                    {
                        return commandName;
                    }
                    set
                    {
                        if (string.IsNullOrWhiteSpace(value) || value.Contains(' '))
                        {
                            throw new Exception("Command name can not be null or white-space and cannot contain spaces!");
                        }
                        commandName = value;
                    }
                }

                /// <summary>
                /// List of the names of the arguments this command takes. Can be null if this command takes no arguments. Argument names must not contain spaces!
                /// </summary>
                public List<string> Arguments { get; set; }

                /// <summary>
                /// List of the switches this command can take. Can be null if this command takes no switches. Switches must not contain spaces!
                /// </summary>
                public List<string> Switches { get; set; }

                /// <summary>
                /// The amount of arguments this command operates with.
                /// </summary>
                public int ArgumentCount
                {
                    get
                    {
                        if (Arguments==null)
                        {
                            return 0;
                        }
                        return Arguments.Count;
                    }
                }

                /// <summary>
                /// The amount of switches this command can operate with.
                /// </summary>
                public int SwitchCount
                {
                    get
                    {
                        if (Switches == null)
                        {
                            return 0;
                        }
                        return Switches.Count;
                    }
                }

                /// <summary>
                /// Whether or not the command is valid.
                /// <list type=">table">
                /// <listheader>A command is valid if the following is true:</listheader>
                /// <item>The name is not null or white-space and does not contain spaces.</item>
                /// <item>The argument names are not null and do not contain spaces.</item>
                /// <item>The switch names are not null and do not contain spaces.</item>
                /// </list>
                /// </summary>
                public bool Valid
                {
                    get
                    {
                        if (string.IsNullOrWhiteSpace(CommandName) || CommandName.Contains(' '))
                        {
                            return false;
                        }
                        if (ArgumentCount>0)
                        {
                            foreach (var arg in Arguments)
                            {
                                if (string.IsNullOrWhiteSpace(arg) || arg.Contains(' '))
                                {
                                    return false;
                                }
                            }
                        }
                        if (SwitchCount>0)
                        {
                            foreach (var s in Switches)
                            {
                                if (string.IsNullOrWhiteSpace(s) || s.Contains(' '))
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }

                /// <summary>
                /// Instantiates a command.
                /// </summary>
                /// <param name="commandName">The name (identifier) of the command.</param>
                /// <param name="commandAction">The command to execute.</param>
                /// <param name="arguments">List of the names of the arguments this command takes. Can be null if this command takes no arguments. Argument names must not contain spaces!</param>
                /// <param name="switches">List of the switches this command can take. Can be null if this command takes no switches. Switches must not contain spaces!</param>
                public Command(string commandName, DelegateCommand commandAction, IEnumerable<string> arguments=null, IEnumerable<string> switches = null)
                {
                    CommandName = commandName;
                    CommandAction = commandAction;
                    if (arguments!=null)
                    {
                        Arguments = new List<string>(arguments);
                    }
                    if (switches != null)
                    {
                        Switches = new List<string>(switches);
                    }
                }
            }

            #region Exceptions

            /// <summary>
            /// Occours when the arguments could not be parsed.
            /// </summary>
            public class CommandCouldNotBeParsedException : Exception {
                public CommandCouldNotBeParsedException(string message) : base(message) { }
            }

            /// <summary>
            /// Occours when a command was not found in the list of commands.
            /// </summary>
            public class CommandNotFoundException : Exception {
                public CommandNotFoundException(string message) : base(message) { }
            }

            /// <summary>
            /// Occours when a command got invalid arguments.
            /// </summary>
            public class CommandInvalidArgumentsException : Exception
            {
                public CommandInvalidArgumentsException(string message) : base(message) { }
            }

            #endregion

            #endregion

            #region Private fields

            private List<Command> commands;

            private MyCommandLine commandLine;

            #endregion

            #region Private methods

            private void AddHelpCommands(string commandName, string argumentName)
            {
                AddCommand(new Command(commandName, (MyCommandLine line, out CommandInvalidArgumentsException e) => { e = null; ShowHelp(); }));
                AddCommand(new Command(
                    commandName: commandName,
                    commandAction: (MyCommandLine line, out CommandInvalidArgumentsException e) =>
                    {
                        e = null;
                        if (line.ArgumentCount == 2 && commands.Any(c => CommandComparer.Equals(c.CommandName, line.Argument(1))))
                        {
                            ShowHelp(line.Argument(1));
                        }
                        else
                        {
                            e = new CommandInvalidArgumentsException($"There is no command named '{line.Argument(1)}'.");
                        }
                    },
                    arguments: new List<string>(new string[] { argumentName })
                ));
            }

            #endregion

            #region Public properties

            /// <summary>
            /// The comparer used when trying to parse command name.
            /// </summary>
            public StringComparer CommandComparer { get; private set; }

            /// <summary>
            /// This method will be called with status messages such as notifications about command executions and failures.
            /// </summary>
            public Action<string> LogMethod { get; private set; }

            /// <summary>
            /// Whether or not to throw exceptions specific to this parser or only log failures via <see cref="LogMethod"/>.
            /// </summary>
            public bool ThrowExceptions { get; set; }

            /// <summary>
            /// Iterates through the list of commands.
            /// </summary>
            public IEnumerable<Command> Commands
            {
                get
                {
                    foreach (var cmd in commands)
                    {
                        yield return cmd;
                    }
                }
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Adds a command to the list of commands.
            /// </summary>
            /// <remarks>
            /// Command must not have the same name and argument count as any in the list of commands.
            /// </remarks>
            public void AddCommand(Command command)
            {
                if (!command.Valid)
                {
                    throw new Exception($"Can not add command with name '{command.CommandName}' because it is not a valid command!");
                }
                if (commands.Any(c=>c.ArgumentCount==command.ArgumentCount  && CommandComparer.Equals(c.CommandName,command.CommandName)))
                {
                    throw new Exception($"There is already a command with name '{command.CommandName}' and '{command.ArgumentCount}' arguments.");
                }
                commands.Add(command);
            }

            /// <summary>
            /// Removes the given command from the list of commands.
            /// </summary>
            /// <returns>Whether or not the command was removed. Also returns false when the command was not in the list.</returns>
            public bool RemoveCommand(Command command)
            {
                return commands.Remove(command);
            }

            /// <summary>
            /// Tries to parse the given argument as a command.
            /// </summary>
            /// <param name="showHelpOnFail">Whether or not to output a list of commands and their arguments if the command is not found or has an incorrect amount of arguments.</param>
            public void Parse(string argument, bool showHelpOnFail=true)
            {
                commandLine.Clear();
                if (!commandLine.TryParse(argument))
                {
                    string errorMsg = $"Could not parse argument: '{argument}'.";
                    LogMethod?.Invoke(errorMsg);
                    if (ThrowExceptions)
                    {
                        throw new CommandCouldNotBeParsedException(errorMsg);
                    }
                }
                else
                {
                    string cmdName = commandLine.Argument(0);
                    int argCount = commandLine.ArgumentCount - 1;

                    var commandsWithThisName = commands.FindAll(c => CommandComparer.Equals(cmdName,c.CommandName));
                    if (commandsWithThisName.Count<=0)
                    {
                        string errorMsg = $"There is no valid command with name: '{cmdName}'";
                        LogMethod?.Invoke(errorMsg);
                        if (showHelpOnFail)
                        {
                            ShowHelp();
                        }
                        if (ThrowExceptions)
                        {
                            throw new CommandNotFoundException(errorMsg);
                        }
                        return;
                    }

                    var command = commandsWithThisName.Find(c => c.ArgumentCount == argCount);
                    if (command==null)
                    {
                        string errorMsg = $"No command with name '{cmdName}' takes {argCount} arguments.";
                        LogMethod(errorMsg);
                        if (showHelpOnFail)
                        {
                            ShowHelp(cmdName);
                        }
                        if (ThrowExceptions)
                        {
                            throw new CommandInvalidArgumentsException(errorMsg);
                        }
                        return;
                    }

                    CommandInvalidArgumentsException e=null;
                    command.CommandAction?.Invoke(commandLine, out e);

                    if (!(e==null))
                    {
                        LogMethod(e.Message);
                        if (showHelpOnFail)
                        {
                            ShowHelp(cmdName);
                        }
                        if (ThrowExceptions)
                        {
                            throw e;
                        }
                    }
                }
            }

            /// <summary>
            /// Shows help for command(s).
            /// </summary>
            /// <param name="commandName">The name of the command to show help for. If set to null or is not valid, help will be shown for all commands.</param>
            public void ShowHelp(string commandName = null)
            {
                if (LogMethod==null)
                {
                    return;
                }
                string msg="";
                int listedCommandCount = 0;
                if (string.IsNullOrWhiteSpace(commandName))
                {
                    msg = "List of available commands:\n";
                    foreach (var cmd in commands.OrderBy(c=>c.CommandName))
                    {
                        listedCommandCount++;
                        msg += $"\n{cmd.CommandName}";
                        if (cmd.ArgumentCount>0)
                        {
                            foreach (var arg in cmd.Arguments)
                            {
                                msg +=$" '{arg}'";
                            }
                        }
                        if (cmd.SwitchCount>0)
                        {
                            foreach (var s in cmd.Switches)
                            {
                                msg +=$" [-{s}]";
                            }
                        }
                    }
                    if (listedCommandCount<=0)
                    {
                        msg += "\nThere are no commands.";
                    }
                }
                else
                {
                    msg = $"List of available commands with name '{commandName}':\n";
                    foreach (var cmd in commands.FindAll(c=>CommandComparer.Equals(c.CommandName,commandName)))
                    {
                        listedCommandCount++;
                        msg += $"\n{cmd.CommandName}";
                        if (cmd.ArgumentCount > 0)
                        {
                            foreach (var arg in cmd.Arguments)
                            {
                                msg += $" '{arg}'";
                            }
                        }
                        if (cmd.SwitchCount > 0)
                        {
                            foreach (var s in cmd.Switches)
                            {
                                msg += $" [-{s}]";
                            }
                        }
                    }
                    if (listedCommandCount <= 0)
                    {
                        msg += "\nThere are no such commands.";
                    }
                }
                LogMethod?.Invoke(msg);
            }

            /// <summary>
            /// Clears the command list.
            /// </summary>
            /// <param name="addHelpCommand">Whether or not to add help commands automatically.</param>
            /// <param name="helpCommandName">The name of the help command to add.<para>Only matters if <paramref name="addHelpCommand"/> is true.</para></param>
            /// <param name="helpCommandArgumentName">The name of the help command's argument to add.<para>Only matters if <paramref name="addHelpCommand"/> is true.</para></param>
            public void Clear(bool addHelpCommand=true, string helpCommandName = "help", string helpCommandArgumentName ="command_name")
            {
                commands.Clear();
                if (addHelpCommand)
                {
                    AddHelpCommands(helpCommandName, helpCommandArgumentName);
                }
            }

            #endregion

            /// <summary>
            /// Instantiates a command parser with custom command comparison rules.
            /// </summary>
            /// <param name="commandComparer">The comparer used when trying to parse command name.</param>
            /// <param name="logMethod">This method will be called with status messages such as notifications about command executions and failures.</param>
            /// <param name="throwExceptions">Whether or not to throw exceptions specific to this parser or only log failures via <see cref="LogMethod"/>.</param>
            public ArgCommandParser(StringComparer commandComparer, Action<string> logMethod=null, bool throwExceptions = false)
            {
                commandLine = new MyCommandLine();
                commands = new List<Command>();

                CommandComparer = commandComparer;
                LogMethod = logMethod;
                ThrowExceptions = throwExceptions;
            }

            /// <summary>
            /// Instantiates a command parser with a help command automatically included.
            /// </summary>
            /// <param name="commandComparer">The comparer used when trying to parse command name.</param>
            /// <param name="helpCommandName">The name of the help command to include.</param>
            /// <param name="helpCommandArgumentName">The name of the help command's optional argument.</param>
            /// <param name="logMethod">This method will be called with status messages such as notifications about command executions and failures.</param>
            /// <param name="throwExceptions">Whether or not to throw exceptions specific to this parser or only log failures via <see cref="LogMethod"/>.</param>
            public ArgCommandParser(StringComparer commandComparer, string helpCommandName, string helpCommandArgumentName="command_name", Action<string> logMethod = null, bool throwExceptions = false) : this(commandComparer, logMethod,throwExceptions)
            {
                AddHelpCommands(helpCommandName, helpCommandArgumentName);
            }

            /// <summary>
            /// Instantiates a command parser with default command comparison rules ('<see cref="StringComparer.OrdinalIgnoreCase"/>').
            /// </summary>
            /// <param name="logMethod">This method will be called with status messages such as notifications about command executions and failures.</param>
            /// <param name="throwExceptions">Whether or not to throw exceptions specific to this parser or only log failures via <see cref="LogMethod"/>.</param>
            public ArgCommandParser(Action<string> logMethod = null, bool throwExceptions = false) : this(StringComparer.OrdinalIgnoreCase,logMethod,throwExceptions)
            {}

            /// <summary>
            /// Instantiates a command parser with a help command automatically included and default command comparison rules ('<see cref="StringComparer.OrdinalIgnoreCase"/>').
            /// </summary>
            /// <param name="helpCommandName">The name of the help command to include.</param>
            /// <param name="helpCommandArgumentName">The name of the help command's optional argument.</param>
            /// <param name="logMethod">This method will be called with status messages such as notifications about command executions and failures.</param>
            /// <param name="throwExceptions">Whether or not to throw exceptions specific to this parser or only log failures via <see cref="LogMethod"/>.</param>
            public ArgCommandParser(string helpCommandName, string helpCommandArgumentName = "command_name", Action<string> logMethod = null, bool throwExceptions = false) : this(StringComparer.OrdinalIgnoreCase, logMethod, throwExceptions)
            {
                AddHelpCommands(helpCommandName, helpCommandArgumentName);
            }
        }
    }
}
