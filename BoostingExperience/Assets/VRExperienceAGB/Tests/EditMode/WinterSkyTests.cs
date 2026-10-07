using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Tests
{
    public class WinterSkyTests
    {
        [Test]
        public void MeridianAndPoleHaveExpectedAltitudeAt55North()
        {
            var pole = WinterSkyCoordinates.Direction(0,90);
            Assert.That(Math.Asin(pole.up)*180/Math.PI, Is.EqualTo(55).Within(1e-10));
            var equator = WinterSkyCoordinates.Direction(6,0);
            Assert.That(Math.Asin(equator.up)*180/Math.PI, Is.EqualTo(35).Within(1e-10));
            Assert.That(equator.south, Is.GreaterThan(0)); Assert.That(equator.east, Is.Zero.Within(1e-12));
        }
        [TestCase(6.752481,-16.716116,17,19)]
        [TestCase(5.919529,7.407063,42,43)]
        [TestCase(3.791410,24.105137,49,51)]
        public void WinterLandmarksHavePlausibleAltitude(double ra,double dec,double low,double high)
        { var d=WinterSkyCoordinates.Direction(ra,dec); Assert.That(Math.Asin(d.up)*180/Math.PI,Is.InRange(low,high)); }
        [Test]
        public void MeteorsNeverExceedThreeInAnyRollingMinute()
        {
            var schedule = new MeteorSchedule(); var events = new List<double>();
            for (int i=0;i<36000;i++) if(schedule.Advance(.1,true)) events.Add(i*.1);
            Assert.That(events.Count, Is.InRange(110,165));
            foreach(var t in events) Assert.That(events.Count(e=>e>=t&&e<t+60),Is.LessThanOrEqualTo(3));
            int count=schedule.Count; schedule.Advance(500,false); Assert.That(schedule.Count,Is.EqualTo(count));
            schedule.Advance(500,true); Assert.That(schedule.Count,Is.EqualTo(count+1));
            Assert.That(schedule.Advance(.01,true),Is.False);
        }
    }
}
