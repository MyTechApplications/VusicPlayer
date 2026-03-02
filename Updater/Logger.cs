using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    public static class Logger
    {
        private static string logPath =
            Path.Combine(AppContext.BaseDirectory, "vusicplayer.log");

        public static void Log(string message, string source, LogLevelType logLevelType)
        {
            string logLine =
                  $"{DateTime.Now:O}|{logLevelType}|{source}|{message}";

            File.AppendAllText(logPath,
                source);
            LogAdded?.Invoke(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = logLevelType,
                Source = source,
                Message = message
            });
        }
        public static event Action<LogEntry>? LogAdded;
        public enum LogLevelType
        {
            Information,
            Warning,
            Error, Success
        }
        public static List<LogEntry> GetLogDetailsList()
        {
            var list = new List<LogEntry>();

            if (!File.Exists(logPath))
                return list;

            var lines = File.ReadAllLines(logPath);

            foreach (var line in lines)
            {
                var parts = line.Split('|');

                if (parts.Length != 4)
                    continue;

                if (!DateTime.TryParse(parts[0], out DateTime timestamp))
                    continue;

                if (!Enum.TryParse(parts[1], out LogLevelType level))
                    continue;

                list.Add(new LogEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    Source = parts[2],
                    Message = parts[3]
                });
            }

            return list;
        }


    }
}
