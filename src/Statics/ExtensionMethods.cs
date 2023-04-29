namespace OsuSkinMixer.Statics;

using System.IO;

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
            < 60 => $"{timespan.Seconds} {(timespan.Seconds == 1 ? "second" : "seconds")} ago",
            < 60 * 60 => $"{timespan.Minutes} {(timespan.Minutes == 1 ? "minute" : "minutes")} ago",
            < 60 * 60 * 24 => $"{timespan.Hours} {(timespan.Hours == 1 ? "hour" : "hours")} ago",
            < 60 * 60 * 24 * 7 => $"{timespan.Days} {(timespan.Days == 1 ? "day" : "days")} ago",
            < 60 * 60 * 24 * 30 => $"{timespan.Days / 7} {(timespan.Days / 7 == 1 ? "week" : "weeks")} ago",
            < 60 * 60 * 24 * 365 => $"{timespan.Days / 30} {(timespan.Days / 30 == 1 ? "month" : "months")} ago",
            _ => $"{timespan.Days / 365} {(timespan.Days / 365 == 1 ? "year" : "years")} ago",
        };
    }
}