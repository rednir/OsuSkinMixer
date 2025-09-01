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

    public void FetchTextureOrDefault(string filepathNoExtension, string extension = "png", bool prefer2x = true, int maxSize = 2048)
    {
        string filepath = $"{filepathNoExtension}{(prefer2x ? "@2x" : string.Empty)}.{extension}";

        Task.Run(() => GetTextureAsync(filepath, maxSize).ContinueWith(async t =>
        {
            if (t.Result is null)
            {
                if (prefer2x)
                {
                    Texture2D fallbackResult = await GetTextureAsync($"{filepathNoExtension}.{extension}", maxSize);
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
            }

            CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, t.Result, true));
        }));
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

    private async Task<Texture2D> GetTextureAsync(string filepath, int maxSize)
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

        return await tcs.Task;
    }

    private static void CallOnMainThread(Action action)
        => Callable.From(action).CallDeferred();
}