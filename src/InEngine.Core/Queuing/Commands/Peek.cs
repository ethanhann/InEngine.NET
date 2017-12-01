﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using InEngine.Core.Exceptions;
using Konsole;
using Konsole.Forms;
using Newtonsoft.Json;

namespace InEngine.Core.Queuing.Commands
{
    public class Peek : AbstractCommand
    {
        [Option("from", DefaultValue = 0, HelpText = "The first message to peek at (0-indexed).")]
        public long From { get; set; }

        [Option("to", DefaultValue = 10, HelpText = "The last message to peek at.")]
        public long To { get; set; }

        [Option("json", HelpText = "View the messages as JSON.")]
        public bool JsonFormat { get; set; }

        [Option("pending", HelpText = "Peek at messages in the pending queue.")]
        public bool PendingQueue { get; set; }

        [Option("failed", HelpText = "Peek at messages in the failed queue.")]
        public bool FailedQueue { get; set; }

        [Option("in-progress", HelpText = "Peek at messages in the in-progress queue.")]
        public bool InProgressQueue { get; set; }

        [Option("secondary", HelpText = "Peek at messages in secondary queues. Primary queues are used by default.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            if (From < 0)
                throw new ArgumentException("--from cannot be negative");
            if (To < 0)
                throw new ArgumentException("--to cannot be negative");
            if (To < From)
                throw new ArgumentException("--from cannot be greater than --to");
            
            if (PendingQueue == false && FailedQueue == false && InProgressQueue == false)
                throw new CommandFailedException("Must specify at least one queue to peek in. Use -h to see available options.");
            var broker = Queue.Make(UseSecondaryQueue);
            if (PendingQueue) {
                PrintMessages(broker.PeekPendingMessages(From, To), "Pending");
            }
            if (InProgressQueue) {
                PrintMessages(broker.PeekInProgressMessages(From, To), "In-progress");
            }
            if (FailedQueue) {
                PrintMessages(broker.PeekFailedMessages(From, To), "Failed");
            }
        }

        public void PrintMessages(List<IMessage> messages, string queueName)
        {
            WarningText($"{queueName}:");
            if (!messages.Any()) {
                Line(" no messages available.");
            }
         
            Newline();

            var konsoleForm = new Form(120, new ThinBoxStyle());
            messages.ForEach(x => {
                var message = x as IMessage;
                if (JsonFormat)
                    Line(message.SerializeToJson());
                else
                    konsoleForm.Write(Queue.ExtractCommandInstanceFromMessage(message));
            });
        }
    }
}