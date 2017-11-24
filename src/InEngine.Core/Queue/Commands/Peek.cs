﻿using System;
using CommandLine;
using InEngine.Core.Exceptions;
using Konsole;
using Konsole.Forms;
using Newtonsoft.Json;

namespace InEngine.Core.Queue.Commands
{
    public class Peek : AbstractCommand
    {
        [Option("offset", DefaultValue = 0, HelpText = "The maximum number of messages to peek.")]
        public long Offset { get; set; }

        [Option("limit", DefaultValue = 10, HelpText = "The maximum number of messages to peek.")]
        public long Limit { get; set; }

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
            if (PendingQueue == false && FailedQueue == false && InProgressQueue == false)
                throw new CommandFailedException("Must specify at least one queue to peek in. Use -h to see available options.");
            var broker = Broker.Make(UseSecondaryQueue);
            var from = Offset;
            var to = Offset + Limit - 1;
            if (PendingQueue) {
                Warning("Pending:");
                var konsoleForm = new Form(120, new ThinBoxStyle());
                broker.PeekPendingMessages(from, to).ForEach(x => {
                    var message = x as Message;
                    if (JsonFormat)
                        Line(message.SerializeToJson());
                    else
                        konsoleForm.Write(broker.ExtractCommandInstanceFromMessage(message));
                });
            }
            if (InProgressQueue)
                Info($"In-progress: {broker.PeekInProgressMessages(from, to).ToString()}");
            if (FailedQueue)
                Info($"Failed: {broker.PeekFailedMessages(from, to).ToString()}");
        }
    }
}
