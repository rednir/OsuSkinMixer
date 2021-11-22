using System.Collections.Generic;
using System.Text;

namespace OsuSkinMixer
{
    public class SkinIniSection : Dictionary<string, string>
    {
        public SkinIniSection(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(Name).AppendLine("]");
            foreach (var pair in this)
                sb.Append(pair.Key).Append(": ").AppendLine(pair.Value);

            return sb.ToString();
        }
    }
}