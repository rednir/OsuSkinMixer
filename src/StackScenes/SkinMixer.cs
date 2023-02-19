using OsuSkinMixer.Models.SkinOptions;
using OsuSkinMixer.Components.SkinOptionsSelector;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
    public override string Title => "Skin Mixer";

    private SkinOption[] SkinOptions { get; } = SkinOption.Default;

    private SkinOptionsSelector SkinOptionsSelector;

    public override void _Ready()
    {
        SkinOptionsSelector = GetNode<SkinOptionsSelector>("SkinOptionsSelector");

        SkinOptionsSelector.CreateOptionComponents(SkinOptions);
    }
}
