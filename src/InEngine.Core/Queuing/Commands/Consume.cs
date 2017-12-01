﻿using System;
using CommandLine;

namespace InEngine.Core.Queuing.Commands
{
    public class Consume : AbstractCommand
    {
        [Option("all", HelpText = "Consume all the messages in the primary or secondary queue.")]
        public bool ShouldConsumeAll { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Consume from the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            var queue = Queue.Make(UseSecondaryQueue);
            var shouldConsume = true;
            while (shouldConsume)
                shouldConsume = queue.Consume() && ShouldConsumeAll;
        }

        public override void Failed(Exception exception)
        {
            Error(exception.Message);
        }
    }
}