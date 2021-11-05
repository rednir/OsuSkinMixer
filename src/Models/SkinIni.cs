using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OsuSkinMixer
{
    public class SkinIni
    {
        public SkinIni(string name, string author, string version = "latest")
        {
            Sections = new List<SkinIniSection>()
            {
                new SkinIniSection("General")
                {
                    { "Name", name },
                    { "Author", author },
                    { "Version", version },
                },
                new SkinIniSection("Colours"),
                new SkinIniSection("Fonts"),
                new SkinIniSection("CatchTheBeat"),
                new SkinIniSection("Mania"),
            };
        }

        public SkinIni(string fileContent)
        {
            Sections = new List<SkinIniSection>();
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
                    lines[i] = lines[i].Substring(0, commentIndex);

                // Check if the line is declaring the next section.
                if (lines[i].Contains("[") && lines[i].Contains("]"))
                {
                    int start = lines[i].IndexOf("[") + 1;
                    int length = lines[i].IndexOf("]") - start;
                    Sections.Add(new SkinIniSection(lines[i].Substring(start, length)));
                    continue;
                }

                string[] keyAndValue = lines[i].Split(new char[] { ':' }, 2);

                // Ignore lines without a key/value.
                if (keyAndValue.Length < 2)
                    continue;

                keyAndValue[0] = keyAndValue[0].Trim();
                keyAndValue[1] = keyAndValue[1].Trim();

                // Can't add a key/value when a section name is not yet declared.
                if (Sections.Count == 0)
                    throw new ArgumentException($"Line {i + 1} on skin.ini '{lines[i]}': Expected a section name.");

                var section = Sections.Last();

                // Replace already existing keys.
                section.Remove(keyAndValue[0]);

                section.Add(keyAndValue[0], keyAndValue[1]);
            }
        }

        public List<SkinIniSection> Sections { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var section in Sections)
            {
                sb.Append('[').Append(section.Name).AppendLine("]");
                foreach (var pair in section)
                    sb.Append(pair.Key).Append(": ").AppendLine(pair.Value);

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}