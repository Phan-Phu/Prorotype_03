using Prototype.Domain;
using Prototype.Application;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>Player-facing calendar/time panel matching the navigation design.</summary>
    public class HUD : MonoBehaviour
    {
        public GameState State;
        public PlayerController Player;
        private Text _dateTimeText;

        void Awake()
        {
            _dateTimeText = transform.Find("DateTimePanel/DateTimeText")?.GetComponent<Text>();
        }

        void Update()
        {
            if (State == null || _dateTimeText == null) return;
            _dateTimeText.text = FormatDateTime(State);
        }

        void OnGUI()
        {
            if (State == null) return;
            if (_dateTimeText != null) return;
            var s = State;
            // Bug bash B0 (2026-08-15): Clock.Hour counts past 24 internally (DayEndHour=26, so it
            // runs 6..25 before ForceEndDay rolls it back to 6 — that raw range is what GameClockTests
            // asserts on, don't change it). Display must wrap at 24 or the HUD reads "24:00"/"25:00"
            // instead of "00:00"/"01:00" for the last two hours before sleep — display-only fix.
            int displayHour = GameClock.WrapHour(s.Clock.Hour);
            var hudRect = ResponsiveUILayout.HudRect(Screen.width, Screen.height);
            var oldMatrix = GUI.matrix;
            var oldContentColor = GUI.contentColor;
            GUI.contentColor = new Color32(42, 30, 20, 255);
            float scale = ResponsiveUILayout.HudScale(Screen.width, Screen.height);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), hudRect.position);
            int weekday = ((s.Clock.Day - 1) % 7) + 1;
            string dayName = weekday == 1 ? "Mon." : weekday == 2 ? "Tue." : weekday == 3 ? "Wed." :
                weekday == 4 ? "Thu." : weekday == 5 ? "Fri." : weekday == 6 ? "Sat." : "Sun.";
            string meridiem = displayHour < 12 ? "am" : "pm";
            int twelveHour = displayHour % 12;
            if (twelveHour == 0) twelveHour = 12;
            GUI.Box(new Rect(hudRect.x, hudRect.y, ResponsiveUILayout.HudNativeW, ResponsiveUILayout.HudNativeH),
                $"Spring        Year 1\n{dayName} {s.Clock.Day}        {twelveHour}:00 {meridiem}");
            GUI.matrix = oldMatrix;
            GUI.contentColor = oldContentColor;
        }

        static string FormatDateTime(GameState s)
        {
            int displayHour = GameClock.WrapHour(s.Clock.Hour);
            int weekday = ((s.Clock.Day - 1) % 7) + 1;
            string dayName = weekday == 1 ? "Mon." : weekday == 2 ? "Tue." : weekday == 3 ? "Wed." :
                weekday == 4 ? "Thu." : weekday == 5 ? "Fri." : weekday == 6 ? "Sat." : "Sun.";
            string meridiem = displayHour < 12 ? "am" : "pm";
            int twelveHour = displayHour % 12;
            if (twelveHour == 0) twelveHour = 12;
            return $"Spring        Year 1\n{dayName} {s.Clock.Day}        {twelveHour}:00 {meridiem}";
        }
    }
}
