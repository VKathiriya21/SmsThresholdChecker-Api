using SmsThresholdChecker.Api.Interfaces;

namespace SmsThresholdChecker.Api.Services
{
    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public Task<long> GetCurrentWindowTicksAsync() =>
            Task.FromResult(UtcNow.Ticks / TimeSpan.TicksPerSecond);
    }
}