using System;
using System.Threading;
using System.Threading.Tasks;
using MinImage.CommandProcessing;
using ImSh = SixLabors.ImageSharp;

namespace Frontend
{
    internal class Program
    {
        private static readonly object ConsoleLock = new object(); // Lock object for thread-safe console writes

        static async Task Main(string[] args)
        {
            ImSh.Configuration.Default.PreferContiguousImageBuffers = true;
            var cts = new CancellationTokenSource();
            CommandProcessor commandProcessor = new CommandProcessor(cts.Token);

            // Start a background task to listen for cancellation
            var cancellationTask = Task.Run(() =>
            {
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;
                        if (key == ConsoleKey.X)
                        {
                            SafeConsoleWriteLine("\nExit execution requested!");
                            cts.Cancel();
                            break;
                        }
                    }
                }
            });

            // Rest of your main logic
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    SafeConsoleWriteLine("Type \"Help\" to list available options");
                    SafeConsoleWriteLine("Enter command:");
                    string? input = Console.ReadLine();

                    if (cts.Token.IsCancellationRequested)
                    {
                        Console.Clear();
                        break;
                    }

                    // Your command processing logic here
                    bool result = await commandProcessor.ProcessCommandAsync(input);
                }
            }
            finally
            {
                // Await the cancellation task to ensure proper termination
                await cancellationTask;
                SafeConsoleWriteLine("Cancellation task completed.");
            }

            SafeConsoleWriteLine("Program terminated.            ");
        }

        /// <summary>
        /// Thread-safe method to write a line to the console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        private static void SafeConsoleWriteLine(string message)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Thread-safe method to write to the console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        private static void SafeConsoleWrite(string message)
        {
            lock (ConsoleLock)
            {
                Console.Write(message);
            }
        }
    }
}
