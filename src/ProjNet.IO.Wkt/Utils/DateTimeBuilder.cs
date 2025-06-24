using System;
using System.Globalization;
using Pidgin;

namespace ProjNet.IO.Wkt.Utils
{
    internal sealed class DateTimeBuilder
    {
        internal int? Year { get; set; }
        internal int? Month { get; set; }
        internal int? Day { get; set; }

        internal uint? OrdinalDay { get; set; }

        internal int? Hour { get; set; }
        internal int? Minutes { get; set; }
        internal int? Seconds { get; set; }
        internal int? Milliseconds { get; set; }

        internal TimeSpan? LocalOffset { get; set; }

        internal DateTimeKind Kind { get; set; } = DateTimeKind.Unspecified;


        public DateTimeBuilder SetYear(int y)
        {
            Year = y;
            return this;
        }
        public DateTimeBuilder SetMonth(int m)
        {
            Month = m;
            return this;
        }
        public DateTimeBuilder SetDay(int d)
        {
            Day = d;
            return this;
        }

        public DateTimeBuilder SetHour(int h)
        {
            Hour = h;
            return this;
        }
        public DateTimeBuilder SetMinutes(int m)
        {
            Minutes = m;
            return this;
        }
        public DateTimeBuilder SetSeconds(int s)
        {
            Seconds = s;
            return this;
        }
        public DateTimeBuilder SetMilliseconds(int ms)
        {
            Milliseconds = ms;
            return this;
        }

        public DateTimeBuilder SetOrdinalDay(Maybe<uint> od)
        {
            OrdinalDay = od.HasValue ? od.GetValueOrDefault() : OrdinalDay;
            return this;
        }


        public DateTimeBuilder SetKind(DateTimeKind k)
        {
            Kind = k;
            return this;
        }

        public DateTimeBuilder SetLocalOffset(TimeSpan lo)
        {
            if (lo == TimeSpan.Zero)
            {
                Kind = DateTimeKind.Utc;
            }
            else
            {
                LocalOffset = lo;
                Kind = DateTimeKind.Local;
            }

            return this;
        }

        public DateTimeBuilder Merge(DateTimeBuilder other)
        {
            Day = Day ?? other.Day;
            Month = Month ?? other.Month;
            Year = Year ?? other.Year;

            Hour = Hour ?? other.Hour;
            Minutes = Minutes ?? other.Minutes;
            Seconds = Seconds ?? other.Seconds;
            Milliseconds = Milliseconds ?? other.Milliseconds;

            OrdinalDay = OrdinalDay ?? other.OrdinalDay;
            Kind = Kind == DateTimeKind.Unspecified ? other.Kind : Kind;
            LocalOffset = LocalOffset ?? other.LocalOffset;

            return this;
        }

        public DateTimeOffset ToDateTimeOffset()
        {
            var dt = new DateTime(Year.GetValueOrDefault(), Month.GetValueOrDefault(), Day.GetValueOrDefault(),
                Hour.GetValueOrDefault(), Minutes.GetValueOrDefault(), Seconds.GetValueOrDefault(),
                Milliseconds.GetValueOrDefault(),
                new GregorianCalendar(),
                Kind);

            return new DateTimeOffset(dt, LocalOffset.GetValueOrDefault());
        }
    }
}
