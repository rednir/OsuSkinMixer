using System;
using System.IO;

namespace OsuSkinMixer.Statics;

public static class ExtensionMethods
{
    public static DirectoryInfo CopyDirectory(this DirectoryInfo sourceDir, string destinationDir, bool overwrite = false)
    {
        if (overwrite && Directory.Exists(destinationDir))
            Directory.Delete(destinationDir, true);

        DirectoryInfo[] dirs = sourceDir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in sourceDir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir, newDestinationDir, overwrite);
        }

        return new DirectoryInfo(destinationDir);
    }

    public static string Humanise(this TimeSpan timespan)
    {
        return timespan.TotalSeconds switch
        {
            < 1 => "Just now",
            < 60 => $"{timespan.Seconds} seconds ago",
            < 3600 => $"{timespan.Minutes} minutes ago",
            < 86400 => $"{timespan.Hours} hours ago",
            _ => $"{timespan.Days} days ago"
        };
    }
}