using Godot;

namespace OsuSkinMixer.StackScenes;

public abstract partial class StackScene : Container
{
    public abstract string Title { get; }

    [Signal]
    public delegate void ScenePushedEventHandler(StackScene scene);

    [Signal]
    public delegate void ScenePoppedEventHandler();

    [Signal]
    public delegate void ToastPushedEventHandler(string text);
}