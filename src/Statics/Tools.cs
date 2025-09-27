namespace OsuSkinMixer.Statics;

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OsuSkinMixer.Models;
using OsuSkinMixer.Models.Osu;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class Tools
{
    public static void ShellOpenFile(string path)
        => OS.ShellOpen(OS.GetName() == "macOS" ? $"file://{path}" : path);

    public static void TriggerOskImport(OsuSkinBase skin)
    {
        string osuPath = Path.Combine(Settings.Content.OsuFolder, "osu!.exe");

        if (!File.Exists(osuPath))
            throw new FileNotFoundException($"osu! executable not found at {osuPath}");

        string oskDestPath = $"{Settings.TempFolderPath}/{skin.Name}.osk";

        // osu! will handle the empty .osk (zip) file by switching the current skin to the skin with name `skin.Name`.
        File.WriteAllBytes(oskDestPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Process.Start(new ProcessStartInfo
        {
            FileName = osuPath,
            Arguments = oskDestPath,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }

    public static Rectangle GetContentRectFromImage(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;

        int minX = width;
        int minY = height;
        int maxX = 0;
        int maxY = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Rgba32 pixel = image[x, y];

                if (pixel.A != 0)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (minX == width || minY == height || maxX == 0 || maxY == 0)
            return Rectangle.Empty;

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static Rectangle GetContentRectFromImage(string path)
    {
        try
        {
            using Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            return GetContentRectFromImage(image);
        }
        catch
        {
            return Rectangle.Empty;
        }
    }

    public static AudioStreamWav GetAudioStreamWav(byte[] bytes)
    {
        AudioStreamWav stream = new();

        int bitsPerSample = 0;

        for (int i = 0; i < 100; i++)
        {
            // Next 4 bytes are either RIFF, WAVE, or fmt.
            string next = Encoding.ASCII.GetString(bytes, i, 4);

            if (next == "fmt ")
            {
                // Get format subchunk index.
                int fsc0 = i + 8;

                int formatCode = bytes[fsc0] + (bytes[fsc0 + 1] << 8);
                stream.Format = (AudioStreamWav.FormatEnum)formatCode;

                int channelNum = bytes[fsc0 + 2] + (bytes[fsc0 + 3] << 8);
                stream.Stereo = channelNum == 2;

                int sampleRate = bytes[fsc0 + 4] + (bytes[fsc0 + 5] << 8) + (bytes[fsc0 + 6] << 16) + (bytes[fsc0 + 7] << 24);
                stream.MixRate = sampleRate;

                bitsPerSample = bytes[fsc0 + 14] + (bytes[fsc0 + 15] << 8);
            }
            else if (next == "data")
            {
                if (bitsPerSample == 0)
                    throw new InvalidDataException("Format chunk missing.");

                int audioDataSize = bytes[i + 4] + (bytes[i + 5] << 8) + (bytes[i + 6] << 16) + (bytes[i + 7] << 24);
                int dataStartIndex = i + 8;
                
                stream.Data = bytes.Skip(dataStartIndex).Take(audioDataSize).ToArray();

                if (bitsPerSample == 24)
                {
                    stream.Data = ConvertFrom24To16Bit(stream.Data);
                }
                else if (bitsPerSample == 32)
                {
                    stream.Data = ConvertFrom32To16Bit(stream.Data);
                    stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
                }
            }
        }

        return stream;
    }

    private static byte[] ConvertFrom24To16Bit(byte[] bytes)
    {
        byte[] result = new byte[bytes.Length / 3 * 2];

        int j = 0;
        for (int i = 0; i < bytes.Length; i += 3)
        {
            result[j] = bytes[i + 1];
            result[j + 1] = bytes[i + 2];
            j += 2;
        }

        return result;
    }

    private static byte[] ConvertFrom32To16Bit(byte[] bytes)
    {
        byte[] result = new byte[bytes.Length / 2];

        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);

        for (int i = 0; i < bytes.Length; i += 4)
        {
            // Convert from float to 16-bit signed integer, and clamp to avoid distortion.
            int value = (int)(32768 * reader.ReadSingle());
            int clamped = Math.Clamp(value, short.MinValue, short.MaxValue);

            result[i / 2] = (byte)clamped;
            result[i / 2 + 1] = (byte)(clamped >> 8);
        }

        return result;
    }

    public static string ComputeSHA256OfFile(string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(fileStream);
        StringBuilder sb = new("sha256:");
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}