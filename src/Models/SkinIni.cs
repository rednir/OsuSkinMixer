using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OsuSkinMixer
{
    public class SkinIni
    {
        public SkinIni()
        {
        }

        public SkinIni(string fileContent)
        {
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
                if (lines[i].StartsWith("[") && lines[i].EndsWith("]"))
                {
                    Sections.Add(new SkinIniSection(lines[i].Substring(1, lines[i].Length - 2)));
                    continue;
                }
                else if (Sections.Count == 0)
                {
                    throw new ArgumentException($"Line {i + 1} on skin.ini: Expected a section name.");
                }

                string[] keyAndValue = lines[i].Split(':');
                if (keyAndValue.Length != 2)
                    throw new ArgumentException($"Line {i + 1} on skin.ini: Invalid number of ':' characters.");

                Sections.Last().Add(keyAndValue[0].Trim(), keyAndValue[1].Trim());
            }
        }

        public List<SkinIniSection> Sections { get; } = new List<SkinIniSection>();

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