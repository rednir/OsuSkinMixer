using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.IO;

namespace OsuSkinMixer.Autoload;

public partial class TextureLoadingService : Node
{
    [Signal] public delegate void TextureReadyEventHandler(string filePath, Texture2D texture, bool is2x, bool isDefault);

    private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();

    private readonly ConcurrentDictionary<string, object> _skinLock = new();

    public void FetchTextureOrDefault(string filepathNoExtension, string extension = "png", bool prefer2x = true, int maxSize = 2048)
    {
        string filepath = $"{filepathNoExtension}{(prefer2x ? "@2x" : string.Empty)}.{extension}";

        Task.Run(() =>
        {
            Texture2D result = GetTexture(filepath, maxSize);
            if (result is not null)
            {
                CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, result, true, false));
                return;
            }

            if (prefer2x)
            {
                Texture2D fallbackResult = GetTexture($"{filepathNoExtension}.{extension}", maxSize);
                if (fallbackResult is not null)
                {
                    CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, fallbackResult, false, false));
                    return;
                }
            }

            // Fallback to loading the default skin texture from internal assets.
            string filename = Path.GetFileName(filepath);
            CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, GD.Load<Texture2D>($"res://assets/defaultskin/{filename}"), prefer2x, true));
            return;
        })
        .ContinueWith(t =>
        {
            if (t.IsFaulted)
                Settings.Log($"Error fetching texture: {t.Exception.Message}");
        });
    }

    /// <summary>Loads a skin element without materialising lazer blobs on the UI thread.</summary>
    public string FetchTextureOrDefault(OsuSkin skin, string filename, string extension = "png", bool prefer2x = true, int maxSize = 2048)
    {
        string requestKey = $"skin:{skin.Identity}:{filename}";
        Task.Run(() =>
        {
            string preferredName = $"{filename}{(prefer2x ? "@2x" : string.Empty)}.{extension}";
            Texture2D result = GetTexture(skin.FindFile(preferredName), $"{requestKey}:{preferredName}", extension, skin.Identity, maxSize);
            if (result is not null)
            {
                CallOnMainThread(() => EmitSignal(SignalName.TextureReady, requestKey, result, true, false));
                return;
            }

            if (prefer2x)
            {
                string fallbackName = $"{filename}.{extension}";
                Texture2D fallbackResult = GetTexture(skin.FindFile(fallbackName), $"{requestKey}:{fallbackName}", extension, skin.Identity, maxSize);
                if (fallbackResult is not null)
                {
                    CallOnMainThread(() => EmitSignal(SignalName.TextureReady, requestKey, fallbackResult, false, false));
                    return;
                }
            }

            CallOnMainThread(() => EmitSignal(SignalName.TextureReady, requestKey,
                GD.Load<Texture2D>($"res://assets/defaultskin/{preferredName}"), prefer2x, true));
        })
        .ContinueWith(t =>
        {
            if (t.IsFaulted)
                Settings.Log($"Error fetching texture: {t.Exception.Message}");
        });
        return requestKey;
    }


    public void InvalidateSkinCache(OsuSkin skin)
    {
        string normalizedSkinPath = Path.GetDirectoryName(skin.GetElementFilepathWithoutExtension("cursor"));
        string skinCachePrefix = $"skin:{skin.Identity}:";

        foreach (string key in _textureCache.Keys)
        {
            if (key.StartsWith(skinCachePrefix, StringComparison.Ordinal))
            {
                _textureCache.TryRemove(key, out _);
                continue;
            }
            try
            {
                string normalizedKeyPath = Path.GetFullPath(key);
                if (normalizedKeyPath.StartsWith(normalizedSkinPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    _textureCache.TryRemove(key, out _);
            }
            catch
            {
            }
        }

        // In-flight loads retain their lock; removing it could create a second lock for the same files.
    }

    private Texture2D GetTexture(string filepath, int maxSize)
        => GetTexture(filepath, filepath, Path.GetExtension(filepath).TrimStart('.'), Path.GetDirectoryName(Path.GetFullPath(filepath)), maxSize);

    private Texture2D GetTexture(string filepath, string cacheKey, string extension, string skinKey, int maxSize)
    {
        if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
            return null;

        _skinLock.TryAdd(skinKey, new object());

        // Ensure there's no more than one texture loading for each skin at a time.
        lock (_skinLock[skinKey])
        {
            if (_textureCache.TryGetValue(cacheKey, out Texture2D cachedTexture))
                return cachedTexture;

            Image image = new();
            Error err;
            if (Path.GetExtension(filepath).Equals($".{extension}", StringComparison.OrdinalIgnoreCase))
                err = image.Load(filepath);
            else
            {
                var bytes = File.ReadAllBytes(filepath);
                err = extension.Equals("jpg", StringComparison.OrdinalIgnoreCase)
                    ? image.LoadJpgFromBuffer(bytes)
                    : image.LoadPngFromBuffer(bytes);
            }

            if (err != Error.Ok || image.IsEmpty())
            {
                image.Dispose();
                return null;
            }

            // menu-background.png for example can be quite expensive to load and cause lag spikes, so downscale.
            var width = image.GetWidth();
            var height = image.GetHeight();
            if (width > maxSize || height > maxSize)
            {
                float scale = (float)maxSize / Mathf.Max(width, height);
                image.Resize(Mathf.CeilToInt(width * scale), Mathf.CeilToInt(height * scale), Image.Interpolation.Lanczos);
            }

            TaskCompletionSource<Texture2D> tcs = new();
            // GPU work has to be done on the main thread.
            CallOnMainThread(() =>
            {
                var texture = ImageTexture.CreateFromImage(image);
                _textureCache[cacheKey] = texture;
                tcs.SetResult(texture);

                image.Dispose();
            });

            return tcs.Task.Result;
        }
    }

    private static string GetSkinNameFromElementPath(string elementPath)
    {
        string fullElementPath = Path.GetFullPath(elementPath);

        foreach (string skinFolder in new[] { Settings.SkinsFolderPath, Settings.HiddenSkinsFolderPath })
        {
            string skinsFolderPath = Path.GetFullPath(skinFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string relative = Path.GetRelativePath(skinsFolderPath, fullElementPath);

            if (relative.StartsWith(".."))
                continue;

            relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int index = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
            return index >= 0 ? relative[..index] : relative;
        }

        return string.Empty;
    }

    private static void CallOnMainThread(Action action)
        => Callable.From(action).CallDeferred();
}
