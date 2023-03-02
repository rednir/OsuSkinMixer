using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OsuSkinMixer.Models;

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

		string[] iniColorRgb = skin
			.SkinIni?
			.TryGetPropertyValue("Colours", "Combo1")?
			.Replace(" ", string.Empty)
			.Split(',');

        if (iniColorRgb != null
			&& float.TryParse(iniColorRgb[0], out float r)
            && float.TryParse(iniColorRgb[1], out float g)
            && float.TryParse(iniColorRgb[2], out float b))
        {
            HitcircleTexture.Modulate = new Color(r / 255, g / 255, b / 255);
        }
        else
        {
            HitcircleTexture.Modulate = new Color(0, 202, 0);
        }
    }
}
