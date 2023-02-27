using Godot;
using System;
using OsuSkinMixer.Models.Osu;

namespace OsuSkinMixer.Components;

public partial class Hitcircle : CenterContainer
{
	private TextureRect HitcircleTexture;
	private TextureRect HitcircleoverlayTexture;
	private TextureRect Default1Texture;

	public override void _Ready()
	{
		HitcircleTexture = GetNode<TextureRect>("HitcircleTexture");
		HitcircleoverlayTexture = GetNode<TextureRect>("HitcircleoverlayTexture");
		Default1Texture = GetNode<TextureRect>("Default1Texture");
	}

	public void SetSkin(OsuSkin skin)
	{
		HitcircleTexture.Texture = skin.HitcircleTexture;
		HitcircleoverlayTexture.Texture = skin.HitcircleoverlayTexture;
		Default1Texture.Texture = skin.Default1Texture;
	}
}
