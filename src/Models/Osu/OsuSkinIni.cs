using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OsuSkinMixer.Models;

public class OsuSkinIni
{
    public static bool PropertyHasFilePath(string propertyName) =>
        (propertyName.StartsWith("Stage") && propertyName != "StageSeparation")
        || propertyName.EndsWith("Prefix")
        || propertyName.StartsWith("KeyImage")
        || propertyName.StartsWith("NoteImage")
        || propertyName == "LightingN"
        || propertyName == "LightingL"
        || propertyName == "WarningArrow"
        || propertyName == "WarningArrow"
        || propertyName == "Hit0"
        || propertyName == "Hit50"
        || propertyName == "Hit100"
        || propertyName == "Hit200"
        || propertyName == "Hit300"
        || propertyName == "Hit300g";

    public OsuSkinIni(string name, string author, string version = "latest")
    {
        Sections = new List<OsuSkinIniSection>()
            {
                new OsuSkinIniSection("General")
                {
                    { "Name", name },
                    { "Author", author },
                    { "Version", version },
                },
                new OsuSkinIniSection("Colours"),
                new OsuSkinIniSection("Fonts"),
                new OsuSkinIniSection("CatchTheBeat"),
            };
    }

    public OsuSkinIni(string fileContent)
    {
        Sections = new List<OsuSkinIniSection>();
        string[] lines = fileContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();

            int commentIndex = lines[i].IndexOf("//");

            // Ignore blank lines and completely commented lines.
            if (commentIndex == 0 || string.IsNullOrWhiteSpace(lines[i]))
                continue;

            // Ignore comments further on in the line.
            if (commentIndex != -1)
                lines[i] = lines[i][..commentIndex];

            // Check if the line is declaring the next section.
            if (lines[i].Contains('[') && lines[i].Contains(']'))
            {
                int start = lines[i].IndexOf("[") + 1;
                int length = lines[i].IndexOf("]") - start;
                Sections.Add(new OsuSkinIniSection(lines[i].Substring(start, length)));
                continue;
            }

            // Can't add a key/value when a section name is not yet declared.
            if (Sections.Count == 0)
                continue;

            string[] keyAndValue = lines[i].Split(new char[] { ':' }, 2);

            // Ignore lines without a key/value.
            if (keyAndValue.Length < 2)
                continue;

            keyAndValue[0] = keyAndValue[0].Trim();
            keyAndValue[1] = keyAndValue[1].Trim().Replace("\\", "/");

            var section = Sections.Last();

            // Replace already existing keys.
            section.Remove(keyAndValue[0]);

            section.Add(keyAndValue[0], keyAndValue[1]);
        }
    }

    public string TryGetPropertyValue(string sectionName, string propertyName)
    {
        OsuSkinIniSection section = Sections.Find(s => s.Name == sectionName);

        if (section == null)
            return null;

        section.TryGetValue(propertyName, out string value);
        return value;
    }

    public List<OsuSkinIniSection> Sections { get; }

    public override string ToString() => string.Join("\n", Sections);
}