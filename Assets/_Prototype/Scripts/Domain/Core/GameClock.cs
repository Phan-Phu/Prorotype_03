using System;
using Prototype.Domain;

namespace Prototype.Domain
{
    /// <summary>
    /// In-game time, plain C#. Drives the day/night loop:
    ///   Tick(deltaSeconds) -> accumulates real time -> fires OnHourPassed each in-game hour.
    ///   When the clock passes DayEndHour, ForceEndDay() fires OnDayEnded, rolls Day++, resets Hour.
    /// ForceEndDay is the single entry point for "sleep" (debug panel, boot arg, test all call it).
    /// </summary>
    public class GameClock
    {
    public int Day { get; internal set; } = 1;
        public int Hour { get; private set; } = BalanceConfig.DayStartHour;

        public event Action OnHourPassed;
        public event Action OnDayEnded;

        private int _msAccum;

        public void Tick(float deltaSeconds)
        {
            int hourMs = (int)(BalanceConfig.SecondsPerInGameHour * 1000f);
            _msAccum += (int)(deltaSeconds * 1000f);
            while (_msAccum >= hourMs)
            {
                _msAccum -= hourMs;
                AdvanceHour();
            }
        }

        /// <summary>Debug helper: advance exactly one in-game hour (does not skip the day boundary).</summary>
        public void SkipHour() => AdvanceHour();

        /// <summary>
        /// Bug bash B0 (2026-08-15): Hour runs past 24 internally (DayEndHour=26 => range is 6..25, by
        /// design — don't change AdvanceHour/DayEndHour for this), but any UI showing the clock must
        /// wrap it to a normal 0..23 24h-clock read (HUD used to print "24:00"/"25:00"). Pulled out as
        /// a static so it's testable without a MonoBehaviour/OnGUI.
        /// </summary>
        public static int WrapHour(int hour) => ((hour % 24) + 24) % 24;

        void AdvanceHour()
        {
            Hour++;
            if (Hour >= BalanceConfig.DayEndHour) { ForceEndDay(); return; }
            OnHourPassed?.Invoke();
        }

        public void ForceEndDay()
        {
            OnDayEnded?.Invoke();
            Day++;
            Hour = BalanceConfig.DayStartHour;
            _msAccum = 0;
            OnHourPassed?.Invoke();
        }
    }
}
