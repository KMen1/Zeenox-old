using System.Collections.Generic;
using Victoria.Filters;

namespace KBot.Modules.Audio.Helpers;

public static class Filters
{
    public static EqualizerBand[] BassBoost()
    {
        return new EqualizerBand[]
        {
            new(0, 0.2),
            new(1, 0.2),
            new(2, 0.2)
        };
    }
    public static EqualizerBand[] Pop()
    {
        return new EqualizerBand[]
        {
            new(0, 0.65),
            new(1, 0.45),
            new(2, -0.25),
            new(3, -0.25),
            new(4, -0.25),
            new(5, 0.45),
            new(6, 0.55),
            new(7, 0.6),
            new(8, 0.6),
            new(9, 0.6),
        };
    }
    public static EqualizerBand[] Soft()
    {
        return new EqualizerBand[]
        {
            new(8, -0.25),
            new(9, -0.25),
            new(10, -0.25),
            new(11, -0.25),
            new(12, -0.25),
            new(13, -0.25)
        };
    }
    public static EqualizerBand[] TrebleBass()
    {
        return new EqualizerBand[]
        {
            new(0, 0.6),
            new(1, 0.67),
            new(2, 0.67),
            new(4, -0.2),
            new(5, 0.15),
            new(6, -0.25),
            new(7, 0.23),
            new(8, 0.35),
            new(9, 0.45),
            new(10, 0.55),
            new(11, 0.6),
            new(12, 0.55),
        };
    }

    public static IFilter NightCore()
    {
        return new TimescaleFilter
        {
            Speed = 1.165,
            Pitch = 1.125,
            Rate = 1.05
        };
    }

    public static IFilter EightD()
    {
        return new RotationFilter
        {
            Hertz = 0.2
        };
    }
    public static IFilter VaporWave()
    {
        return new TimescaleFilter
        {
            Speed = 1.0,
            Pitch = 0.5,
            Rate = 1.0
        };
        /*
        new TremoloFilter()
        {
            Depth = 0.3,
            Frequency = 14
        }*/

    }
    public static IFilter Doubletime()
    {
        return new TimescaleFilter
        {
            Speed = 1.165,
            Pitch = 1.0,
            Rate = 1.0
        };
    }
    public static IFilter Slowmotion()
    {
        return new TimescaleFilter
        {
            Speed = 0.5,
            Pitch = 1.0,
            Rate = 0.8
        };
    }
    public static IFilter Chipmunk()
    {
        return new TimescaleFilter
        {
            Speed = 1.05,
            Pitch = 1.35,
            Rate = 1.25
        };
    }
    public static IFilter Darthvader()
    {
        return new TimescaleFilter
        {
            Speed = 0.975,
            Pitch = 0.5,
            Rate = 0.8
        };
    }
    public static IFilter Dance()
    {
        return new TimescaleFilter
        {
            Speed = 1.25,
            Pitch = 1.25,
            Rate = 1.25
        };
    }
    public static IFilter China()
    {
        return new TimescaleFilter
        {
            Speed = 0.75,
            Pitch = 1.25,
            Rate = 1.25
        };
    }
    public static IEnumerable<IFilter> Vibrate()
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
    }
    public static IFilter Vibrato()
    {
        return new VibratoFilter
        {
            Frequency = 4.0,
            Depth = 0.75
        };
    }
    public static IFilter Tremolo()
    {
        return new TremoloFilter
        {
            Frequency = 4.0,
            Depth = 0.75
        };
    }
}