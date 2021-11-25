using System.Collections.Generic;
using Godot;

namespace OsuSkinMixer
{
    public abstract class SkinOption : Object
    {
        public virtual string Name { get; set; }

        public OptionButton OptionButton { get; set; }

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
                            new SkinIniOption("Colours", "MenuGlow"),
                            new SkinIniOption("Colours", "SongSelectActiveText"),
                            new SkinIniOption("Colours", "SongSelectInactiveText"),

                            new SkinFileOption("button-left", false),
                            new SkinFileOption("button-middle", false),
                            new SkinFileOption("button-right", false),
                            new SkinFileOption("menu*", false),
                            new SkinFileOption("mode-*", false),
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
                            new SkinFileOption("star2", false),
                            new SkinFileOption("welcome_text", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Mod icons",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("selection-mod-*", false)
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Results screen",
                        Children = new SkinOption[]
                        {
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
                        Name = "In-game",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("General", "ComboBurstRandom"),
                            new SkinIniOption("Colours", "InputOverlayText"),
                            new SkinIniOption("Fonts", "ScorePrefix"),
                            new SkinIniOption("Fonts", "ScoreOverlap"),
                            new SkinIniOption("Fonts", "ComboPrefix"),
                            new SkinIniOption("Fonts", "ComboOverlap"),

                            new SkinFileOption("arrow-pause", false),
                            new SkinFileOption("arrow-warning", false),
                            new SkinFileOption("count1", false),
                            new SkinFileOption("count2", false),
                            new SkinFileOption("count3", false),
                            new SkinFileOption("fail-background", false),
                            new SkinFileOption("go", false),
                            new SkinFileOption("inputoverlay*", false),
                            new SkinFileOption("masking-border", false),
                            new SkinFileOption("pause*", false),
                            new SkinFileOption("play*", false),
                            new SkinFileOption("scorebar-*", false),
                            new SkinFileOption("scoreentry-*", false),
                            new SkinFileOption("section-*", false),
                            new SkinFileOption("ready", false),
                            new SkinFileOption("score-*", false),
                            new SkinFileOption("numbers-*", false),
                        },
                    },
                },
            },
            new ParentSkinOption
            {
                Name = "Gameplay",
                Children = new SkinOption[]
                {
                    new SkinFileOption("comboburst", false),
                    new ParentSkinOption
                    {
                        Name = "Hitcircles",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("General", "HitCircleOverlayAboveNumber"),
                            new SkinIniOption("Fonts", "HitCirclePrefix"),
                            new SkinIniOption("Fonts", "HitCircleOverlap"),

                            new SkinFileOption("approachcircle", false),
                            new SkinFileOption("default-*", false),
                            new SkinFileOption("hitcircle*", false),
                            new SkinFileOption("target*", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Sliders",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("General", "AllowSliderBallTint"),
                            new SkinIniOption("General", "SliderBallFlip"),
                            new SkinIniOption("Colours", "SliderBall"),
                            new SkinIniOption("Colours", "SliderBorder"),
                            new SkinIniOption("Colours", "SliderTrackOverride"),

                            new SkinFileOption("sliderb*", false),
                            new SkinFileOption("sliderendcircle*", false),
                            new SkinFileOption("sliderfollowcircle", false),
                            new SkinFileOption("sliderpoint*", false),
                            new SkinFileOption("sliderstartcircle*", false),
                            new SkinFileOption("sliderscorepoint", false),
                            new SkinFileOption("reversearrow", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Combo Colours",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("Colours", "Combo1"),
                            new SkinIniOption("Colours", "Combo2"),
                            new SkinIniOption("Colours", "Combo3"),
                            new SkinIniOption("Colours", "Combo4"),
                            new SkinIniOption("Colours", "Combo5"),
                            new SkinIniOption("Colours", "Combo6"),
                            new SkinIniOption("Colours", "Combo7"),
                            new SkinIniOption("Colours", "Combo8"),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Spinners",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("General", "SpinnerFadePlayfield"),
                            new SkinIniOption("General", "SpinnerNoBlink"),
                            new SkinIniOption("Colours", "SpinnerBackground"),
                            new SkinIniOption("Colours", "StarBreakAdditive"),

                            new SkinFileOption("spinner-*", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Followpoints",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("followpoint*", false),
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
                    new ParentSkinOption
                    {
                        Name = "+ fruits",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("CatchTheBeat", "HyperDash"),
                            new SkinIniOption("CatchTheBeat", "HyperDashFruit"),
                            new SkinIniOption("CatchTheBeat", "HyperDashAfterImage"),

                            new SkinFileOption("comboburst-fruits", false),
                            new SkinFileOption("fruit-*", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "+ taiko",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("taiko*", false),
                            new SkinFileOption("pippidon*", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "+ mania",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("Mania", "*"),
                            new SkinFileOption("mania*", false),
                            new SkinFileOption("lighting*", false),
                        },
                    },
                    new SkinIniOption("General", "AnimationFramerate"),
                },
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
                            new SkinIniOption("General", "CursorCentre"),
                            new SkinIniOption("General", "CursorExpand"),
                            new SkinIniOption("General", "CursorRotate"),

                            new SkinFileOption("cursor", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Trail",
                        Children = new SkinOption[]
                        {
                            new SkinIniOption("General", "CursorTrailRotate"),

                            new SkinFileOption("cursortrail", false),
                            new SkinFileOption("cursormiddle", false),
                            new SkinFileOption("star-2", false),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Smoke",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("cursor-smoke", false),
                        },
                    },
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
                            new SkinFileOption("normal-*", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Soft",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("soft-*", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Drum",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("drum-*", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Spinner",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("spinner*", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Nightcore beats",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("nightcore-*", true),
                        },
                    },
                    new ParentSkinOption
                    {
                        Name = "Combobreak (+)",
                        Children = new SkinOption[]
                        {
                            new SkinFileOption("combobreak", true),
                        },
                    },
                    new SkinIniOption("General", "CustomComboBurstSounds"),
                    new SkinIniOption("General", "LayeredHitSounds"),
                    new SkinIniOption("General", "SpinnerFrequencyModulate"),
                    new SkinFileOption("comboburst", true),
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
                            new SkinFileOption("menu*", true),
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
                            new SkinFileOption("pause*", true),
                            new SkinFileOption("readys", true),
                            new SkinFileOption("section*", true),
                        },
                    },
                },
            },
        };
    }
}