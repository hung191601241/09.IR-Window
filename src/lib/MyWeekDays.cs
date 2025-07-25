using System;
using System.Collections.Generic;

namespace VisionInspection
{
    class MyWeekDays
    {
        private const int SECONDS_PER_DAY = 86400;

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public static List<MyWeekDays> GetWeekDays(DateTime from, DateTime to) {
            var ret = new List<MyWeekDays>();
            // Make sure from.Time is 00:00:00 and to.TIme is 23:59:59
            from = from.Date;
            to = to.Date.AddSeconds(SECONDS_PER_DAY - 1);
            if (from > to) {
                return ret;
            }

            var w1 = MyWeekDays.GetWeekFromStartDay(from);
            ret.Add(w1);

            var dt = GetNextMonday(from);
            while (dt <= to) {
                var w = GetWeekContainDay(dt);
                if (w.End > to) {
                    w.End = to;
                }
                ret.Add(w);
                dt = dt.AddDays(7);
            }
            return ret;
        }

        public static MyWeekDays GetWeekFromStartDay(DateTime start) {
            var ret = new MyWeekDays();

            ret.Start = start;
            var dif = start.DayOfWeek - DayOfWeek.Sunday;
            ret.End = start.AddDays((7 - dif) % 7).AddSeconds(SECONDS_PER_DAY - 1);
            return ret;
        }

        public static MyWeekDays GetWeekContainDay(DateTime dt) {
            var ret = new MyWeekDays();

            ret.Start = dt.AddDays(DayOfWeek.Monday - dt.DayOfWeek);
            ret.End = ret.Start.AddDays(6).AddSeconds(SECONDS_PER_DAY - 1);
            return ret;
        }

        private static DateTime GetNextMonday(DateTime dt) {
            if (dt.DayOfWeek == DayOfWeek.Sunday) {
                return dt.AddDays(1);
            }
            var dif = dt.DayOfWeek - DayOfWeek.Monday;
            return dt.AddDays(7 - dif);
        }
    }
}
