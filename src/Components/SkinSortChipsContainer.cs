using OsuSkinMixer.Models;

namespace OsuSkinMixer;

public partial class SkinSortChipsContainer : HBoxContainer
{
	public event Action<SkinSort> SortSelected;

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
			{
				_sortButtons[i].Disabled = true;
				SortSelected?.Invoke((SkinSort)index);
				continue;
			}

			_sortButtons[i].Disabled = false;
			_sortButtons[i].ButtonPressed = false;
		}
	}
}
