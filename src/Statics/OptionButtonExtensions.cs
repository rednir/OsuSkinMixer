using System;
using System.Collections.Generic;
using Godot;

namespace OsuSkinMixer
{
    public static class OptionButtonExtensions
    {
        public static void SelectAndEmit(this OptionButton optionButton, int index)
        {
            optionButton.Select(index);
            optionButton.EmitSignal("item_selected", index);
        }
    }
}