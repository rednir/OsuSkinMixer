using Godot;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.StackScenes;

public partial class SkinInfo : StackScene
{
	public override string Title => "Skin info";

	private OsuSkin Skin { get; set; }

	private Label SkinNameLabel;

	public override void _Ready()
	{
		SkinNameLabel = GetNode<Label>("%SkinNameLabel");
		SkinNameLabel.Text = Skin.Name;
	}

	public void SetSkin(OsuSkin skin)
	{
		Skin = skin;
	}
}
