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
		HitcircleTexture.Texture = skin.GetTexture("hitcircle.png");
		HitcircleoverlayTexture.Texture = skin.GetTexture("hitcircleoverlay.png");
		Default1Texture.Texture = skin.GetTexture("default-1.png");
	}
}
