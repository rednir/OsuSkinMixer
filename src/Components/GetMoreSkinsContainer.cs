namespace OsuSkinMixer.Components;

public partial class GetMoreSkinsContainer : PanelContainer
{
    public override void _Ready()
    {
        GetNode<Button>("%GetMoreSkinsButton").Pressed += GetNode<GetMoreSkinsPopup>("%GetMoreSkinsPopup").In;
    }
}
