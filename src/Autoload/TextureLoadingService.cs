using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.IO;

namespace OsuSkinMixer.Autoload;

public partial class TextureLoadingService : Node
{
    [Signal] public delegate void TextureReadyEventHandler(string filePath, Texture2D texture);

    private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();

    public void FetchTextureOrDefault(string filepathNoExtension, string extension, bool prefer2x = true, int maxSize = 2048)
    {
        string filepath = $"{filepathNoExtension}{(prefer2x ? "@2x" : string.Empty)}.{extension}";
        GD.Print($"Fetching texture: {filepath}");

        Task.Run(() => GetTextureAsync(filepath, maxSize).ContinueWith(async t =>
        {
            if (prefer2x && t.Result is null)
            {
                Texture2D fallbackResult = await GetTextureAsync($"{filepathNoExtension}.{extension}", maxSize);
                CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, fallbackResult));
                return;
            }

            CallOnMainThread(() => EmitSignal(SignalName.TextureReady, filepathNoExtension, t.Result));
        }));
    }

    private async Task<Texture2D> GetTextureAsync(string filepath, int maxSize)
    {
        if (_textureCache.TryGetValue(filepath, out Texture2D cachedTexture))
        {
            EmitSignal(SignalName.TextureReady, filepath, cachedTexture);
            return cachedTexture;
        }

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