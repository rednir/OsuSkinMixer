using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using OsuSkinMixer.Models.SkinOptions;

namespace OsuSkinMixer.Components;

public partial class SkinCreatorPopup : Control
{
	private AnimationPlayer AnimationPlayer;
	private CancellationTokenSource CancellationTokenSource;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Popup/AnimationPlayer");
	}

	public void CreateSkin(string skinName, IEnumerable<SkinOption> skinOptions)
	{
		SkinCreator skinCreator = new()
		{
			Name = skinName,
			SkinOptions = skinOptions.ToArray(),
		};

		AnimationPlayer.Play("in");

		CancellationTokenSource = new CancellationTokenSource();
		skinCreator.Create(CancellationTokenSource.Token);
	}
}
