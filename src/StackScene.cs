using Godot;

namespace OsuSkinMixer;

public abstract partial class StackScene : Control
{
    public abstract string Title { get; }

    [Signal]
    public delegate void ScenePushedEventHandler(StackScene scene);
}