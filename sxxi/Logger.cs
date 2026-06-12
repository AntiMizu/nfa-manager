using System;
using System.IO;

namespace sxxi
{
    public static class Logger
    {
        private static readonly object _lock = new object();

        private static string GetLogPath()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, $"sxxi_{DateTime.Now:yyyy-MM-dd}.log");
        }

        public static void Error(string message)
        {
            lock (_lock)
            {
                try
                {
                    string entry = $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}{Environment.NewLine}";
                    File.AppendAllText(GetLogPath(), entry);
                }
                catch { }
            }
        }

        public static void Error(Exception ex)
        {
            Error(ex.ToString());
        }

        public static void Info(string message)
        {
            lock (_lock)
            {
                try
                {
                    string entry = $"[{DateTime.Now:HH:mm:ss}] INFO: {message}{Environment.NewLine}";
                    File.AppendAllText(GetLogPath(), entry);
                }
                catch { }
            }
        }
    }
}
