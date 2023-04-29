namespace OsuSkinMixer.Components;

public partial class GetMoreSkinsPopup : Popup
{
    public override void _Ready()
    {
        base._Ready();

        GetNode<Button>("%CloseButton").Pressed += Out;
        GetNode<Button>("%OsuForumsButton").Pressed += () => OS.ShellOpen("https://osu.ppy.sh/community/forums/109");
        GetNode<Button>("%RedditButton").Pressed += () => OS.ShellOpen("https://reddit.com/r/OsuSkins/");
        GetNode<Button>("%OsuckButton").Pressed += () => OS.ShellOpen("https://skins.osuck.net/index.php");
        GetNode<Button>("%CirclePeopleButton").Pressed += () => OS.ShellOpen("https://circle-people.com/skins/");
        GetNode<Button>("%CompendiumButton").Pressed += () => OS.ShellOpen("https://compendium.skinship.xyz/");
        GetNode<Button>("%OsuskinsNetButton").Pressed += () => OS.ShellOpen("https://osuskins.net/");
    }
}
