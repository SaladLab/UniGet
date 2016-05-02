using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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
                case "pack":
                    return PackTool.Run(args.Skip(1).ToArray());

                case "remove":
                    return RemoveTool.Run(args.Skip(1).ToArray());

                case "restore":
                    return RestoreTool.Run(args.Skip(1).ToArray());

                default:
                    Console.WriteLine("Wrong command: " + command);
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("UniGet (https://github.com/SaladLab/UniGet) " + GetVersion());
            Console.WriteLine("usage: UniGet command [...]");
            Console.WriteLine("command: pack");
            Console.WriteLine("         remove");
            Console.WriteLine("         restore");
        }

        private static string GetVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi.FileVersion;
            }
            catch (Exception)
            {
                return "None";
            }
        }
    }
}
