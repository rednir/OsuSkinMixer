using System.Linq;
using Godot;
using OsuSkinMixer.Models;

namespace OsuSkinMixer;

public partial class SkinSortChipsContainer : HBoxContainer
{
	private Button[] _sortButtons;

	public override void _Ready()
	{
		_sortButtons = GetChildren().Cast<Button>().ToArray();

        for (int i = 0; i < _sortButtons.Length; i++)
		{
			int index = i;
            _sortButtons[i].Pressed += () => OnSortButtonPressed(index);
		}
    }

	private void OnSortButtonPressed(int index)
	{
		for (int i = 0; i < _sortButtons.Length; i++)
		{
			if (i == index)
				continue;

			_sortButtons[i].ButtonPressed = false;
		}
	}
}
