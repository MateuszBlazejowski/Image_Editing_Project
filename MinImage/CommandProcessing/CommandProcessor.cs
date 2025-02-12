using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MinImage.CommandProcessing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImSh = SixLabors.ImageSharp;
using MinImage.ImageProcessing;
using MinImage.ImageGenerating;
using MinImage.AdditionalFuncionalities;
using MinImage;
using System.Security.Cryptography;

namespace Frontend
{
    public class CommandProcessor
    {
        private int imagesCount;
        private readonly ImageGenerator _imageGenerator;
        private readonly ImageBlurrer _imageBlurrer;
        private readonly ImageRandomCircles _imageRandomCircles;
        private readonly ImageGammaCorrector _imageGammaCorrector;
        private readonly ImageColorCorrector _imageColorCorrector;
        private readonly ImageRoomDrawer _imageRoomDrawer;


        private readonly CommandChainValidator _commandChainValidator;
        private readonly ProgressReporter _progressReporter;
        private readonly Saver _saver;
        private ConcurrentDictionary<int, Picture> _imageDictionary;

        private readonly CancellationToken _cancellationToken;
        private string? path = null;

        public CommandProcessor(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _imageGenerator = new ImageGenerator();
            _imageRandomCircles = new ImageRandomCircles();
            _imageBlurrer = new ImageBlurrer();
            _imageGammaCorrector = new ImageGammaCorrector();
            _imageColorCorrector = new ImageColorCorrector();
            _imageRoomDrawer = new ImageRoomDrawer();


            _commandChainValidator = new CommandChainValidator();
            _progressReporter = new ProgressReporter();
            _saver = new Saver();
            _imageDictionary = new ConcurrentDictionary<int, Picture>();

            // Subscribe to progress updates
            _imageGenerator.ProgressUpdated += (imageId, progress) =>
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                _progressReporter.UpdateWorkerProgress(imageId, progress, "Generating...                 ");
            };

            _imageBlurrer.ProgressUpdated += (imageId, progress) =>
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                _progressReporter.UpdateWorkerProgress(imageId, progress, "Blurring...                  ");
            };

            _imageRandomCircles.ProgressUpdated += (imageId, progress) =>
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                _progressReporter.UpdateWorkerProgress(imageId, progress, "Drawing Circles...           ");
            };

            _imageGammaCorrector.ProgressUpdated += (imageId, progress) =>
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                _progressReporter.UpdateWorkerProgress(imageId, progress, "Gamma Correcting...         ");
            };

            _imageColorCorrector.ProgressUpdated += (imageId, progress) =>
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                _progressReporter.UpdateWorkerProgress(imageId, progress, "Color Correcting...         ");
            };

            _imageRoomDrawer.ProgressUpdated += (imageId, progress) =>
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                _progressReporter.UpdateWorkerProgress(imageId, progress, "Drawing Room...            ");
            };
        }

        public async Task<bool> ProcessCommandAsync(string input)
        {
            Console.CursorVisible = false;

            _imageDictionary.Clear();
            _progressReporter.Restart();
            if (_cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Operation canceled by user.");
                return false;
            }

            var commands = input.Split('|', StringSplitOptions.TrimEntries);
            if (commands.Count() == 1 && commands[0].Equals("Help"))
            {
                PrintHelp();
                return false;
            }
            if (commands.Count() == 1)
            {
                var _commands = input.Split(' ', StringSplitOptions.TrimEntries);
                if (_commands.Count() == 2 && _commands[0] == "ChangePath")
                {
                    ChangePath(_commands[1]);
                    return false;
                }
            }
            int commandCount = _commandChainValidator.ValidateCommandChain(input, out imagesCount);
            if (commandCount == -1)
            {
                Console.WriteLine("Error: Invalid command chain.");
                return false;
            }


            // Initialize image dictionary and progress workers
            for (int i = 0; i < imagesCount; i++)
            {
                _imageDictionary.TryAdd(i, new Picture());
            }
            _progressReporter.InitializeWorkers(imagesCount, commandCount);

            // Start all tasks concurrently
            var tasks = _imageDictionary.Keys.Select(imageId =>
                Task.Run(() => ProcessImagePipelineAsync(imageId, commands, _cancellationToken), _cancellationToken)
            );

            try
            {
                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Command processing canceled.");
                return false;
            }

            if (!_cancellationToken.IsCancellationRequested)
            {
                _saver.SaveAllImages(_imageDictionary, path);
            }

            Console.CursorVisible = true;
            return true;

        }


        private async Task ProcessImagePipelineAsync(int imageId, string[] commands, CancellationToken token)
        {
            foreach (var command in commands)
            {
                if (token.IsCancellationRequested)
                    break;

                var picture = _imageDictionary[imageId];
                if (!await ProcessSingleCommandAsync(imageId, command, picture, token))
                {
                    Console.WriteLine($"Error processing command: {command} for Image {imageId}");
                    return;
                }
            }
        }


        private async Task<bool> ProcessSingleCommandAsync(int imageId, string command, Picture picture, CancellationToken token)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;



            return parts[0] switch
            {
                "Generate" => await ProcessGenerateCommandAsync(imageId, parts, token),
                "Input" => await ProcessInputCommandAsync(imageId, parts, token),
                "Blur" => await ProcessBlurCommandAsync(imageId, parts, picture.Image, token),
                "RandomCircles" => await ProcessRandomCirclesCommandAsync(imageId, parts, picture.Image, token),
                "ColorCorrection" => await ProcessColorCorrectionCommandAsync(imageId, parts, picture.Image, token),
                "GammaCorrection" => await ProcessGammaCorrectionCommandAsync(imageId, parts, picture.Image, token),
                "Room" => await ProcessRoomCommandAsync(imageId, parts, picture.Image, token),
                "Output" => await ProcessOutputCommandAsync(imageId, parts, picture, token),
                "Help" => false,
                _ => throw new InvalidOperationException($"Unknown command: {parts[0]}")
            };

        }


        private Task<bool> ProcessGenerateCommandAsync(int imageId, string[] parts, CancellationToken token)
        {
            if (parts.Length != 4 ||
                !int.TryParse(parts[2], out int width) ||
                !int.TryParse(parts[3], out int height))
            {
                Console.WriteLine("Invalid syntax. Use: Generate <width> <height>");
                return Task.FromResult(false);
            }

            // Generate image and update progress
            var image = _imageGenerator.GenerateSingleImage(width, height, imageId, token);
            _progressReporter.CommandFinished(imageId);
            var picture = new Picture(image, null);
            _imageDictionary[imageId] = picture;
            return Task.FromResult(true);
        }

        private Task<bool> ProcessBlurCommandAsync(int imageId, string[] parts, ImSh.Image<Rgba32>? image, CancellationToken token)
        {
            if (parts.Length != 3 ||
                !int.TryParse(parts[1], out int blurWidth) ||
                !int.TryParse(parts[2], out int blurHeight))
            {
                Console.WriteLine("Invalid syntax. Use: Blur <width> <height>");
                return Task.FromResult(false);
            }

            if (image == null)
            {
                Console.WriteLine($"Error: Image with ID {imageId} is not initialized.");
                return Task.FromResult(false);
            }

            try
            {
                // Apply blur and update progress
                _imageBlurrer.ApplyBlur(image, blurWidth, blurHeight, imageId, token);
                _progressReporter.CommandFinished(imageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying blur: {ex.Message}");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ProcessRandomCirclesCommandAsync(int imageId, string[] parts, ImSh.Image<Rgba32>? image, CancellationToken token)
        {
            if (parts.Length != 3 ||
                !int.TryParse(parts[1], out int numCircles) ||
                !int.TryParse(parts[2], out int radius))
            {
                Console.WriteLine("Invalid syntax. Use: RandomCircles <numCircles> <radius>");
                return Task.FromResult(false);
            }

            if (image == null)
            {
                Console.WriteLine($"Error: Image with ID {imageId} is not initialized.");
                return Task.FromResult(false);
            }

            try
            {
                _imageRandomCircles.ApplyRandomCircles(image, numCircles, radius, imageId, token);
                _progressReporter.CommandFinished(imageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing circles: {ex.Message}");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ProcessColorCorrectionCommandAsync(int imageId, string[] parts, Image<Rgba32>? image, CancellationToken token)
        {
            if (parts.Length != 4 ||
                !float.TryParse(parts[1], out float red) ||
                !float.TryParse(parts[2], out float green) ||
                !float.TryParse(parts[3], out float blue))
            {
                Console.WriteLine("Invalid syntax. Use: ColorCorrection <red> <green> <blue>");
                return Task.FromResult(false);
            }

            if (image == null)
            {
                Console.WriteLine($"Error: Image with ID {imageId} is not initialized.");
                return Task.FromResult(false);
            }

            try
            {
                _imageColorCorrector.ApplyColorCorrection(image, red, green, blue, imageId, token);
                _progressReporter.CommandFinished(imageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying color correction: {ex.Message}");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ProcessGammaCorrectionCommandAsync(int imageId, string[] parts, Image<Rgba32>? image, CancellationToken token)
        {
            if (parts.Length != 2 || !float.TryParse(parts[1], out float gamma))
            {
                Console.WriteLine("Invalid syntax. Use: GammaCorrection <gamma>");
                return Task.FromResult(false);
            }

            if (image == null)
            {
                Console.WriteLine($"Error: Image with ID {imageId} is not initialized.");
                return Task.FromResult(false);
            }

            try
            {
                _imageGammaCorrector.ApplyGammaCorrection(image, gamma, imageId, token);
                _progressReporter.CommandFinished(imageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying gamma correction: {ex.Message}");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ProcessRoomCommandAsync(int imageId, string[] parts, Image<Rgba32>? image, CancellationToken token)
        {
            if (parts.Length != 5 ||
                !float.TryParse(parts[1], out float x1) ||
                !float.TryParse(parts[2], out float y1) ||
                !float.TryParse(parts[3], out float x2) ||
                !float.TryParse(parts[4], out float y2))
            {
                Console.WriteLine("Invalid syntax. Use: Room <x1> <y1> <x2> <y2>");
                return Task.FromResult(false);
            }

            if (image == null)
            {
                Console.WriteLine($"Error: Image with ID {imageId} is not initialized.");
                return Task.FromResult(false);
            }

            try
            {
                _imageRoomDrawer.DrawRoom(image, x1, y1, x2, y2, imageId, token);
                _progressReporter.CommandFinished(imageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing room: {ex.Message}");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private async Task<bool> ProcessInputCommandAsync(int imageId, string[] parts, CancellationToken token)
        {
            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid syntax. Use: Input <filename>");
                return false;
            }

            // Construct the absolute path
            string outputFolder = Path.Combine(Environment.CurrentDirectory, "Output");
            // string filePath = Path.Combine(outputFolder, parts[1].Trim());
            string filePath = parts[1];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File '{filePath}' does not exist in the output folder.");
                return false;
            }

            try
            {
                // Report initial progress
                _progressReporter.UpdateWorkerProgress(imageId, 0, "Loading Image           ");

                // Load the image as Rgba32
                using var image = await ImSh.Image.LoadAsync<Rgba32>(filePath, token);

                if (image == null || image.Width == 0 || image.Height == 0)
                {
                    Console.WriteLine($"Error: Failed to load a valid image from '{filePath}'.");
                    return false;
                }

                // Report mid-progress
                _progressReporter.UpdateWorkerProgress(imageId, 50, "Processing Image           ");

                // Initialize Picture and store it
                var picture = new Picture(image.CloneAs<Rgba32>(), null);
                _imageDictionary[imageId] = picture;

                // Finalize progress reporting
                _progressReporter.UpdateWorkerProgress(imageId, 100, "Image Loaded        ");
                _progressReporter.CommandFinished(imageId);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return false;
            }
        }



        private Task<bool> ProcessOutputCommandAsync(int imageId, string[] parts, Picture picture, CancellationToken token)
        {
            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid syntax. Use: Output <filename_prefix>");
                return Task.FromResult(false);
            }

            if (picture == null || picture.Image == null)
            {
                Console.WriteLine($"Error: Image with ID {imageId} is not initialized.");
                return Task.FromResult(false);
            }

            try
            {
                _progressReporter.UpdateWorkerProgress(imageId, 0, "Naming File       ");
                string prefix = parts[1];
                picture.FilePrefix = prefix;
                _progressReporter.UpdateWorkerProgress(imageId, 100, "Naming File       ");
                _progressReporter.CommandFinished(imageId);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error naming file: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private void ChangePath(string _path)
        {
            if (Directory.Exists(_path))
            {
                path = _path;
                Console.WriteLine("New path set");
            }
            else
            {
                Console.WriteLine("Directory does not exist, aborting...");
            }
        }
        private void PrintHelp()
        {
            Console.WriteLine(@"
List of available commands:

Generating commands:

Generate <imagesnumber> <width> <height> - Generate an image of the specified size.
Input <file_name> - Load an image from the output folder with the given name.

Processing commands:

Blur <width> <height> - Apply a blur effect to the image.
Output <filename_prefix> - Save the image with the given filename prefix.
RandomCircles <numCircles> <radius> - Add random circles to the image.
ColorCorrection <red> <green> <blue> - Apply color correction by adding red, green, and blue values.
GammaCorrection <gamma> - Apply gamma correction with the specified gamma value.
Room <x1> <y1> <x2> <y2> - Draw a filled rectangle with the given coordinates, given form 0 to 1.

Command syntax should be as follows:
<Generating command> | <Processing Command> | <Processing Command>

Generating command at the beginning is mandatory, all following commands are not, however, only one generating command is allowed.
Next commands should be typed after '|', the number of processing commands is not limited.

During execution, press 'x' to abort and terminate the program.

Other commands:

ChangePath ""path"" - Set the path to the folder where images will be saved (default: in the binaries of MinImage).
");
        }


    }
}
