using System.Collections.Generic;

namespace OsuSkinMixer
{
    public static class SkinOptions
    {
        public static OptionInfo[] Default => new OptionInfo[]
        {
            new OptionInfo
            {
                Name = "Interface",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Song select",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["Colours"] = new string[]
                            {
                                "MenuGlow",
                                "SongSelectActiveText",
                                "SongSelectInactiveText",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "button-left",
                            "button-middle",
                            "button-right",
                            "menu*",
                            "mode-*",
                            "options-offset-tick",
                            "ranking-a-small",
                            "ranking-b-small",
                            "ranking-c-small",
                            "ranking-d-small",
                            "ranking-s-small",
                            "ranking-sh-small",
                            "ranking-x-small",
                            "ranking-xh-small",
                            "selection-*",
                            "star",
                            "star2",
                            "welcome_text",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Results screen",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "ranking-a",
                            "ranking-b",
                            "ranking-c",
                            "ranking-d",
                            "ranking-s",
                            "ranking-sh",
                            "ranking-x",
                            "ranking-xh",
                            "ranking-accuracy",
                            "ranking-graph",
                            "ranking-maxcombo",
                            "ranking-panel",
                            "ranking-perfect",
                            "ranking-replay",
                            "ranking-retry",
                            "ranking-title",
                            "ranking-winner",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "In-game",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["General"] = new string[]
                            {
                                "ComboBurstRandom",
                            },
                            ["Colours"] = new string[]
                            {
                                "InputOverlayText",
                            },
                            ["Fonts"] = new string[]
                            {
                                "ScorePrefix",
                                "ScoreOverlap",
                                "ComboPrefix",
                                "ComboOverlap",
                            }
                        },
                        IncludeFileNames = new string[]
                        {
                            "arrow-pause",
                            "arrow-warning",
                            "count-1",
                            "count-2",
                            "count-3",
                            "fail-background",
                            "go",
                            "inputoverlay*",
                            "pause*",
                            "play*",
                            "scorebar-*",
                            "scoreentry-*",
                            "section-*",
                            "ready",
                            "score-*",
                            // For some reason whitecat 2.1 skin uses this prefix instead of "score-[0-9].png",
                            // this is just a hotfix for that since it's a popular skin.
                            "numbers-*",
                        }
                    },
                },
            },
            new OptionInfo
            {
                Name = "Gameplay",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "osu!",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["General"] = new string[]
                            {
                                "AllowSliderBallTint",
                                "AnimationFramerate",
                                "HitCircleOverlayAboveNumber",
                                "SliderBallFlip",
                                "SpinnerFadePlayfield",
                                "SpinnerNoBlink",
                            },
                            ["Colours"] = new string[]
                            {
                                "Combo1",
                                "Combo2",
                                "Combo3",
                                "Combo4",
                                "Combo5",
                                "Combo6",
                                "Combo7",
                                "Combo8",
                                "SliderBall",
                                "SliderBorder",
                                "SliderTrackOverride",
                                "SpinnerBackground",
                                "StarBreakAdditive",
                            },
                            ["Fonts"] = new string[]
                            {
                                "HitCirclePrefix",
                                "HitCircleOverlap",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "approachcircle",
                            "comboburst",
                            "default-*",
                            "followpoint*",
                            "hit*",
                            "particle*",
                            "reversearrow",
                            "sliderb*",
                            "sliderendcircle*",
                            "sliderfollowcircle",
                            "sliderpoint*",
                            "sliderstartcircle*",
                            "spinner-*",
                            "target*",
                            "masking-border",    // These two elements are global to all gamemodes, but there's
                            "star-2"             // no real point creating another sub-option just for these.
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Catch the beat",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["CatchTheBeat"] = new string[]
                            {
                                "HyperDash",
                                "HyperDashFruit",
                                "HyperDashAfterImage",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "comboburst-fruits",
                            "fruit-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Taiko",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "taiko*",
                            "pippidon*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Mania",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["Mania"] = new string[]
                            {
                                "*",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "mania*",
                            "lighting*"
                        },
                    },
                },
            },
            new OptionInfo
            {
                Name = "Cursor",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Head",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            {
                                "General", new string[]
                                {
                                    "CursorCentre",
                                    "CursorExpand",
                                    "CursorRotate",
                                }
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "cursor",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Trail",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            {
                                "General", new string[]
                                {
                                    "CursorTrailRotate",
                                }
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "cursortrail",
                            "cursormiddle",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Smoke",
                        IsAudio = false,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "cursor-smoke",
                        },
                    },
                },
            },
            new OptionInfo
            {
                Name = "Hitsounds",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Normal",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "normal-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Soft",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "soft-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Drum",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "drum-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Spinner",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "spinner*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Nightcore beats",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "nightcore-*",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "Combobreak (+)",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>
                        {
                            ["General"] = new string[]
                            {
                                "CustomComboBurstSounds",
                                "LayeredHitSounds",
                                "SpinnerFrequencyModulate",
                            },
                        },
                        IncludeFileNames = new string[]
                        {
                            "combobreak",
                            "comboburst",
                        },
                    },
                },
            },
            new OptionInfo
            {
                Name = "Menu Sounds",
                SubOptions = new SubOptionInfo[]
                {
                    new SubOptionInfo
                    {
                        Name = "Interface",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "applause",
                            "back-button-click",
                            "back-button-hover",
                            "check-off",
                            "check-on",
                            "click-close",
                            "click-short-confirm",
                            "click-short",
                            "heartbeat",
                            "key*",
                            "match*",
                            "menu*",
                            "metronomelow",
                            "multi-skipped",
                            "seeya",
                            "select-*",
                            "shutter",
                            "sliderbar",
                            "welcome",
                        },
                    },
                    new SubOptionInfo
                    {
                        Name = "In-game",
                        IsAudio = true,
                        IncludeSkinIniProperties = new Dictionary<string, string[]>(),
                        IncludeFileNames = new string[]
                        {
                            "count1s",
                            "count2s",
                            "count3s",
                            "failsound",
                            "gos",
                            "pause*",
                            "readys",
                            "section*",
                        },
                    },
                },
            },
        };
    }
}