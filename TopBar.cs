using Godot;
using System;

namespace OsuSkinMixer
{
    public class TopBar : Panel
    {
        public Main Main { get; set; }

        public override void _Ready()
        {
            GetNode<MenuButton>("HBoxContainer/SkinButton").GetPopup().Connect("id_pressed", this, "_SkinButtonPressed");
            GetNode<MenuButton>("HBoxContainer/OptionsButton").GetPopup().Connect("id_pressed", this, "_OptionsButtonPressed");
            GetNode<MenuButton>("HBoxContainer/HelpButton").GetPopup().Connect("id_pressed", this, "_HelpButtonPressed");
        }

        public void _SkinButtonPressed(int id)
        {
            switch (id)
            {
                case 0:
                    Main.CreateSkin();
                    break;

                case 1:
                    Main.RefreshSkins();
                    break;

                case 2:
                    Main.UseExistingSkin();
                    break;

                case 3:
                    Main.RandomizeSelections();
                    break;

                case 4:
                    Main.ResetSelections();
                    break;
            }
        }

        public void _OptionsButtonPressed(int id)
        {
            switch (id)
            {
                case 0:
                    Main.PromptForSkinsFolder();
                    break;
            }
        }

        public void _HelpButtonPressed(int id)
        {
            switch (id)
            {
                case 0:
                case 1:
                    OS.ShellOpen("https://github.com/rednir/OsuSkinMixer/issues/new/choose");
                    break;
            }
        }
    }
}
