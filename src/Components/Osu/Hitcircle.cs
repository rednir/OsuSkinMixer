using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.Components;

public partial class Hitcircle : Node2D
{
	private Sprite2D ApproachcircleSprite;
	private Sprite2D HitcircleSprite;
	private Sprite2D HitcircleoverlaySprite;
	private Sprite2D DefaultSprite;

	public override void _Ready()
	{
		ApproachcircleSprite = GetNode<Sprite2D>("%ApproachcircleSprite");
		HitcircleSprite = GetNode<Sprite2D>("%HitcircleSprite");
		HitcircleoverlaySprite = GetNode<Sprite2D>("%HitcircleoverlaySprite");
		DefaultSprite = GetNode<Sprite2D>("%DefaultSprite");
	}

	public void SetSkin(OsuSkin skin, int combo)
	{
		ApproachcircleSprite.Texture = skin.GetTexture("approachcircle.png");
		HitcircleSprite.Texture = skin.GetTexture("hitcircle.png");
		HitcircleoverlaySprite.Texture = skin.GetTexture("hitcircleoverlay.png");
		DefaultSprite.Texture = skin.GetTexture("default-1.png");

		string[] iniColorRgb = skin
			.SkinIni?
			.TryGetPropertyValue("Colours", $"Combo{combo}")?
			.Replace(" ", string.Empty)
			.Split(',');

		if (iniColorRgb != null
			&& float.TryParse(iniColorRgb[0], out float r)
			&& float.TryParse(iniColorRgb[1], out float g)
			&& float.TryParse(iniColorRgb[2], out float b))
		{
			HitcircleSprite.Modulate = new Color(r / 255, g / 255, b / 255);
		}
		else
		{
			HitcircleSprite.Modulate = new Color(0, 202, 0);
		}
	}
}
