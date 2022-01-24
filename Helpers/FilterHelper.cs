using System.Collections.Generic;
using Victoria.Filters;

namespace KBot.Helpers;

public static class FilterHelper
{
    public static EqualizerBand[] BassBoost()
    {
        var equalizer = new EqualizerBand[]
        {
            new(0, 0.2),
            new(1, 0.2),
            new(2, 0.2)
        };
        return equalizer;
    }
    public static EqualizerBand[] Pop()
    {
        var equalizer = new EqualizerBand[]
        {
            new(0, 0.65),
            new(1, 0.45),
            new(2, -0.45),
            new(3, -0.65),
            new(4, -0.35),
            new(5, 0.45),
            new(6, 0.55),
            new(7, 0.6),
            new(8, 0.6),
            new(9, 0.6),
            //new(10, 0),
            //new(11, 0),
            //new(12, 0),
            //new(13, 0)
        };
        return equalizer;
    }
    public static EqualizerBand[] Soft()
    {
        var equalizer = new EqualizerBand[]
        {
            /*new(0, 0),
            new(1, 0),
            new(2, 0),
            new(3, 0),
            new(4, 0),
            new(5, 0),
            new(6, 0),
            new(7, 0),*/
            new(8, -0.25),
            new(9, -0.25),
            new(10, -0.25),
            new(11, -0.25),
            new(12, -0.25),
            new(13, -0.25)
        };
        return equalizer;
    }
    public static EqualizerBand[] TrebleBass()
    {
        var equalizer = new EqualizerBand[]
        {
            new(0, 0.6),
            new(1, 0.67),
            new(2, 0.67),
            //new(3, 0),
            new(4, -0.5),
            new(5, 0.15),
            new(6, -0.45),
            new(7, 0.23),
            new(8, 0.35),
            new(9, 0.45),
            new(10, 0.55),
            new(11, 0.6),
            new(12, 0.55),
            //new(13, 0)
        };
        return equalizer;
    }

    public static TimescaleFilter NightCore()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.165,
            Pitch = 1.125,
            Rate = 1.05
        };
        return timescaleFilter;
    }

    public static RotationFilter EightD()
    {
        var rotationFilter = new RotationFilter
        {
            Hertz = 0.2
        };
        return rotationFilter;
    }
    public static (IEnumerable<IFilter>, EqualizerBand[]) VaporWave()
    {
        var eq = new EqualizerBand[]
        {
            new(0, 0.2),
            new(1, 0.2)
        };
        var filters = new IFilter[]
        {
            new TimescaleFilter
            {
                Speed = 1.0,
                Pitch = 0.5,
                Rate = 1.0
            },
            new TremoloFilter()
            {
                Depth = 0.3,
                Frequency = 14
            }
        };
        return (filters, eq);
    }
    public static TimescaleFilter Doubletime()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.165,
            Pitch = 1.0,
            Rate = 1.0
        };
        return timescaleFilter;
    }
    public static TimescaleFilter Slowmotion()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 0.5,
            Pitch = 1.0,
            Rate = 0.8
        };
        return timescaleFilter;
    }
    public static TimescaleFilter Chipmunk()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.05,
            Pitch = 1.35,
            Rate = 1.25
        };
        return timescaleFilter;
    }
    public static TimescaleFilter Darthvader()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 0.975,
            Pitch = 0.5,
            Rate = 0.8
        };
        return timescaleFilter;
    }
    public static TimescaleFilter Dance()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.25,
            Pitch = 1.25,
            Rate = 1.25
        };
        return timescaleFilter;
    }
    public static TimescaleFilter China()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 0.75,
            Pitch = 1.25,
            Rate = 1.25
        };
        return timescaleFilter;
    }
    public static IEnumerable<IFilter> Vibrate()
    {
        var filters = new IFilter[]
        {
            new VibratoFilter()
            {
                Frequency = 4.0,
                Depth = 0.75
            },
            new TremoloFilter()
            {
                Frequency = 4.0,
                Depth = 0.75
            }
        };
        return filters;
    }
    public static VibratoFilter Vibrato()
    {
        var vibrato = new VibratoFilter()
        {
            Frequency = 4.0,
            Depth = 0.75
        };
        return vibrato;
    }
    public static TremoloFilter Tremolo()
    {
        var tremolo = new TremoloFilter()
        {
            Frequency = 4.0,
            Depth = 0.75
        };
        return tremolo;
    }
}