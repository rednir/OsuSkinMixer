namespace OsuSkinMixer.Components;

using System.Runtime.InteropServices.Marshalling;
using System.Text;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;

public partial class SkinOptionsSelector : PanelContainer
{
    public SkinOption[] SkinOptions { get; } = SkinOption.Default;

    private PackedScene SkinOptionComponentScene;

    private ExpandablePanelContainer ExpandablePanelContainer;
    private VBoxContainer OptionsContainer;
    private AnimationPlayer AnimationPlayer;
    private AudioStreamPlayer AudioStreamPlayer;
    private Panel ExpandHint;
    private PanelContainer LazerMenuWarningContainer;
    private SkinSelectorPopup SkinSelectorPopup;

    private SkinOptionComponent SkinOptionComponentInSelection;

    private List<SkinOptionComponent> SkinOptionComponents;

    private readonly Random Random = new();

    private Action previewFinishedAction;

    private readonly Queue<AudioStream> PreviewAudioStreamQueue = new();

    public override void _Ready()
    {
        SkinOptionComponentScene = GD.Load<PackedScene>("res://src/Components/SkinOptionsSelector/SkinOptionComponent.tscn");

        ExpandablePanelContainer = GetNode<ExpandablePanelContainer>("%ExpandablePanelContainer");
        OptionsContainer = GetNode<VBoxContainer>("%OptionsContainer");
        AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        AudioStreamPlayer = GetNode<AudioStreamPlayer>("%AudioStreamPlayer");
        ExpandHint = GetNode<Panel>("%ExpandHint");
        LazerMenuWarningContainer = GetNode<PanelContainer>("%LazerMenuWarningContainer");
        SkinSelectorPopup = GetNode<SkinSelectorPopup>("%SkinSelectorPopup");
        UpdateClientSpecificControls();

        SkinSelectorPopup.OnSelected = s =>
        {
            Settings.Log($"Skin option '{SkinOptionComponentInSelection.SkinOption.Name}' set to: {s}");
            OptionComponentSelected(new SkinOptionValue(s));
        };

        SkinSelectorPopup.SkinComponentsContainer.SkinPreviewRequested += OnPreviewRequested;
        OsuData.AllSkinsLoaded += UpdateClientSpecificControls;
        OsuData.SkinRemoved += OnSkinRemoved;
        AudioStreamPlayer.Finished += PlayNextPreviewAudio;

        if (!Settings.Content.ArrowButtonPressed)
            AnimationPlayer.Play("hint");
    }

    public override void _ExitTree()
    {
        OsuData.AllSkinsLoaded -= UpdateClientSpecificControls;
        OsuData.SkinRemoved -= OnSkinRemoved;
        AudioStreamPlayer = null;
    }

    private void UpdateClientSpecificControls()
    {
        LazerMenuWarningContainer.Visible = OsuData.Library?.Kind == Storage.OsuClientKind.Lazer;
    }

    public void CreateOptionComponents(SkinOptionValueType defaultValueType)
    {
        SkinOptionComponents = new List<SkinOptionComponent>();

        InitialiseOptionComponents(SkinOptions, defaultValueType, 0);
    }

    private void InitialiseOptionComponents(IEnumerable<SkinOption> children, SkinOptionValueType defaultValueType, int layer)
    {
        foreach (SkinOption skinOption in children)
        {
            var instance = SkinOptionComponentScene.Instantiate<SkinOptionComponent>();
            instance.SetSkinOption(skinOption, new SkinOptionValue(defaultValueType), layer);

            instance.OnResetButtonPressed += () =>
            {
                Settings.Log($"Skin option '{skinOption.Name}' reset to default: {instance.DefaultValue}");
                SkinOptionComponentInSelection = instance;
                OptionComponentSelected(instance.DefaultValue);
            };
            instance.OnButtonPressed += () =>
            {
                bool showPreviewButtons = (skinOption is ParentSkinOption parentSkinOption && parentSkinOption.PreviewFileNames is not null)
                    || (skinOption is SkinFileOption skinFileOption && skinFileOption.IsAudio);
                SkinOptionComponentInSelection = instance;
                SkinSelectorPopup.ShowPreviewButtons = showPreviewButtons;
                SkinSelectorPopup.In();
            };

            SkinOptionComponents.Add(instance);

            if (skinOption is not ParentSkinOption parent)
                continue;

            InitialiseOptionComponents(parent.Children, defaultValueType, layer + 1);
            instance.OnArrowButtonToggled += p =>
            {
                if (instance.CreateChildrenContainer())
                {
                    foreach (SkinOption child in parent.Children)
                    {
                        SkinOptionComponent matchingComponent = SkinOptionComponents.Find(c => c.SkinOption == child);
                        if (matchingComponent is not null)
                        {
                            matchingComponent.ParentContainer = instance.ChildrenContainer;
                            matchingComponent.ParentContainer.AddChild(matchingComponent);
                        }
                    }
                }

                instance.ChildrenContainer.Visible = p;

                if (!Settings.Content.ArrowButtonPressed)
                {
                    ExpandHint.Visible = false;
                    Settings.Content.ArrowButtonPressed = true;
                }
            };

            // The top level options are the only options that are shown initially.
            if (layer == 0)
            {
                instance.ParentContainer = OptionsContainer;
                OptionsContainer.AddChild(instance);
            }
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
        SkinOptionComponentInSelection.SetOptionValue(valueSelected);

        foreach (var parent in SkinOption.GetParents(SkinOptionComponentInSelection.SkinOption, SkinOptions))
        {
            SkinOptionComponent parentOptionComponent = SkinOptionComponents.Find(c => c.SkinOption == parent);
            if (parent.Children.All(o => o.Value == valueSelected))
                parentOptionComponent.SetOptionValue(valueSelected);
            else
                parentOptionComponent.SetOptionValue(new SkinOptionValue(SkinOptionValueType.Various));
        }

        SetValueOfAllChildrenOfOption(SkinOptionComponentInSelection.SkinOption, valueSelected);
        SkinSelectorPopup.Out();
        SkinOptionComponentInSelection.CallDeferred(SkinOptionComponent.MethodName.FocusButton);

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

        SkinOptionComponents.Find(c => c.SkinOption == option).SetOptionValue(value);
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

    private void OnPreviewRequested(OsuSkin skin, bool previewing, Action finishedAction)
    {
        SkinOption skinOption = SkinOptionComponentInSelection.SkinOption;

        previewFinishedAction?.Invoke();

        if (!previewing)
        {
            AudioStreamPlayer.Stop();
            PreviewAudioStreamQueue.Clear();
            previewFinishedAction = null;
            return;
        }

        previewFinishedAction = finishedAction;

        if (skinOption is ParentSkinOption parentSkinOption)
        {
            if (parentSkinOption.PreviewFileNames is null)
                return;

            PreviewAudioStreamQueue.Clear();

            StringBuilder toastContent = new("Playing: ");
            foreach (var fileName in parentSkinOption.PreviewFileNames)
            {
                AudioStream stream = skin.GetAudioStream(fileName);
                if (stream is not null)
                {
                    //stream.ResourceName = fileName;
                    PreviewAudioStreamQueue.Enqueue(stream);
                    toastContent.Append($"{fileName}, ");
                }
            }

            if (PreviewAudioStreamQueue.Count == 0)
            {
                Settings.PushToast($"This skin doesn't have these sounds.");
                previewFinishedAction?.Invoke();
                previewFinishedAction = null;
                return;
            }

            Settings.PushToast(toastContent.ToString().TrimEnd(',', ' '));

            PlayNextPreviewAudio();
        }
        else if (skinOption is SkinFileOption skinFileOption && skinFileOption.IsAudio)
        {
            PreviewAudioStreamQueue.Clear();

            AudioStream stream = skin.GetAudioStream(skinFileOption.IncludeFileName);
            PreviewAudioStreamQueue.Enqueue(stream);

            if (stream is null)
            {
                Settings.PushToast($"This skin doesn't have this sound.");
                previewFinishedAction?.Invoke();
                previewFinishedAction = null;
                return;
            }

            Settings.PushToast($"Playing: {skinFileOption.IncludeFileName}");

            PlayNextPreviewAudio();
        }
    }

    private void PlayNextPreviewAudio()
    {
        if (PreviewAudioStreamQueue.Count == 0 || AudioStreamPlayer is null)
        {  
            previewFinishedAction?.Invoke();
            previewFinishedAction = null;
            return;
        }

        AudioStream stream = PreviewAudioStreamQueue.Dequeue();
        AudioStreamPlayer.Stream = stream;
        AudioStreamPlayer.Play();
    }
}
