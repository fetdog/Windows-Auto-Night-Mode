﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoDarkModeSvc.Communication;
using Sharprompt;

namespace AutoDarkModeComms
{
    class Program
    {
        private static Version Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version;

        public const string QuitShell = "QuitShell";
        static void Main(string[] args)
        {
            Console.WriteLine($"Auto Dark Mode Shell version {Version.Major}.{Version.Minor}");
            IMessageClient client = new PipeClient();
            if (args.Length > 0)
            {
                if (args.ToList().Contains("--and-launch-service"))
                {
                    Console.WriteLine($"attempting to start service");
                    {
                        using Process svc = new();
                        svc.StartInfo.UseShellExecute = false;
                        svc.StartInfo.FileName = GetExecutionPathService();
                        _ = svc.Start();
                    }
                }
                int timeoutDefault = 10;
                Console.WriteLine(args[0]);
                if (args.Length == 2)
                {
                    Console.WriteLine($"custom timeout: {args[1]}s");
                    bool success = int.TryParse(args[1], out timeoutDefault);
                    if (!success) timeoutDefault = 10;
                }
                Console.WriteLine($"Result: {client.SendMessageAndGetReply(args[0], timeoutSeconds: timeoutDefault)}");
                Console.WriteLine("Please check service.log for more details");
                Environment.Exit(0);
            }
            var flags = BindingFlags.Static | BindingFlags.Public;
            List<string> fields = typeof(Command).GetFields(flags)
                .Where(p => p.IsDefined(typeof(IncludableAttribute)))
                .Select(f => $"{f.Name} ({(string)typeof(Command).GetField(f.Name).GetValue(null)})")
                .ToList();
            fields.Add(QuitShell);
            string selection = "";
            do
            {
                selection = Prompt.Select("Select a command", fields);
                selection = selection.Split("(")[0].Trim();
                if (selection != QuitShell)
                {
                    selection = (string)typeof(Command).GetField(selection).GetValue(null);
                    Console.WriteLine($"Result: {client.SendMessageAndGetReply(selection, timeoutSeconds: 15)}");
                    Console.WriteLine("Please check service.log for more details");
                }
            }
            while (selection != QuitShell);

        }
        public static string GetExecutionPathService()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeSvc.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }
    }
}
