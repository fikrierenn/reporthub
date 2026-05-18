using System;

namespace Mosaik.Core.Domain
{
    // Türkiye yerel takvimine göre "bugün" hesabı. Sunucu UTC çalıştığında
    // 22:00 UTC sonrasında "Today" Turkey'de zaten yarın olur — vade/iş günü
    // karşılaştırmaları bunu yanlış yapar. Tüm reminder/compliance/calendar
    // karşılaştırmaları bu helper üzerinden geçer.
    //
    // Not: timestamp alanları (CreatedAt, ReminderSentAt vb.) UTC olarak
    // saklanmaya devam eder — bu helper sadece tarih kıyaslamaları için.
    public static class BusinessClock
    {
        // Windows/Linux farkı: Windows "Turkey Standard Time", IANA "Europe/Istanbul".
        private static readonly TimeZoneInfo TurkeyTz = ResolveTurkeyTz();

        private static TimeZoneInfo ResolveTurkeyTz()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
            catch (TimeZoneNotFoundException) { }
            try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
            catch (TimeZoneNotFoundException) { }
            return TimeZoneInfo.Utc; // son çare — log eden yer geçici sapma görür
        }

        public static DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz);

        public static DateOnly Today => DateOnly.FromDateTime(NowLocal);
    }
}
