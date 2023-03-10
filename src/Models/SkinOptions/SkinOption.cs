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
                            new SkinFileOption("button-left", false),
                            new SkinFileOption("button-middle", false),
                            new SkinFileOption("button-right", false),
                            new SkinFileOption("menu-background", false),
                            new SkinFileOption("menu-back*", false),
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
                            new SkinFileOption("ranking-a-small", false),
                            new SkinFileOption("ranking-b-small", false),
                            new SkinFileOption("ranking-c-small", false),
                            new SkinFileOption("ranking-d-small", false),
                            new SkinFileOption("ranking-s-small", false),
                            new SkinFileOption("ranking-sh-small", false),
                            new SkinFileOption("ranking-x-small", false),
                            new SkinFileOption("ranking-xh-small", false),
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
                            new SkinFileOption("selection-mod-autoplay", false),
                            new SkinFileOption("selection-mod-cinema", false),
                            new SkinFileOption("selection-mod-doubletime", false),
                            new SkinFileOption("selection-mod-easy", false),
                            new SkinFileOption("selection-mod-fadein", false),
                            new SkinFileOption("selection-mod-flashlight", false),
                            new SkinFileOption("selection-mod-halftime", false),
                            new SkinFileOption("selection-mod-hardrock", false),
                            new SkinFileOption("selection-mod-hidden", false),
                            new SkinFileOption("selection-mod-key1", false),
                            new SkinFileOption("selection-mod-key2", false),
                            new SkinFileOption("selection-mod-key3", false),
                            new SkinFileOption("selection-mod-key4", false),
                            new SkinFileOption("selection-mod-key5", false),
                            new SkinFileOption("selection-mod-key6", false),
                            new SkinFileOption("selection-mod-key7", false),
                            new SkinFileOption("selection-mod-key8", false),
                            new SkinFileOption("selection-mod-key9", false),
                            new SkinFileOption("selection-mod-keycoop", false),
                            new SkinFileOption("selection-mod-mirror", false),
                            new SkinFileOption("selection-mod-nightcore", false),
                            new SkinFileOption("selection-mod-nofail", false),
                            new SkinFileOption("selection-mod-perfect", false),
                            new SkinFileOption("selection-mod-random", false),
                            new SkinFileOption("selection-mod-relax", false),
                            new SkinFileOption("selection-mod-relax2", false),
                            new SkinFileOption("selection-mod-scorev2", false),
                            new SkinFileOption("selection-mod-spunout", false),
                            new SkinFileOption("selection-mod-suddendeath", false),
                            new SkinFileOption("selection-mod-target", false),
                            new SkinFileOption("selection-mod-touchdevice", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Results screen",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("pause-replay", false),
                            new SkinFileOption("ranking-a", false),
                            new SkinFileOption("ranking-b", false),
                            new SkinFileOption("ranking-c", false),
                            new SkinFileOption("ranking-d", false),
                            new SkinFileOption("ranking-s", false),
                            new SkinFileOption("ranking-sh", false),
                            new SkinFileOption("ranking-x", false),
                            new SkinFileOption("ranking-xh", false),
                            new SkinFileOption("ranking-accuracy", false),
                            new SkinFileOption("ranking-graph", false),
                            new SkinFileOption("ranking-maxcombo", false),
                            new SkinFileOption("ranking-panel", false),
                            new SkinFileOption("ranking-perfect", false),
                            new SkinFileOption("ranking-replay", false),
                            new SkinFileOption("ranking-retry", false),
                            new SkinFileOption("ranking-title", false),
                            new SkinFileOption("ranking-winner", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Pause/Fail screen",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("fail-background", false),
                            new SkinFileOption("pause-back", false),
                            new SkinFileOption("pause-continue", false),
                            new SkinFileOption("pause-overlay", false),
                            new SkinFileOption("pause-retry", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "In-game",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("arrow-pause", false),
                            new SkinFileOption("arrow-warning", false),
                            new SkinFileOption("count1", false),
                            new SkinFileOption("count2", false),
                            new SkinFileOption("count3", false),
                            new SkinFileOption("go", false),
                            new SkinFileOption("inputoverlay*", false),
                            new SkinFileOption("masking-border", false),
                            new SkinFileOption("play-skip*", false),
                            new SkinFileOption("play-unranked", false),
                            new SkinFileOption("play-warningarrow", false),
                            new SkinFileOption("scorebar-bg", false),
                            new SkinFileOption("scorebar-colour*", false),
                            new SkinFileOption("scorebar-ki", false),
                            new SkinFileOption("scorebar-kidanger", false),
                            new SkinFileOption("scorebar-kidanger2", false),
                            new SkinFileOption("scorebar-marker", false),
                            new SkinFileOption("scoreentry-*", false),
                            new SkinFileOption("section-fail", false),
                            new SkinFileOption("section-pass", false),
                            new SkinFileOption("ready", false),
                            new SkinFileOption("score-*", false),

                            new SkinIniPropertyOption("General", "ComboBurstRandom"),
                            new SkinIniPropertyOption("Colours", "InputOverlayText"),
                            new SkinIniPropertyOption("Fonts", "ScorePrefix"),
                            new SkinIniPropertyOption("Fonts", "ScoreOverlap"),
                            new SkinIniPropertyOption("Fonts", "ComboPrefix"),
                            new SkinIniPropertyOption("Fonts", "ComboOverlap"),
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
                                    new SkinFileOption("approachcircle", false),
                                    new SkinFileOption("default-*", false),
                                    new SkinFileOption("hitcircle", false),
                                    new SkinFileOption("hitcircleoverlay", false),
                                    new SkinFileOption("hitcircleselect", false),
                                    new SkinFileOption("target*", false),

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
                                    new SkinFileOption("sliderb*", false),
                                    new SkinFileOption("sliderendcircle*", false),
                                    new SkinFileOption("sliderfollowcircle", false),
                                    new SkinFileOption("sliderpoint*", false),
                                    new SkinFileOption("sliderscorepoint", false),
                                    new SkinFileOption("sliderstartcircle*", false),
                                    new SkinFileOption("reversearrow", false),

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
                                    new SkinFileOption("hit0*", false),
                                    new SkinFileOption("hit100*", false),
                                    new SkinFileOption("hit300*", false),
                                    new SkinFileOption("hit50*", false),
                                    new SkinFileOption("particle*", false),
                                },
                            },
                            new SkinFileOption("followpoint*", false),
                            new SkinFileOption("comboburst", false),
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
                                    new SkinFileOption("taiko-hit0", false),
                                    new SkinFileOption("taiko-hit100", false),
                                    new SkinFileOption("taiko-hit100k", false),
                                    new SkinFileOption("taiko-hit300", false),
                                    new SkinFileOption("taiko-hit300g", false),
                                    new SkinFileOption("taiko-hit300k", false),
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
