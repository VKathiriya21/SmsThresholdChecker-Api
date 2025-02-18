using SmsThresholdChecker.Api.Services;

namespace SmsThresholdChecker.Api.Interfaces
{
    public interface ISmsCounter
    {
        DateTime LastAccessed { get; }
        Task<long> TryIncrementAsync(int limit);
        Task<long> TryDecrementAsync();
    }

}