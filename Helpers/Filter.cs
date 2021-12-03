using Victoria.Filters;

namespace KBot.Helpers
{
    public static class Filter
    {
        public static EqualizerBand[] BassBoost(bool enabled)
        {
            var equalizer = new EqualizerBand[]
            {
                new(0, enabled ? 0 : -0.075),
                new(1, enabled ? 0 : .125),
                new(2, enabled ? 0 : .125)
            };
            return equalizer;
        }
        
        public static TimescaleFilter NightCore(bool enabled)
        {
            var timescaleFilter = new TimescaleFilter()
            {
                Speed = enabled ? 0 : 1.1999999523162842,
                Pitch = enabled ? 0 : 1.2999999523163953,
                Rate = enabled ? 0: 1.0
            };
            return timescaleFilter;
        }
        
        public static RotationFilter EightD(bool enabled)
        {
            var rotationFilter = new RotationFilter()
            {
                Hertz = enabled ? 0 : 0.2999999523162842,
            };
            return rotationFilter;
        }
        
        public static TimescaleFilter VaporWave(bool enabled)
        {
            var timescaleFilter = new TimescaleFilter()
            {
                Speed = enabled ? 0 : 0.8500000238418579,
                Pitch = enabled ? 0 : 0.800000011920929,
                Rate = enabled ? 0 : 1.0
            };
            return timescaleFilter;
        }
        
        public static KarokeFilter Karaoke(bool enabled)
        {
            var karaokeFilter = new KarokeFilter()
            {
                FilterBand = enabled ? 0 : 0.5,
                FilterWidth = 5
            };
            return karaokeFilter;
        }
        
        public static TimescaleFilter Speed(bool enabled, double speed)
        {
            var timescaleFilter = new TimescaleFilter()
            {
                Speed = enabled ? speed : 1.0,
                Pitch = enabled ? 0 : 1.0,
                Rate = enabled ? 0 : 1.0
            };
            return timescaleFilter;
        }
        
        public static  TimescaleFilter Pitch(bool enabled, double pitch)
        {
            var timescaleFilter = new TimescaleFilter()
            {
                Speed = enabled ? 0 : 1.0,
                Pitch = enabled ? pitch : 1.0,
                Rate = enabled ? 0 : 1.0
            };
            return timescaleFilter;
        }
        
    }
}