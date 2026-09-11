using Prototype.Domain;
using Prototype.Application;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>Player-facing calendar/time panel matching the navigation design.</summary>
    public class HUD : MonoBehaviour
    {
        public IGameStateQuery StateQuery;
        public PlayerController Player;
        private Text _dateTimeText;

        void Awake()
        {
            _dateTimeText = transform.Find("DateTimePanel/DateTimeText")?.GetComponent<Text>();
        }

        void Update()
        {
            if (_dateTimeText == null) return;
            if (StateQuery == null) return;
            var result = StateQuery.Read().GetAwaiter().GetResult();
            if (result.IsSuccess)
                _dateTimeText.text = FormatDateTime(result.Value.Clock);
        }

        static string FormatDateTime(GameClockDto clock)
        {
            int displayHour = clock.DisplayHour;
            int weekday = ((clock.Day - 1) % 7) + 1;
            string dayName = weekday == 1 ? "Mon." : weekday == 2 ? "Tue." : weekday == 3 ? "Wed." :
                weekday == 4 ? "Thu." : weekday == 5 ? "Fri." : weekday == 6 ? "Sat." : "Sun.";
            string meridiem = displayHour < 12 ? "am" : "pm";
            int twelveHour = displayHour % 12;
            if (twelveHour == 0) twelveHour = 12;
            return $"Spring        Year 1\n{dayName} {clock.Day}        {twelveHour}:00 {meridiem}";
        }
    }
}
