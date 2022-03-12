using Lavalink4NET.Filters;

namespace KBot.Modules.Music.Helpers;

public static class Filters
{
    public static EqualizerBand[] BassBoost()
    {
        return new EqualizerBand[]
        {
            new(0, 0.2f),
            new(1, 0.2f),
            new(2, 0.2f)
        };
    }
    public static EqualizerBand[] Pop()
    {
        return new EqualizerBand[]
        {
            new(0, 0.65f),
            new(1, 0.45f),
            new(2, -0.25f),
            new(3, -0.25f),
            new(4, -0.25f),
            new(5, 0.45f),
            new(6, 0.55f),
            new(7, 0.6f),
            new(8, 0.6f),
            new(9, 0.6f),
        };
    }
    public static EqualizerBand[] Soft()
    {
        return new EqualizerBand[]
        {
            new(8, -0.25f),
            new(9, -0.25f),
            new(10, -0.25f),
            new(11, -0.25f),
            new(12, -0.25f),
            new(13, -0.25f)
        };
    }
    public static EqualizerBand[] TrebleBass()
    {
        return new EqualizerBand[]
        {
            new(0, 0.6f),
            new(1, 0.67f),
            new(2, 0.67f),
            new(4, -0.2f),
            new(5, 0.15f),
            new(6, -0.25f),
            new(7, 0.23f),
            new(8, 0.35f),
            new(9, 0.45f),
            new(10, 0.55f),
            new(11, 0.6f),
            new(12, 0.55f),
        };
    }

    public static TimescaleFilterOptions NightCore()
    {
        return new TimescaleFilterOptions
        {
            Speed = 1.165f,
            Pitch = 1.125f,
            Rate = 1.05f
        };
    }

    public static RotationFilterOptions EightD()
    {
        return new RotationFilterOptions
        {
            Frequency = 0.2f
        };
    }
    public static TimescaleFilterOptions VaporWave()
    {
        return new TimescaleFilterOptions
        {
            Speed = 1.0f,
            Pitch = 0.5f,
            Rate = 1.0f
        };
        /*
        new TremoloFilter()
        {
            Depth = 0.3,
            Frequency = 14
        }*/

    }
    public static TimescaleFilterOptions Doubletime()
    {
        return new TimescaleFilterOptions
        {
            Speed = 1.165f,
            Pitch = 1.0f,
            Rate = 1.0f
        };
    }
    public static TimescaleFilterOptions Slowmotion()
    {
        return new TimescaleFilterOptions
        {
            Speed = 0.5f,
            Pitch = 1.0f,
            Rate = 0.8f
        };
    }
    public static TimescaleFilterOptions Chipmunk()
    {
        return new TimescaleFilterOptions
        {
            Speed = 1.05f,
            Pitch = 1.35f,
            Rate = 1.25f
        };
    }
    public static TimescaleFilterOptions Darthvader()
    {
        return new TimescaleFilterOptions
        {
            Speed = 0.975f,
            Pitch = 0.5f,
            Rate = 0.8f
        };
    }
    public static TimescaleFilterOptions Dance()
    {
        return new TimescaleFilterOptions
        {
            Speed = 1.25f,
            Pitch = 1.25f,
            Rate = 1.25f
        };
    }
    public static TimescaleFilterOptions China()
    {
        return new TimescaleFilterOptions
        {
            Speed = 0.75f,
            Pitch = 1.25f,
            Rate = 1.25f
        };
    }
    /*public static IEnumerable<IFilter> Vibrate()
    {
        return new IFilter[]
        {
            new VibratoFilter
            {
                Frequency = 4.0,
                Depth = 0.75
            },
            new TremoloFilter
            {
                Frequency = 4.0,
                Depth = 0.75
            }
        };
    }*/
    public static VibratoFilterOptions Vibrato()
    {
        return new VibratoFilterOptions
        {
            Frequency = 4.0f,
            Depth = 0.75f
        };
    }
    public static TremoloFilterOptions Tremolo()
    {
        return new TremoloFilterOptions
        {
            Frequency = 4.0f,
            Depth = 0.75f
        };
    }
}