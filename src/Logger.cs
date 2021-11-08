using System;
using System.IO;
using Godot;
using File = System.IO.File;

namespace OsuSkinMixer
{
    public static class Logger
    {
        public static void Init()
        {
            try
            {
                if (Settings.Content.LogToFile)
                    File.WriteAllText(Settings.LogFilePath, $"------- osu! skin mixer {Settings.VERSION} -------");
            }
            catch (Exception ex)
            {
                OS.Alert($"Unable to write to log file: {ex.Message}\n\nLogging will be disabled.");
                Settings.Content.LogToFile = false;
                Settings.Save();
            }
        }

        public static void Log(string text)
        {
            if (Settings.Content.LogToFile)
                File.AppendAllText(Settings.LogFilePath, $"[{DateTime.Now.ToLongTimeString()}] {text}\n");
        }
    }
}