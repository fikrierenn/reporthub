using System;

namespace Mosaik.Core.Domain
{
    // Türkiye yerel takvimine göre "bugün" hesabı. Sunucu UTC çalıştığında
    // 22:00 UTC sonrasında "Today" Turkey'de ertesi gün olur — vade/iş günü
    // karşılaştırmaları bunu yanlış yapar. Tüm reminder/compliance/calendar
    // karşılaştırmaları IBusinessClock üzerinden geçer.
    //
    // Not: timestamp alanları (CreatedAt, ReminderSentAt vb.) UTC olarak
    // saklanmaya devam eder — bu helper sadece tarih kıyaslamaları için.
    public interface IBusinessClock
    {
        DateTime NowLocal { get; }
        DateOnly Today { get; }
        TimeZoneInfo TimeZone { get; }
    }

    public sealed class SystemBusinessClock : IBusinessClock
    {
        private readonly TimeZoneInfo _tz;

        public SystemBusinessClock(TimeZoneInfo timeZone)
        {
            _tz = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
        }

        public DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
        public DateOnly Today => DateOnly.FromDateTime(NowLocal);
        public TimeZoneInfo TimeZone => _tz;

        // Resolver — Program.cs startup'ta çağrılır. Windows/Linux farkı:
        // Windows "Turkey Standard Time", IANA "Europe/Istanbul". İkisi de bulunamazsa
        // exception fırlatır — silent UTC fallback yapma (configuration hatasını gizler).
        public static TimeZoneInfo ResolveTurkeyTimeZone()
        {
            foreach (var id in new[] { "Turkey Standard Time", "Europe/Istanbul" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { /* try next */ }
            }
            throw new InvalidOperationException(
                "Türkiye saat dilimi bulunamadı (denenen: 'Turkey Standard Time', 'Europe/Istanbul'). " +
                "Sunucuda tzdata eksik olabilir — yapılandırmayı kontrol edin.");
        }
    }
}
