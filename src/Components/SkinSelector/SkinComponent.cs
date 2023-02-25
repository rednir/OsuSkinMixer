using Godot;
using System.Linq;
using OsuSkinMixer.Models.Osu;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Components.SkinSelector;

public partial class SkinComponent : HBoxContainer
{
	public Button Button { get; private set; }

	public Skin Skin { get; private set; }

	private Label NameLabel;
	private Label AuthorLabel;
	private Hitcircle Hitcircle;

	public override void _Ready()
	{
		Button = GetNode<Button>("%Button");
		NameLabel = GetNode<Label>("%Name");
		AuthorLabel = GetNode<Label>("%Author");
		Hitcircle = GetNode<Hitcircle>("%Hitcircle");
	}

	public void SetValues(Skin skin)
	{
		Skin = skin;

		NameLabel.Text = skin.Name;
		AuthorLabel.Text = skin.SkinIni?.TryGetPropertyValue("General", "Author");
		Hitcircle.SetSkin(skin);
	}
}
