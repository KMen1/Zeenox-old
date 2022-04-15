using Lavalink4NET.Filters;
using Lavalink4NET.Player;

namespace KBot.Extensions;

public static class PlayerFilterMapExtensions
{
    public static string EnableBassBoost(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
            {
                new(0, 0.2f),
                new(1, 0.2f),
                new(2, 0.2f)
            }
        };
        return "Basszus Erősítés";
    }

    public static string EnablePop(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
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
                new(9, 0.6f)
            }
        };
        return "Pop";
    }

    public static string EnableSoft(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
            {
                new(8, -0.25f),
                new(9, -0.25f),
                new(10, -0.25f),
                new(11, -0.25f),
                new(12, -0.25f),
                new(13, -0.25f)
            }
        };
        return "Pop";
    }

    public static string EnableTreblebass(this PlayerFilterMap map)
    {
        map.Equalizer = new EqualizerFilterOptions
        {
            Bands = new EqualizerBand[]
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
                new(12, 0.55f)
            }
        };
        return "Pop";
    }

    public static string EnableNightcore(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.165f,
            Pitch = 1.125f,
            Rate = 1.05f
        };
        return "Nightcore";
    }

    public static string EnableEightd(this PlayerFilterMap map)
    {
        map.Rotation = new RotationFilterOptions
        {
            Frequency = 0.2f
        };
        return "8D";
    }

    public static string EnableVaporwave(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.0f,
            Pitch = 0.5f,
            Rate = 1.0f
        };
        return "Vaporwave";
        /*
new TremoloFilter()
{
    Depth = 0.3,
    Frequency = 14
}*/
    }

    public static string EnableDoubletime(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.0f,
            Pitch = 0.5f,
            Rate = 1.0f
        };
        return "Gyorsítás";
    }

    public static string EnableSlowmotion(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 0.5f,
            Pitch = 1.0f,
            Rate = 0.8f
        };
        return "Lassítás";
    }

    public static string EnableChipmunk(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.05f,
            Pitch = 1.35f,
            Rate = 1.25f
        };
        return "Alvin és a mókusok";
    }

    public static string EnableDarthvader(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 0.975f,
            Pitch = 0.5f,
            Rate = 0.8f
        };
        return "Darth Vader";
    }

    public static string EnableDance(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 1.25f,
            Pitch = 1.25f,
            Rate = 1.25f
        };
        return "Tánc";
    }

    public static string EnableChina(this PlayerFilterMap map)
    {
        map.Timescale = new TimescaleFilterOptions
        {
            Speed = 0.75f,
            Pitch = 1.25f,
            Rate = 1.25f
        };
        return "Kína";
    }

    public static string EnableVibrato(this PlayerFilterMap map)
    {
        map.Vibrato = new VibratoFilterOptions
        {
            Frequency = 4.0f,
            Depth = 0.75f
        };
        return "Vibrato";
    }

    public static string EnableTremolo(this PlayerFilterMap map)
    {
        map.Tremolo = new TremoloFilterOptions
        {
            Frequency = 4.0f,
            Depth = 0.75f
        };
        return "Tremolo";
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
}