using System;
using System.IO;
using System.Text;
using Godot;
using File = System.IO.File;

namespace OsuSkinMixer
{
    public static class Logger
    {
        private static StringBuilder LogContent { get; } = new StringBuilder();

        public static void Init()
        {
            try
            {
                if (Settings.Content.LogToFile)
                    File.WriteAllText(Settings.LogFilePath, $"------- osu! skin mixer {Settings.VERSION} -------\n{LogContent}");
            }
            catch (Exception ex)
            {
                OS.Alert($"Unable to write to log file: {ex.Message}\n\nLogging will be disabled.");
                Settings.Content.LogToFile = false;
                Settings.Save();
            }
        }

        public static void Log(string text) =>
            AddToLog($"[{DateTime.Now.ToLongTimeString()}] {text}");

        public static void LogException(Exception ex) =>
            AddToLog($"[{DateTime.Now.ToLongTimeString()}] Exception was thrown:\n\n{ex}\n\n");

        private static void AddToLog(string value)
        {
            LogContent.AppendLine(value);
            if (Settings.Content.LogToFile)
                File.AppendAllText(Settings.LogFilePath + "\n", value);
        }
    }
}