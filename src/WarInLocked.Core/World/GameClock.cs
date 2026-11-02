using System;

namespace WarInLocked.Core.World
{
    /// <summary>
    /// The in-game clock (#56, Dyllan: "Starts at 8 am and runs 5 times faster than normal time."; sped up to 6x on 2 Nov so a game day is 4 real hours). Day 1 starts at 8:00 am.
    /// One real second is <see cref="SpeedFactor"/> game seconds. It only moves when <see cref="Update"/> is called, which
    /// <c>Character.Combat.Update(seconds)</c> does every frame, so it pauses whenever the Game stops updating (menus, dialogue, pause).
    /// <see cref="Advance"/> jumps it forward (resting, #55). Day/night rules for monsters (#57) read <see cref="IsNight"/>.
    /// </summary>
    public sealed class GameClock
    {
        /// <summary>The clock runs this many times faster than real time.</summary>
        public const double SpeedFactor = 6.0;
        public const int StartHour = 8;
        public const int MinutesPerDay = 24 * 60;

        /// <summary>Night begins at 7 pm (19:00) and lasts until 7 am; day is 7 am to 7 pm, 12 game hours each (2 real hours each at 6x), with no gaps (Dyllan, #57, 2 Nov).</summary>
        public const int NightStartHour = 19;
        /// <summary>Night ends, and day begins, at 7 am (2 Nov; it was 8 am). The single dawn constant: change it here (and <see cref="NightStartHour"/> to keep 12 hour halves) and sleep, <see cref="IsNight"/> and the HUD follow. The game still starts at 8 am, which is day.</summary>
        public const int NightEndHour = 7;
        /// <summary>The hour a night's sleep ends: 7 am, the start of the day (#55; follows <see cref="NightEndHour"/>).</summary>
        public const int WakeHour = NightEndHour;

        private double _totalMinutes = StartHour * 60;   // game minutes since midnight of day 1

        /// <summary>Raised whenever the time changes (every update that moves it, <see cref="Advance"/>, <see cref="SetTotalMinutes"/>). Poll <see cref="Version"/> instead if you prefer.</summary>
        public event Action Changed;
        /// <summary>Bumps every time the displayed minute may have changed; poll it to refresh a HUD.</summary>
        public int Version { get; private set; }

        /// <summary>Game minutes since midnight of day 1 (8:00 am on day 1 = 480), including the fraction. This is what a save stores.</summary>
        public double TotalMinutes => _totalMinutes;

        /// <summary>The day, starting at 1.</summary>
        public int Day => (int)Math.Floor(_totalMinutes / MinutesPerDay) + 1;
        /// <summary>Minutes since midnight today, 0 to 1439 (with the fraction).</summary>
        public double MinutesIntoDay => _totalMinutes - (Day - 1) * MinutesPerDay;
        /// <summary>The hour on a 24 hour clock, 0-23.</summary>
        public int Hour => (int)(MinutesIntoDay / 60);
        /// <summary>The minute, 0-59.</summary>
        public int Minute => (int)(MinutesIntoDay % 60);

        /// <summary>True from 7 pm (19:00) up to 7 am (06:59 is still night); <see cref="IsDay"/> is 7 am to 7 pm (Dyllan, #57).</summary>
        public bool IsNight => Hour >= NightStartHour || Hour < NightEndHour;
        public bool IsDay => !IsNight;

        /// <summary>The time as "8:00 am" (12 hour clock, midnight is "12:00 am", noon "12:00 pm"). PLACEHOLDER format.</summary>
        public string TimeText
        {
            get
            {
                int h12 = Hour % 12 == 0 ? 12 : Hour % 12;
                return h12 + ":" + Minute.ToString("00") + (Hour < 12 ? " am" : " pm");
            }
        }

        /// <summary>Day and time as "Day 1, 8:00 am". PLACEHOLDER format.</summary>
        public string Text => "Day " + Day + ", " + TimeText;

        /// <summary>Moves the clock by real seconds (times <see cref="SpeedFactor"/>). Called from <c>Combat.Update</c>.</summary>
        public void Update(double realSeconds)
        {
            if (realSeconds < 0) throw new ArgumentOutOfRangeException(nameof(realSeconds));
            if (realSeconds == 0) return;
            MoveBy(realSeconds * SpeedFactor / 60.0);
        }

        /// <summary>Jumps the clock forward by game hours (e.g. 3 for a rest). Fractions are fine.</summary>
        public void Advance(double hours)
        {
            if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
            MoveBy(hours * 60.0);
        }

        /// <summary>
        /// Skips to the next dawn (<see cref="WakeHour"/>, 7:00 am): later the same day when it is before 7 am (after midnight), otherwise the next day (#55).
        /// Returns the game hours that passed. Exactly 7:00 am goes to the next day's 7:00 am.
        /// </summary>
        public double AdvanceToMorning()
        {
            double target = (Day - 1) * MinutesPerDay + WakeHour * 60;
            if (_totalMinutes >= target) target += MinutesPerDay;
            double hours = (target - _totalMinutes) / 60.0;
            MoveBy(target - _totalMinutes);
            return hours;
        }

        /// <summary>Sets the clock from a save (game minutes since midnight of day 1).</summary>
        public void SetTotalMinutes(double totalMinutes)
        {
            if (totalMinutes < 0) throw new ArgumentOutOfRangeException(nameof(totalMinutes));
            _totalMinutes = totalMinutes;
            Version++;
            Changed?.Invoke();
        }

        private void MoveBy(double minutes)
        {
            if (minutes <= 0) return;
            _totalMinutes += minutes;
            Version++;
            Changed?.Invoke();
        }
    }
}
