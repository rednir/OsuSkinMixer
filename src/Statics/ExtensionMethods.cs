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
            file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir, newDestinationDir);
        }

        return new DirectoryInfo(destinationDir);
    }
}