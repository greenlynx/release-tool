using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using ReleaseTool.Core;

namespace ReleaseTool
{
    public class Program
    {
        private static void Log(object message = null)
        {
            Console.WriteLine($"{message}");
        }

        private static void LogError(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log($"ERROR! {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.CancelKeyPress += Console_CancelKeyPress;

                var config = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddOptions();
                serviceCollection.Configure<Settings>(config);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;

                var command = args.Where(x => !x.StartsWith("-")).FirstOrDefault();

                new CommandHandler(x => Log(x)).Handle(command, settings);
            }
            catch (ErrorException ex)
            {
                LogError(ex.Message);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
#endif
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Environment.Exit(-1);
        }
    }
}
