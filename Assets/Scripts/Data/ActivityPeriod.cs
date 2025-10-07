using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class ActivityPeriod
    {
        [Serializable, InlineProperty]
        public class TimeOfDay
        {
            public int hour;
            public int minute;

            public TimeOfDay(TimeSpan ts)
            {
                hour = ts.Hours;
                minute = ts.Minutes;
            }

            [JsonConstructor]
            public TimeOfDay(int hour, int minute)
            {
                this.hour = hour;
                this.minute = minute;
            }

            public TimeSpan GetTimeSpanValue()
                => new TimeSpan(hour, minute, 0);
        }

        [Serializable]
        public class WeekAndTime
        {
            [SerializeField, LabelWidth(80)] public DayOfWeek DayOfWeek;
            [SerializeField, LabelWidth(86)] public TimeOfDay Time;
        }

        [TableColumnWidth(300)] public WeekAndTime start;
        [TableColumnWidth(300)] public WeekAndTime end;

        private IEnumerable<int> allWeekDays;

        private IEnumerable<int> AllWeekDays
            => allWeekDays ??= GetAllWeekDays();

        public TimeSpan GetTimeRest(DateTime now)
        {
            var daysRest = end.DayOfWeek - now.DayOfWeek;
            if (daysRest < 0) daysRest += 7;
            var timeRest = end.Time.GetTimeSpanValue() - now.TimeOfDay;
            return new TimeSpan(daysRest, 0, 0, 0).Add(timeRest);
        }

        public bool IsActive(DateTime date)
        {
            if (!AllWeekDays.Contains((int)date.DayOfWeek)) return false;

            var isWithinTimeLimit = true;
            if (date.DayOfWeek == start.DayOfWeek)
                isWithinTimeLimit = date.TimeOfDay >= start.Time.GetTimeSpanValue();
            if (date.DayOfWeek == end.DayOfWeek)
                isWithinTimeLimit = isWithinTimeLimit && date.TimeOfDay < end.Time.GetTimeSpanValue();

            return isWithinTimeLimit;
        }

        public string GetJSONString()
        {
            var startDay = start.DayOfWeek.ToString().Substring(0, 3);
            var endDay = end.DayOfWeek.ToString().Substring(0, 3);
            return
                $"{startDay} {start.Time.hour:D2}:{start.Time.minute:D2} - {endDay} {end.Time.hour:D2}:{end.Time.minute:D2}";
        }

        private IEnumerable<int> GetAllWeekDays()
        {
            var d = (int)start.DayOfWeek;
            var e = (int)end.DayOfWeek;

            while (d != e)
            {
                yield return d;


                d = (d + 1) % 7;
            }

            yield return d;
        }
    }

    public static class ActivityPeriodHelper
    {
        public static DateTime? GetClosestDate(List<ActivityPeriod.WeekAndTime> dates, DateTime nowUTC)
        {
            DateTime? result = null;
            foreach (var wt in dates)
            {
                var date = wt.GetClosestDate(nowUTC);
                if (date < nowUTC)
                    continue;

                if (result == null || result > date)
                    result = date;
            }

            return result;
        }

        public static DateTime GetClosestDate(this ActivityPeriod.WeekAndTime to, DateTime nowUTC)
        {
            var days = to.DayOfWeek - nowUTC.DayOfWeek;
            var timeDiff = to.Time.GetTimeSpanValue() - nowUTC.TimeOfDay;
            return nowUTC.AddDays(days < 0 ? days + 7 : days).Add(timeDiff);
        }
    }
}