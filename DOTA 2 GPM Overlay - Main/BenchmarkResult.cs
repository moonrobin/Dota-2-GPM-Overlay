using System.Collections.Generic;
using System.Linq;

// ReSharper disable InconsistentNaming

namespace DOTA_2_GPM_Overlay___Main
{
    public class BenchmarkResult
    {
        public int hero_id { get; set; }
        public Result result { get; set; }

        public int GetGPMPercentile(int GPM)
        {
            var zeroPercentile = new PercentileValue {percentile = 0, value = 0};

            var lowerBound = result.gold_per_min.Where(x => x.value <= GPM).OrderBy(x => x.value).FirstOrDefault() ??
                             zeroPercentile;
            var upperBound = result.gold_per_min.Where(x => x.value >= GPM).OrderBy(x => x.value).FirstOrDefault() ??
                             zeroPercentile;

            var interpolatedPercentile = Utilities.LinearInterpolation(lowerBound.value, lowerBound.percentile,
                upperBound.value,
                upperBound.percentile, GPM)*100;

            return interpolatedPercentile > 100 ? 100 : (int)interpolatedPercentile;
        }

        public int GetXPMPercentile(int XPM)
        {
            var zeroPercentile = new PercentileValue {percentile = 0, value = 0};

            var lowerBound = result.xp_per_min.Where(x => x.value <= XPM).OrderBy(x => x.value).FirstOrDefault() ??
                             zeroPercentile;
            var upperBound = result.xp_per_min.Where(x => x.value >= XPM).OrderBy(x => x.value).FirstOrDefault() ??
                             zeroPercentile;

            var interpolatedPercentile = Utilities.LinearInterpolation(lowerBound.value, lowerBound.percentile,
                upperBound.value,
                upperBound.percentile, XPM)*100;

            return interpolatedPercentile>100 ? 100 : (int) interpolatedPercentile;
        }
    }

    public class Result
    {
        public List<PercentileValue> gold_per_min { get; set; }
        public List<PercentileValue> xp_per_min { get; set; }
    }

    public class PercentileValue
    {
        public double percentile { get; set; }
        public int value { get; set; }
    }
}
