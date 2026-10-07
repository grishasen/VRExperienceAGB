using System;

namespace VRExperienceAGB.Application
{
    /// <summary>Fixed mean-equatorial J2000 scene, latitude 55 N, local sidereal time 6 h.
    /// Returns east/up/south components. Unity maps east to -X, south to +Z. No clock, location service or camera dependency.</summary>
    public static class WinterSkyCoordinates
    {
        public static (double east, double up, double south) Direction(double raHours, double declinationDegrees)
        {
            double radians = Math.PI / 180, latitude = 55 * radians, dec = declinationDegrees * radians;
            double hourAngle = (6 - raHours) * 15 * radians;
            return (-Math.Cos(dec) * Math.Sin(hourAngle),
                Math.Sin(latitude) * Math.Sin(dec) + Math.Cos(latitude) * Math.Cos(dec) * Math.Cos(hourAngle),
                Math.Sin(latitude) * Math.Cos(dec) * Math.Cos(hourAngle) - Math.Cos(latitude) * Math.Sin(dec));
        }
    }
    /// <summary>Intervals above 20 seconds guarantee at most three starts in any 60-second window.
    /// No catch-up bursts after stalls or pauses.</summary>
    public sealed class MeteorSchedule
    {
        private readonly Random random = new Random(550106);
        public double Remaining { get; private set; } = 26;
        public int Count { get; private set; }
        public bool Advance(double seconds, bool allowed)
        {
            if (!allowed || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return false;
            Remaining -= seconds;
            if (Remaining > 0) return false;
            Remaining = 22 + 12 * random.NextDouble(); Count++; return true;
        }
    }
}
