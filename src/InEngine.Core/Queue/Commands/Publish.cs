﻿using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Publish : AbstractCommand
    {
        [Option("command-assembly", DefaultValue = "InEngine.Core.dll")]
        public string CommandAssembly { get; set; }

        [Option("command-class", DefaultValue = "InEngine.Core.Queue.Commands.Null")]
        public string CommandClass { get; set; }

        [OptionArray("args", HelpText = "The list of arguments for the published command.")]
        public string[] Arguments { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Publish to a secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public ICommand Command { get; set; }

        public override CommandResult Run()
        {
            var command = Command ?? Plugin.LoadFrom(CommandAssembly).CreateCommandInstance(CommandClass);
            if (command == null)
                return new CommandResult(false, "Did not publish message. Could not load command from plugin.");

            if (Arguments != null)
                Parser.Default.ParseArguments(Arguments.ToList().Select(x => $"--{x}").ToArray(), command);

            Broker.Make().Publish(command, UseSecondaryQueue);
            return new CommandResult(true, "Published");
        }
    }
}
