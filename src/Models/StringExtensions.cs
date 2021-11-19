using System.Text;

namespace OsuSkinMixer
{
    public static class StringExtensions
    {
        /// <summary>Copies a string and places newline characters after words.</summary>
        /// <returns>The string with newlines placed.</returns>
        public static string Wrap(this string originalString, int maxCharsPerLine)
        {
            string[] splitString = originalString.Split(' ');
            var stringBuilder = new StringBuilder();

            int charsThisLine = 0;
            foreach (string word in splitString)
            {
                charsThisLine += word.Length + 1;
                if (charsThisLine > maxCharsPerLine)
                {
                    stringBuilder.Append('\n').Append(word).Append(' ');
                    charsThisLine = word.Length;
                }
                else
                {
                    stringBuilder.Append(word).Append(' ');
                }
            }

            // Remove unnecessary space after final word.
            stringBuilder.Remove(stringBuilder.Length - 1, 1);

            return stringBuilder.ToString();
        }
    }
}