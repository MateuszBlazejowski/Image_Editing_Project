using System;
using System.Collections.Generic;
using System.Text;

namespace MinImage.AdditionalFuncionalities
{
    public class ProgressReporter
    {
        private readonly object _lock = new(); // Lock for thread safety
        private int _commandsNumber;
        private readonly int _barSize;
        private Dictionary<int, WorkerData> _workers;

        public ProgressReporter(int barSize = 50)
        {
            _barSize = barSize;
            _workers = new Dictionary<int, WorkerData>();
        }

        public void InitializeWorkers(int totalWorkers, int commandsNumber)
        {
            lock (_lock)
            {
                _commandsNumber = commandsNumber;
                for (int i = 0; i < totalWorkers; i++)
                {
                    _workers[i] = new WorkerData();
                }
                Console.Clear(); // Clear the console before starting
            }
        }

        public void Restart()
        {
            _commandsNumber = 0;
            _workers.Clear();
        }
        public void CommandFinished(int workerId)
        {
            lock (_lock)
            {
                if (_workers.ContainsKey(workerId))
                {
                    _workers[workerId].CommandsFinished++;
                    if (_workers[workerId].CommandsFinished == _commandsNumber)
                        _workers[workerId].Progress = 100;
                }
                Redraw();
            }
        }

        public void UpdateWorkerProgress(int workerId, int progress, string message = "")
        {
            lock (_lock)
            {
                if (_workers.ContainsKey(workerId))
                {
                    // Update progress considering finished commands
                    _workers[workerId].Progress = progress / _commandsNumber +
                                                  _workers[workerId].CommandsFinished * 100 / _commandsNumber;
                    _workers[workerId].Message = message;
                    Redraw();
                }
            }
        }

        private void Redraw()
        {
            // Redraw all progress bars safely
            lock (_lock)
            {
                Console.SetCursorPosition(0, 0);
                var sb = new StringBuilder();
                sb.AppendLine($"Generating {_workers.Count} images...");

                foreach (var (workerId, workerData) in _workers)
                {
                    var progressBar = DrawProgressBar(workerData.Progress, 100, _barSize);
                    sb.AppendLine($" Image: {workerId + 1,-10} {progressBar}  {workerData.Message}");
                }

                Console.Write(sb.ToString());
            }
        }

        private string DrawProgressBar(int progress, int total, int barSize)
        {
            double percentage = (double)progress / total;
            int filled = (int)Math.Round(percentage * barSize);

            char[] barChars = new char[barSize];
            for (int i = 0; i < barSize; i++)
            {
                barChars[i] = i < filled ? '#' : '-';
            }

            // Add dividers for commands
            for (int i = barSize / _commandsNumber; i < barSize - barSize % _commandsNumber; i += barSize / _commandsNumber)
            {
                barChars[i] = '|';
            }

            string progressBar = $"[{new string(barChars)}]";
            string percentageText = $"{progress}%";

            return $"{progressBar} {percentageText}";
        }
    }

    public record WorkerData
    {
        public int Progress { get; set; } = 0;
        public int CommandsFinished { get; set; } = 0;
        public string Message { get; set; } = "";
    }
}
