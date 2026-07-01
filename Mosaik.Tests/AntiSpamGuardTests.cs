using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 41 Faz 3 §4.5 — katmanlı anti-spam, early-exit sıralı.
    public class AntiSpamGuardTests
    {
        [Fact]
        public void Evaluate_CleanSubmit_ReturnsOk()
        {
            Assert.Equal(AntiSpamResult.Ok, AntiSpamGuard.Evaluate(honeypotValue: "", elapsedSeconds: 10, rateLimited: false));
        }

        [Fact]
        public void Evaluate_HoneypotFilled_TrippedFirst()
        {
            // Honeypot dolu — timing/rate ne olursa olsun ilk katman yakalar.
            Assert.Equal(AntiSpamResult.HoneypotTripped, AntiSpamGuard.Evaluate("bot-value", elapsedSeconds: 0.1, rateLimited: true));
        }

        [Fact]
        public void Evaluate_TooFast_ReturnsTooFast()
        {
            Assert.Equal(AntiSpamResult.TooFast, AntiSpamGuard.Evaluate("", elapsedSeconds: 1.0, rateLimited: false));
        }

        [Fact]
        public void Evaluate_RateLimited_ReturnsRateLimited()
        {
            Assert.Equal(AntiSpamResult.RateLimited, AntiSpamGuard.Evaluate("", elapsedSeconds: 10, rateLimited: true));
        }

        [Fact]
        public void Evaluate_NegativeElapsed_SkipsTimingCheck()
        {
            // elapsed hesaplanamadıysa (-1) timing atlanır, diğer katmanlar çalışır.
            Assert.Equal(AntiSpamResult.Ok, AntiSpamGuard.Evaluate("", elapsedSeconds: -1, rateLimited: false));
        }

        [Fact]
        public void Evaluate_HoneypotBeatsTimingAndRate_LayerOrder()
        {
            // Katman sırası: honeypot > timing > rate. Hepsi tetikliyse honeypot döner.
            Assert.Equal(AntiSpamResult.HoneypotTripped, AntiSpamGuard.Evaluate("x", 0.5, true));
        }
    }
}
