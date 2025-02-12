using System;
using System.Collections.Generic;

namespace MinImage.CommandProcessing
{
    public class CommandChainValidator
    {
        private static readonly HashSet<string> ValidCommands = new HashSet<string>
        {
            "Blur",        // Processing command
            "Output",      // Processing command
            "RandomCircles", // processing command
            "ColorCorrection",
            "GammaCorrection",
            "Room",
            "Generate",    // Generating command
            "Input"

        };

        private static readonly HashSet<string> GeneratingCommands = new HashSet<string>
        {
            "Generate",
            "Input"

        };

        /// <summary>
        /// Validates a chain of commands and retrieves the number of images from the generating command.
        /// </summary>
        public int ValidateCommandChain(string commandChain, out int imagesCount)
        {
            imagesCount = 0;

            if (string.IsNullOrWhiteSpace(commandChain))
            {
                Console.WriteLine("Error: Command chain is empty.");
                return -1;
            }

            var commands = commandChain.Split('|', StringSplitOptions.TrimEntries);
            if (commands.Length == 0)
            {
                Console.WriteLine("Error: Command chain is empty.");
                return -1;
            }

            int commandCount = 0;
            bool hasGeneratingCommand = false;

            foreach (var command in commands)
            {
                if (IsHelpCommand(command))
                {
                    continue;
                }

                // Check if the first non-Help command is a generating command
                if (!hasGeneratingCommand)
                {
                    if (!IsGeneratingCommand(command, out imagesCount))
                    {
                        Console.WriteLine("Error: The first valid command must be a generating command.");
                        return -1;
                    }
                    hasGeneratingCommand = true;
                    commandCount++;
                    continue;
                }

                if (!ValidateSingleCommand(command, hasGeneratingCommand, out _, ref commandCount))
                {
                    Console.WriteLine($"Error: Invalid command in chain: '{command}'");
                    return -1;
                }

                commandCount++;
            }

            if (!hasGeneratingCommand)
            {
                Console.WriteLine("Error: The chain must contain a generating command.");
                return -1;
            }

            return commandCount;
        }

        /// <summary>
        /// Validates a single command.
        /// </summary>
        private bool ValidateSingleCommand(string command, bool hasGeneratingCommand, out int generatedImages, ref int commandCount)
        {
            generatedImages = 0;


            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string commandName = parts[0];

            if (!ValidCommands.Contains(commandName))
            {
                return false;
            }

            if (IsGeneratingCommand(command, out generatedImages))
            {
                return !hasGeneratingCommand;
                
            }


            return commandName switch
            {
                "Blur" => parts.Length == 3 && int.TryParse(parts[1], out _) && int.TryParse(parts[2], out _),
                "Output" => parts.Length == 2,
                "RandomCircles" => parts.Length == 3 && int.TryParse(parts[1], out _) && int.TryParse(parts[2], out _),
                "ColorCorrection" => parts.Length == 4 && float.TryParse(parts[1], out _) && float.TryParse(parts[2], out _) && float.TryParse(parts[3], out _),
                "GammaCorrection" => parts.Length == 2 && float.TryParse(parts[1], out _),
                "Room" => parts.Length == 5 && float.TryParse(parts[1], out _) && float.TryParse(parts[2], out _) &&
                 float.TryParse(parts[3], out _) && float.TryParse(parts[4], out _),
                _ => false
            };
        }

        /// <summary>
        /// Checks if the given command is a generating command and extracts the number of images.
        /// </summary>
        private bool IsGeneratingCommand(string command, out int imagesCount)
        {
            imagesCount = 0;

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            if (GeneratingCommands.Contains(parts[0]))
            {
                if (parts[0] == "Generate" && parts.Length == 4 && int.TryParse(parts[1], out imagesCount))
                {
                    // The Generate command
                    return true;
                }
                else if (parts[0] == "Input" && parts.Length == 2)
                {
                    // The Input command
                    imagesCount = 1; // Input always processes a single image
                    return true;
                }
            }

            return false;
        }


        public bool IsHelpCommand(string command)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 1) return false;
            string commandName = parts[0];
            return commandName == "Help";
        }
    }
}
