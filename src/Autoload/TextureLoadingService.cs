using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;

namespace OsuSkinMixer.Autoload;

public partial class TextureLoadingService : Node
{
    [Signal] public delegate void TextureReadyEventHandler(string filePath, Texture2D texture);

    private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();

    public void GetTextureInBackground(string filepath, int maxSize = 2048)
    {
        if (_textureCache.TryGetValue(filepath, out Texture2D cachedTexture))
        {
            EmitSignal(SignalName.TextureReady, filepath, cachedTexture);
            return;
        }

        _textureCache.TryAdd(filepath, null);

        Task.Run(() =>
        {
            Image image = new();
            Error err = image.Load(filepath);

            if (err != Error.Ok)
                return null;

            // menu-background.png for example can be quite expensive to load and cause lag spikes, so downscale.
            var width = image.GetWidth();
            var height = image.GetHeight();
            if (width > maxSize || height > maxSize)
            {
                float scale = (float)maxSize / Mathf.Max(width, height);
                image.Resize(Mathf.CeilToInt(width * scale), Mathf.CeilToInt(height * scale), Image.Interpolation.Lanczos);
            }

            return image;
        })
        .ContinueWith(t =>
        {
            Image image = t.Result;

            if (image is null)
                return;

            Callable.From(() =>
            {
                var texture = ImageTexture.CreateFromImage(image);
                _textureCache.TryUpdate(filepath, texture, null);
                EmitSignal(SignalName.TextureReady, filepath, texture);

                image.Dispose();
            })
            .CallDeferred();
        });
    }
}