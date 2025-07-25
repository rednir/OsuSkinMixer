namespace OsuSkinMixer.Components;

using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class SkinOptionsSelector : PanelContainer
{
    public SkinOption[] SkinOptions { get; } = SkinOption.Default;

    private PackedScene SkinOptionComponentScene;

    private ExpandablePanelContainer ExpandablePanelContainer;
    private VBoxContainer OptionsContainer;
    private AnimationPlayer AnimationPlayer;
    private Panel ExpandHint;
    private SkinSelectorPopup SkinSelectorPopup;

    private SkinOptionComponent SkinOptionComponentInSelection;

    private List<SkinOptionComponent> SkinOptionComponents;

    private readonly Random Random = new();

    public override void _Ready()
    {
        SkinOptionComponentScene = GD.Load<PackedScene>("res://src/Components/SkinOptionsSelector/SkinOptionComponent.tscn");

        ExpandablePanelContainer = GetNode<ExpandablePanelContainer>("%ExpandablePanelContainer");
        OptionsContainer = GetNode<VBoxContainer>("%OptionsContainer");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        ExpandHint = GetNode<Panel>("%ExpandHint");
        SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");

        SkinSelectorPopup.OnSelected = s =>
        {
            Settings.Log($"Skin option '{SkinOptionComponentInSelection.SkinOption.Name}' set to: {s}");
            OptionComponentSelected(new SkinOptionValue(s));
        };

        OsuData.SkinRemoved += OnSkinRemoved;

        if (!Settings.Content.ArrowButtonPressed)
            AnimationPlayer.Play("hint");
    }

    public override void _ExitTree()
    {
        OsuData.SkinRemoved -= OnSkinRemoved;
    }

    public void CreateOptionComponents(SkinOptionValue defaultValue)
    {
        foreach (Node child in GetChildren())
            (child as SkinOptionComponent)?.QueueFree();

        SkinOptionComponents = new List<SkinOptionComponent>();

        foreach (var option in SkinOptions)
            addOptionComponent(option, OptionsContainer, 0);

        void addOptionComponent(SkinOption option, VBoxContainer vbox, int layer)
        {
            SkinOptionComponent component = SkinOptionComponentScene.Instantiate<SkinOptionComponent>();
            vbox.AddChild(component);

            component.ResetButton.Pressed += () =>
            {
                Settings.Log($"Skin option '{option.Name}' reset to default: {component.DefaultValue}");
                SkinOptionComponentInSelection = component;
                OptionComponentSelected(defaultValue);
            };
            component.Button.Pressed += () =>
            {
                SkinOptionComponentInSelection = component;
                SkinSelectorPopup.In();
            };

            component.SetSkinOption(option, defaultValue, layer);

            if (option is ParentSkinOption parentOption)
            {
                var newVbox = new VBoxContainer()
                {
                    CustomMinimumSize = new Vector2(10, 0),
                    Visible = false,
                };
                newVbox.AddThemeConstantOverride("separation", 8);

                vbox.AddChild(newVbox);
                vbox.MoveChild(newVbox, component.GetIndex() + 1);

                component.ArrowButton.Toggled += p =>
                {
                    newVbox.Visible = p;

                    if (!Settings.Content.ArrowButtonPressed)
                    {
                        ExpandHint.Visible = false;
                        Settings.Content.ArrowButtonPressed = true;
                        Settings.Save();
                    }
                };

                foreach (var child in parentOption.Children)
                    addOptionComponent(child, newVbox, layer + 1);
            }

            SkinOptionComponents.Add(component);
        }
    }

    public void Randomize()
    {
        Settings.Log("Randomising all skin options");

        if (OsuData.Skins.Length == 0)
            return;

        foreach (var component in SkinOptionComponents.Where(c => c.SkinOption is not ParentSkinOption))
        {
            SkinOptionComponentInSelection = component;
            OptionComponentSelected(new SkinOptionValue(OsuData.Skins[Random.Next(OsuData.Skins.Length)]));
        }
    }

    public void Reset()
    {
        Settings.Log("Reseting all skin options");

        foreach (var component in SkinOptionComponents.Where(c => c.SkinOption is not ParentSkinOption))
        {
            SkinOptionComponentInSelection = component;
            OptionComponentSelected(component.DefaultValue);
        }
    }

    public void OptionComponentSelected(SkinOptionValue valueSelected)
    {
        // TODO: This method can be optimized further by recursively looping through the components and their
        // children (in their respective VBoxContainers) instead of looping through the ParentSkinOption's children.
        SkinOptionComponentInSelection.SetValue(valueSelected);

        foreach (var parent in SkinOption.GetParents(SkinOptionComponentInSelection.SkinOption, SkinOptions))
        {
            SkinOptionComponent parentOptionComponent = SkinOptionComponents.Find(c => c.SkinOption == parent);
            if (parent.Children.All(o => o.Value == valueSelected))
                parentOptionComponent.SetValue(valueSelected);
            else
                parentOptionComponent.SetValue(new SkinOptionValue(SkinOptionValueType.Various));
        }

        SetValueOfAllChildrenOfOption(SkinOptionComponentInSelection.SkinOption, valueSelected);
        SkinSelectorPopup.Out();
        SkinOptionComponentInSelection.Button.CallDeferred(Button.MethodName.GrabFocus);

        if (valueSelected != SkinOptionComponentInSelection.DefaultValue)
        {
            ExpandablePanelContainer.Activate();
        }
        else if (!SkinOptionComponents.Any(c => c.SkinOption.Value != c.DefaultValue))
        {
            ExpandablePanelContainer.Deactivate();
        }
    }

    private void SetValueOfAllChildrenOfOption(SkinOption option, SkinOptionValue value)
    {
        if (option is ParentSkinOption parentOption)
        {
            foreach (var child in parentOption.Children)
                SetValueOfAllChildrenOfOption(child, value);
        }

        SkinOptionComponents.Find(c => c.SkinOption == option).SetValue(value);
    }

    private void OnSkinRemoved(OsuSkin skin)
    {
        var invalidComponents = SkinOptionComponents.Where(c => c.SkinOption.Value.CustomSkin?.Equals(skin) == true);

        if (!invalidComponents.Any())
            return;

        foreach (var component in invalidComponents)
        {
            SkinOptionComponentInSelection = component;
            OptionComponentSelected(component.DefaultValue);
        }
    }
}
