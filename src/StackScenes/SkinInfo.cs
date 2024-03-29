namespace OsuSkinMixer.StackScenes;

using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

public partial class SkinInfo : StackScene
{
    public override string Title => "Skin info";

    public IEnumerable<OsuSkin> Skins { get; set; }

    private PackedScene SkinInfoPanelScene;

    public override void _Ready()
    {
        SkinInfoPanelScene = GD.Load<PackedScene>("res://src/Components/SkinInfoPanel.tscn");

        foreach (var skin in Skins)
        {
            var instance = SkinInfoPanelScene.Instantiate<SkinInfoPanel>();
            instance.Skin = skin;
            AddChild(instance);
        }
    }
}
