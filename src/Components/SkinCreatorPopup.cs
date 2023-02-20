using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using OsuSkinMixer.Models.SkinOptions;
using Skin = OsuSkinMixer.Models.Osu.Skin;

namespace OsuSkinMixer.Components;

public partial class SkinCreatorPopup : Control
{
	private AnimationPlayer AnimationPlayer;
	private CancellationTokenSource CancellationTokenSource;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
	}

	public Skin CreateSkin(string skinName, IEnumerable<SkinOption> skinOptions)
	{
		SkinCreator skinCreator = new()
		{
			Name = skinName,
			SkinOptions = skinOptions.ToArray(),
		};

		AnimationPlayer.Play("in");

		CancellationTokenSource = new CancellationTokenSource();
		Skin skin = skinCreator.Create(CancellationTokenSource.Token);

		AnimationPlayer.Play("out");

		return skin;
	}
}
