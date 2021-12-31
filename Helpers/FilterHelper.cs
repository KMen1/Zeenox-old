using Victoria.Filters;

namespace KBot.Helpers;

public static class FilterHelper
{
    public static EqualizerBand[] BassBoost()
    {
        var equalizer = new EqualizerBand[]
        {
            new(0, 0.6),
            new(1, 0.67),
            new(2, 0.67)
        };
        return equalizer;
    }

    public static TimescaleFilter NightCore()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.1999999523162842,
            Pitch = 1.2999999523163953,
            Rate = 1.0
        };
        return timescaleFilter;
    }

    public static RotationFilter EightD()
    {
        var rotationFilter = new RotationFilter
        {
            Hertz = 0.3999999523162842
        };
        return rotationFilter;
    }

    public static TimescaleFilter VaporWave()
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 0.8500000238418579,
            Pitch = 0.800000011920929,
            Rate = 1.0
        };
        return timescaleFilter;
    }

    public static TimescaleFilter Speed(double speed)
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = speed,
            Pitch = 1.0,
            Rate = 1.0
        };
        return timescaleFilter;
    }

    public static TimescaleFilter Pitch(double pitch)
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.0,
            Pitch = pitch,
            Rate = 1.0
        };
        return timescaleFilter;
    }
    
    public static IFilter[] DefaultFilters()
    {
        var filters = new IFilter[]
        {
            new TimescaleFilter()
            {
                Pitch = 1.0,
                Speed = 1.0,
                Rate = 1.0
            },
            new RotationFilter()
            {
                Hertz = 0
            }
        };
        return filters;
    }
}