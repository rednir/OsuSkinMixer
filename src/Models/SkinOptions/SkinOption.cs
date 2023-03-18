using System.Collections.Generic;

namespace OsuSkinMixer.Models;

/// <summary>Base class for all skin options.</summary>
public abstract class SkinOption
{
    public virtual string Name { get; set; }

    public SkinOptionValue Value { get; set; }

    public static IEnumerable<ParentSkinOption> GetParents(SkinOption childOption, SkinOption[] skinOptions)
    {
        var result = new List<ParentSkinOption>();
        helper(skinOptions);
        return result;

        bool helper(SkinOption[] children)
        {
            foreach (var option in children)
            {
                if (option == childOption)
                    return true;

                if (option is ParentSkinOption parentOption && helper(parentOption.Children))
                {
                    result.Add(parentOption);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Flattens a skin option hierarchy into a single list ready for iteration.</summary>
    /// <returns>A single list of skin options.</returns>
    public static IEnumerable<SkinOption> Flatten(SkinOption[] skinOptions)
    {
        var result = new List<SkinOption>();
        helper(skinOptions);
        return result;

        void helper(SkinOption[] children)
        {
            if (children == null)
                return;

            foreach (var option in children)
            {
                result.Add(option);
                helper((option as ParentSkinOption)?.Children);
            }
        }
    }

    /// <summary>Gets the default skin option hierarchy.</summary>
    public static SkinOption[] Default => new SkinOption[]
    {
            new ParentSkinOption
            {
                Name = "Interface",
                Children = new SkinOption[]
                {
                    new ParentSkinOption
                    {
                        Name = "Song select",
                        Children = new SkinOption[]
                        {
                            new ParentSkinOption
                            {
                                Name = "Ranking icons",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("ranking-d-small", false, "D rank"),
                                    new SkinFileOption("ranking-c-small", false, "C rank"),
                                    new SkinFileOption("ranking-b-small", false, "B rank"),
                                    new SkinFileOption("ranking-a-small", false, "A rank"),
                                    new SkinFileOption("ranking-s-small", false, "S rank"),
                                    new SkinFileOption("ranking-sh-small", false, "S rank (hidden)"),
                                    new SkinFileOption("ranking-x-small", false, "SS rank"),
                                    new SkinFileOption("ranking-xh-small", false, "SS rank (hidden)"),
                                },
                            },

                            new SkinFileOption("button-left", false),
                            new SkinFileOption("button-middle", false),
                            new SkinFileOption("button-right", false),
                            new SkinFileOption("menu-background", false),
                            new SkinFileOption("menu-back", false, null, true),
                            new SkinFileOption("menu-button-background", false),
                            new SkinFileOption("menu-snow", false),
                            new SkinFileOption("mode-fruits-med", false),
                            new SkinFileOption("mode-fruits-small", false),
                            new SkinFileOption("mode-fruits", false),
                            new SkinFileOption("mode-mania-med", false),
                            new SkinFileOption("mode-mania-small", false),
                            new SkinFileOption("mode-mania", false),
                            new SkinFileOption("mode-osu-med", false),
                            new SkinFileOption("mode-osu-small", false),
                            new SkinFileOption("mode-osu", false),
                            new SkinFileOption("mode-taiko-med", false),
                            new SkinFileOption("mode-taiko-small", false),
                            new SkinFileOption("mode-taiko", false),
                            new SkinFileOption("options-offset-tick", false),
                            new SkinFileOption("selection-mode*", false),
                            new SkinFileOption("selection-mods*", false),
                            new SkinFileOption("selection-options*", false),
                            new SkinFileOption("selection-random*", false),
                            new SkinFileOption("selection-tab", false),
                            new SkinFileOption("star", false),
                            new SkinFileOption("welcome_text", false),

                            new SkinIniPropertyOption("Colours", "MenuGlow"),
                            new SkinIniPropertyOption("Colours", "SongSelectActiveText"),
                            new SkinIniPropertyOption("Colours", "SongSelectInactiveText"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Mod icons",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("selection-mod-autoplay", false, "Autoplay"),
                            new SkinFileOption("selection-mod-cinema", false, "Cinema"),
                            new SkinFileOption("selection-mod-doubletime", false, "Double Time"),
                            new SkinFileOption("selection-mod-easy", false, "Easy"),
                            new SkinFileOption("selection-mod-fadein", false, "Fade-in"),
                            new SkinFileOption("selection-mod-flashlight", false, "Flashlight"),
                            new SkinFileOption("selection-mod-halftime", false, "Half Time"),
                            new SkinFileOption("selection-mod-hardrock", false, "Hard Rock"),
                            new SkinFileOption("selection-mod-hidden", false, "Hidden"),
                            new SkinFileOption("selection-mod-key1", false, "1K"),
                            new SkinFileOption("selection-mod-key2", false, "2K"),
                            new SkinFileOption("selection-mod-key3", false, "3K"),
                            new SkinFileOption("selection-mod-key4", false, "4K"),
                            new SkinFileOption("selection-mod-key5", false, "5K"),
                            new SkinFileOption("selection-mod-key6", false, "6K"),
                            new SkinFileOption("selection-mod-key7", false, "7K"),
                            new SkinFileOption("selection-mod-key8", false, "8K"),
                            new SkinFileOption("selection-mod-key9", false, "9K"),
                            new SkinFileOption("selection-mod-keycoop", false, "Co-op"),
                            new SkinFileOption("selection-mod-mirror", false, "Mirror"),
                            new SkinFileOption("selection-mod-nightcore", false, "Nightcore"),
                            new SkinFileOption("selection-mod-nofail", false, "No Fail"),
                            new SkinFileOption("selection-mod-perfect", false, "Perfect"),
                            new SkinFileOption("selection-mod-random", false, "Random"),
                            new SkinFileOption("selection-mod-relax", false, "Relax"),
                            new SkinFileOption("selection-mod-relax2", false, "Autopilot"),
                            new SkinFileOption("selection-mod-scorev2", false, "ScoreV2"),
                            new SkinFileOption("selection-mod-spunout", false, "Spun Out"),
                            new SkinFileOption("selection-mod-suddendeath", false, "Sudden Death"),
                            new SkinFileOption("selection-mod-target", false, "Target Practice"),
                            new SkinFileOption("selection-mod-touchdevice", false, "Touch Device"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Results screen",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("ranking-panel", false, "Ranking panel"),
                            new SkinFileOption("ranking-title", false, "Ranking title"),
                            new SkinFileOption("ranking-d", false, "D rank"),
                            new SkinFileOption("ranking-c", false, "C rank"),
                            new SkinFileOption("ranking-b", false, "B rank"),
                            new SkinFileOption("ranking-a", false, "A rank"),
                            new SkinFileOption("ranking-s", false, "S rank"),
                            new SkinFileOption("ranking-sh", false, "S rank (hidden)"),
                            new SkinFileOption("ranking-x", false, "SS rank"),
                            new SkinFileOption("ranking-xh", false, "SS rank (hidden)"),
                            new SkinFileOption("ranking-accuracy", false, "Accuracy title"),
                            new SkinFileOption("ranking-maxcombo", false, "Max combo title"),
                            new SkinFileOption("ranking-graph", false, "Peformance graph"),
                            new SkinFileOption("ranking-perfect", false, "Perfect combo text"),
                            new SkinFileOption("ranking-retry", false, "Retry button"),
                            new SkinFileOption("ranking-replay", false, "Watch replay button"),
                            new SkinFileOption("pause-replay", false, "Watch replay button"),
                            new SkinFileOption("ranking-winner", false, "Multi winner"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Pause/Fail screen",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("pause-overlay", false, "Pause overlay"),
                            new SkinFileOption("fail-background", false, "Fail background"),
                            new SkinFileOption("pause-continue", false, "Continue button"),
                            new SkinFileOption("pause-retry", false, "Retry button"),
                            new SkinFileOption("pause-back", false, "Back button"),
                            new SkinFileOption("arrow-pause", false, "Selection arrow"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "In-game",
                        Children = new SkinOption[]
                        {
                            new ParentSkinOption
                            {
                                Name = "Health bar",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("scorebar-bg", false),
                                    new SkinFileOption("scorebar-colour", false, null, true),
                                    new SkinFileOption("scorebar-ki", false),
                                    new SkinFileOption("scorebar-kidanger", false),
                                    new SkinFileOption("scorebar-kidanger2", false),
                                    new SkinFileOption("scorebar-marker", false),
                                },
                            },
                            new ParentSkinOption
                            {
                                Name = "Countdown",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("ready", false, "Ready"),
                                    new SkinFileOption("count3", false, "3"),
                                    new SkinFileOption("count2", false, "2"),
                                    new SkinFileOption("count1", false, "1"),
                                    new SkinFileOption("go", false, "Go"),
                                },
                            },
                            new ParentSkinOption
                            {
                                Name = "Fonts",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("scoreentry-*", false),
                                    new SkinFileOption("score-*", false),

                                    new SkinIniPropertyOption("Fonts", "ScorePrefix"),
                                    new SkinIniPropertyOption("Fonts", "ScoreOverlap"),
                                    new SkinIniPropertyOption("Fonts", "ComboPrefix"),
                                    new SkinIniPropertyOption("Fonts", "ComboOverlap"),
                                }
                            },
                            new ParentSkinOption
                            {
                                Name = "Key overlay",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("inputoverlay*", false),

                                    new SkinIniPropertyOption("Colours", "InputOverlayText"),
                                },
                            },

                            new SkinFileOption("play-skip", false, "Skip button", true),
                            new SkinFileOption("section-fail", false, "Section pass"),
                            new SkinFileOption("section-pass", false, "Section fail"),
                            new SkinFileOption("play-warningarrow", false, "Warning/selection arrows"),
                            new SkinFileOption("arrow-warning", false, "Warning arrows"),
                            new SkinFileOption("play-unranked", false, "Unranked text"),
                            new SkinFileOption("masking-border", false, "Masking border"),

                            new SkinIniPropertyOption("General", "ComboBurstRandom"),
                        },
                    },
                },
            },
            new ParentSkinOption
            {
                Name = "Gameplay",
                Children = new SkinOption[]
                {
                    new ParentSkinOption
                    {
                        Name = "osu!",
                        Children = new SkinOption[]
                        {
                            new ParentSkinOption
                            {
                                Name = "Hitcircles",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("approachcircle", false, "Approach circle"),
                                    new SkinFileOption("default-*", false, "Numbers"),
                                    new SkinFileOption("hitcircle", false, "Hitcircle"),
                                    new SkinFileOption("hitcircleoverlay", false, "Hitcircle overlay"),
                                    new SkinFileOption("hitcircleselect", false, "Editor selection"),
                                    new SkinFileOption("target*", false, "Target practice hitcircle"),

                                    new SkinIniPropertyOption("General", "HitCircleOverlayAboveNumber"),
                                    new SkinIniPropertyOption("Fonts", "HitCirclePrefix"),
                                    new SkinIniPropertyOption("Fonts", "HitCircleOverlap"),
                                },
                            },
                            new ParentSkinOption
                            {
                                Name = "Sliders",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("sliderb", false, "Slider ball", true),
                                    new SkinFileOption("sliderendcircle*", false, "Slider end circle"),
                                    new SkinFileOption("sliderfollowcircle", false, "Slider follow circle"),
                                    new SkinFileOption("sliderscorepoint", false, "Slider tick"),
                                    new SkinFileOption("sliderstartcircle*", false, "Slider start circle"),
                                    new SkinFileOption("reversearrow", false, "Reverse arrow"),
                                    new SkinFileOption("sliderpoint*", false, "Legacy slider point"),

                                    new SkinIniPropertyOption("General", "AllowSliderBallTint"),
                                    new SkinIniPropertyOption("General", "SliderBallFlip"),
                                    new SkinIniPropertyOption("Colours", "SliderBall"),
                                    new SkinIniPropertyOption("Colours", "SliderBorder"),
                                    new SkinIniPropertyOption("Colours", "SliderTrackOverride"),
                                },
                            },
                            new ParentSkinOption
                            {
                                Name = "Combo Colours",
                                Children = new SkinOption[]
                                {
                                    new SkinIniPropertyOption("Colours", "Combo1"),
                                    new SkinIniPropertyOption("Colours", "Combo2"),
                                    new SkinIniPropertyOption("Colours", "Combo3"),
                                    new SkinIniPropertyOption("Colours", "Combo4"),
                                    new SkinIniPropertyOption("Colours", "Combo5"),
                                    new SkinIniPropertyOption("Colours", "Combo6"),
                                    new SkinIniPropertyOption("Colours", "Combo7"),
                                    new SkinIniPropertyOption("Colours", "Combo8"),
                                },
                            },
                            new ParentSkinOption
                            {
                                Name = "Spinners",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("spinner-approachcircle", false),
                                    new SkinFileOption("spinner-background", false),
                                    new SkinFileOption("spinner-bottom", false),
                                    new SkinFileOption("spinner-circle", false),
                                    new SkinFileOption("spinner-clear", false),
                                    new SkinFileOption("spinner-glow", false),
                                    new SkinFileOption("spinner-metre", false),
                                    new SkinFileOption("spinner-middle*", false),
                                    new SkinFileOption("spinner-osu", false),
                                    new SkinFileOption("spinner-rpm", false),
                                    new SkinFileOption("spinner-spin", false),
                                    new SkinFileOption("spinner-top", false),
                                    new SkinFileOption("spinner-warning", false),

                                    new SkinIniPropertyOption("General", "SpinnerFadePlayfield"),
                                    new SkinIniPropertyOption("General", "SpinnerNoBlink"),
                                    new SkinIniPropertyOption("Colours", "SpinnerBackground"),
                                    new SkinIniPropertyOption("Colours", "StarBreakAdditive"),
                                },
                            },
                            new ParentSkinOption
                            {
                                Name = "Hit judgements",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("hit0", false, "Miss", true),
                                    new SkinFileOption("hit50", false, "50", true),
                                    new SkinFileOption("hit100", false, "100", true),
                                    new SkinFileOption("hit300", false, "300", true),
                                    new SkinFileOption("particle*", false, "Particles"),
                                },
                            },
                            new SkinFileOption("followpoint", false, "Follow points", true),
                            new SkinFileOption("comboburst", false, "Combo burst"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "osu!taiko",
                        Children = new SkinOption[]
                        {
                            new ParentSkinOption()
                            {
                                Name = "Hit judgements",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("taiko-hit0", false, "Miss"),
                                    new SkinFileOption("taiko-hit100", false, "100"),
                                    new SkinFileOption("taiko-hit100k", false, "100k"),
                                    new SkinFileOption("taiko-hit300", false, "300"),
                                    new SkinFileOption("taiko-hit300g", false, "300g"),
                                    new SkinFileOption("taiko-hit300k", false, "300k"),
                                },
                            },
                            new ParentSkinOption()
                            {
                                Name = "Pippidon",
                                Children = new SkinOption[]
                                {
                                    new SkinFileOption("pippidonclear", false),
                                    new SkinFileOption("pippidonfail", false),
                                    new SkinFileOption("pippidonfailidle", false),
                                    new SkinFileOption("pippidonfailkiai", false),
                                },
                            },
                            new SkinFileOption("taiko-bar-left", false),
                            new SkinFileOption("taiko-bar-right-glow", false),
                            new SkinFileOption("taiko-bar-right", false),
                            new SkinFileOption("taiko-barline", false),
                            new SkinFileOption("taiko-drum-inner", false),
                            new SkinFileOption("taiko-drum-outer", false),
                            new SkinFileOption("taiko-flower-group", false),
                            new SkinFileOption("taiko-glow", false),
                            new SkinFileOption("taiko-roll-end", false),
                            new SkinFileOption("taiko-roll-middle", false),
                            new SkinFileOption("taiko-slider-fail", false),
                            new SkinFileOption("taiko-slider", false),
                            new SkinFileOption("taikobigcircle*", false),
                            new SkinFileOption("taikohitcircle*", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "osu!catch",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("comboburst-fruits", false),
                            new SkinFileOption("fruit-apple*", false),
                            new SkinFileOption("fruit-bananas*", false),
                            new SkinFileOption("fruit-catcher-fail", false),
                            new SkinFileOption("fruit-catcher-idle", false),
                            new SkinFileOption("fruit-catcher-kiai", false),
                            new SkinFileOption("fruit-drop*", false),
                            new SkinFileOption("fruit-grapes*", false),
                            new SkinFileOption("fruit-orange*", false),
                            new SkinFileOption("fruit-pear*", false),
                            new SkinFileOption("fruit-ryuuta", false),

                            new SkinIniPropertyOption("CatchTheBeat", "HyperDash"),
                            new SkinIniPropertyOption("CatchTheBeat", "HyperDashFruit"),
                            new SkinIniPropertyOption("CatchTheBeat", "HyperDashAfterImage"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "osu!mania",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("comboburst-mania", false),
                            new SkinFileOption("mania*", false),
                            new SkinFileOption("lighting*", false),

                            new SkinIniSectionOption("Mania", "Keys", "1"),
                            new SkinIniSectionOption("Mania", "Keys", "2"),
                            new SkinIniSectionOption("Mania", "Keys", "3"),
                            new SkinIniSectionOption("Mania", "Keys", "4"),
                            new SkinIniSectionOption("Mania", "Keys", "5"),
                            new SkinIniSectionOption("Mania", "Keys", "6"),
                            new SkinIniSectionOption("Mania", "Keys", "7"),
                            new SkinIniSectionOption("Mania", "Keys", "8"),
                            new SkinIniSectionOption("Mania", "Keys", "9"),
                        },
                    },
                    new SkinIniPropertyOption("General", "AnimationFramerate"),
                }
            },
            new ParentSkinOption
            {
                Name = "Cursor",
                Children = new SkinOption[]
                {
                    new ParentSkinOption
                    {
                        Name = "Head",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("cursor", false),

                            new SkinIniPropertyOption("General", "CursorCentre"),
                            new SkinIniPropertyOption("General", "CursorExpand"),
                            new SkinIniPropertyOption("General", "CursorRotate"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Trail",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("cursortrail", false),
                            new SkinFileOption("cursormiddle", false),
                            new SkinFileOption("star2", false),

                            new SkinIniPropertyOption("General", "CursorTrailRotate"),
                        },
                    },
                    new SkinFileOption("cursor-smoke", false),
                },
            },
            new ParentSkinOption
            {
                Name = "Hitsounds",
                Children = new SkinOption[]
                {
                    new ParentSkinOption
                    {
                        Name = "Normal",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("normal-hitclap", true),
                            new SkinFileOption("normal-hitfinish", true),
                            new SkinFileOption("normal-hitnormal", true),
                            new SkinFileOption("normal-hitwhistle", true),
                            new SkinFileOption("normal-sliderslide", true),
                            new SkinFileOption("normal-slidertick", true),
                            new SkinFileOption("normal-sliderwhistle", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Soft",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("soft-hitclap", true),
                            new SkinFileOption("soft-hitfinish", true),
                            new SkinFileOption("soft-hitnormal", true),
                            new SkinFileOption("soft-hitwhistle", true),
                            new SkinFileOption("soft-sliderslide", true),
                            new SkinFileOption("soft-slidertick", true),
                            new SkinFileOption("soft-sliderwhistle", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Drum",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("drum-hitclap", true),
                            new SkinFileOption("drum-hitfinish", true),
                            new SkinFileOption("drum-hitnormal", true),
                            new SkinFileOption("drum-hitwhistle", true),
                            new SkinFileOption("drum-sliderslide", true),
                            new SkinFileOption("drum-slidertick", true),
                            new SkinFileOption("drum-sliderwhistle", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Spinner",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("spinnerbonus", true),
                            new SkinFileOption("spinnerspin", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Nightcore beats",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("nightcore-clap", true),
                            new SkinFileOption("nightcore-finish", true),
                            new SkinFileOption("nightcore-hat", true),
                            new SkinFileOption("nightcore-kick", true),
                        },
                    },
                    new SkinFileOption("combobreak", true),
                    new SkinFileOption("comboburst", true),

                    new SkinIniPropertyOption("General", "CustomComboBurstSounds"),
                    new SkinIniPropertyOption("General", "LayeredHitSounds"),
                    new SkinIniPropertyOption("General", "SpinnerFrequencyModulate"),
                },
            },
            new ParentSkinOption
            {
                Name = "Menu Sounds",
                Children = new SkinOption[]
                {
                    new ParentSkinOption
                    {
                        Name = "Interface",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("applause", true),
                            new SkinFileOption("back-button-click", true),
                            new SkinFileOption("back-button-hover", true),
                            new SkinFileOption("check-off", true),
                            new SkinFileOption("check-on", true),
                            new SkinFileOption("click-close", true),
                            new SkinFileOption("click-short-confirm", true),
                            new SkinFileOption("click-short", true),
                            new SkinFileOption("heartbeat", true),
                            new SkinFileOption("key*", true),
                            new SkinFileOption("match*", true),
                            new SkinFileOption("menu-back*", true),
                            new SkinFileOption("menu-charts*", true),
                            new SkinFileOption("menu-direct*", true),
                            new SkinFileOption("menu-edit*", true),
                            new SkinFileOption("menu-exit*", true),
                            new SkinFileOption("menu-freeplay*", true),
                            new SkinFileOption("menu-multiplayer*", true),
                            new SkinFileOption("menu-options*", true),
                            new SkinFileOption("menu-play*", true),
                            new SkinFileOption("menuback", true),
                            new SkinFileOption("menuclick", true),
                            new SkinFileOption("menuhit", true),
                            new SkinFileOption("metronomelow", true),
                            new SkinFileOption("multi-skipped", true),
                            new SkinFileOption("seeya", true),
                            new SkinFileOption("select-*", true),
                            new SkinFileOption("shutter", true),
                            new SkinFileOption("sliderbar", true),
                            new SkinFileOption("welcome", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "In-game",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("count1s", true),
                            new SkinFileOption("count2s", true),
                            new SkinFileOption("count3s", true),
                            new SkinFileOption("failsound", true),
                            new SkinFileOption("gos", true),
                            new SkinFileOption("pause-back*", true),
                            new SkinFileOption("pause-continue*", true),
                            new SkinFileOption("pause-retry*", true),
                            new SkinFileOption("pause-hover", true),
                            new SkinFileOption("pause-loop", true),
                            new SkinFileOption("readys", true),
                            new SkinFileOption("sectionfail", true),
                            new SkinFileOption("sectionpass", true),
                        },
                    },
                },
            },
    };
}
