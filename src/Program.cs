using System;
using System.Linq;

namespace UniGet
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            var command = args[0].ToLower();
            switch (command)
            {
                case "restore":
                    return RestoreTool.Run(args.Skip(1).ToArray());

                case "pack":
                    return PackTool.Run(args.Skip(1).ToArray());

                default:
                    Console.WriteLine("Wrong command: " + command);
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage!");
        }
    }
}
