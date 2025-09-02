using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.IO;

namespace OsuSkinMixer.Autoload;

public partial class TextureLoadingService : Node
{
    [Signal] public delegate void TextureReadyEventHandler(string filePath, Texture2D texture, bool is2x);

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
                CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, result, true));
                return;
            }

            if (prefer2x)
            {
                Texture2D fallbackResult = GetTexture($"{filepathNoExtension}.{extension}", maxSize);
                if (fallbackResult is not null)
                {
                    CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, fallbackResult, false));
                    return;
                }
            }

            // Fallback to loading the default skin texture from internal assets.
            string filename = Path.GetFileName(filepath);
            CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, GD.Load<Texture2D>($"res://assets/defaultskin/{filename}"), prefer2x));
            return;
        });
    }


    public void InvalidateSkinCache(OsuSkin skin)
    {
        string normalizedSkinPath = skin.Directory.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (string key in _textureCache.Keys)
        {
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
    }

    private Texture2D GetTexture(string filepath, int maxSize)
    {
        string skinName = GetSkinNameFromElementPath(filepath);
        _skinLock.TryAdd(skinName, new object());

        lock (_skinLock[skinName])
        {
            if (_textureCache.TryGetValue(filepath, out Texture2D cachedTexture))
                return cachedTexture;

            _textureCache.TryAdd(filepath, null);

            if (!File.Exists(filepath))
                return null;

            Image image = new();
            Error err = image.Load(filepath);

            if (err != Error.Ok || image.IsEmpty())
                return null;

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
                _textureCache.TryUpdate(filepath, texture, null);
                tcs.SetResult(texture);

                image.Dispose();
            });

            return tcs.Task.Result;
        }
    }

    private static string GetSkinNameFromElementPath(string elementPath)
    {
        string skinsFolderPath = Path.GetFullPath(Settings.SkinsFolderPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string relative = Path.GetRelativePath(skinsFolderPath, Path.GetFullPath(elementPath));

        if (relative.StartsWith(".."))
            return null;

        relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        int index = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        return index >= 0 ? relative[..index] : relative;
    }

    private static void CallOnMainThread(Action action)
        => Callable.From(action).CallDeferred();
}