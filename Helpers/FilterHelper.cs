using Victoria.Filters;

namespace KBot.Helpers;

public static class FilterHelper
{
    public static EqualizerBand[] BassBoost(bool enabled)
    {
        var equalizer = new EqualizerBand[]
        {
            new(0, enabled ? 0.6 : 0.01),
            new(1, enabled ? 0.67 : 0.01),
            new(2, enabled ? 0.67 : 0.01)
        };
        return equalizer;
    }

    public static TimescaleFilter NightCore(bool enabled)
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = enabled ? 1.1999999523162842 : 1.0,
            Pitch = enabled ? 1.2999999523163953 : 1.0,
            Rate = 1.0
        };
        return timescaleFilter;
    }

    public static RotationFilter EightD(bool enabled)
    {
        var rotationFilter = new RotationFilter
        {
            Hertz = enabled ? 0.3999999523162842 : 0
        };
        return rotationFilter;
    }

    public static TimescaleFilter VaporWave(bool enabled)
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = enabled ? 0.8500000238418579 : 1.0,
            Pitch = enabled ? 0.800000011920929 : 1.0,
            Rate = 1.0
        };
        return timescaleFilter;
    }

    public static TimescaleFilter Speed(bool enabled, double speed)
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = enabled ? speed : 1.0,
            Pitch = 1.0,
            Rate = 1.0
        };
        return timescaleFilter;
    }

    public static TimescaleFilter Pitch(bool enabled, double pitch)
    {
        var timescaleFilter = new TimescaleFilter
        {
            Speed = 1.0,
            Pitch = enabled ? pitch : 1.0,
            Rate = 1.0
        };
        return timescaleFilter;
    }
}