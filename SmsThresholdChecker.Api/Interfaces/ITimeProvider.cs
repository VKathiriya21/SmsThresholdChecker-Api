namespace SmsThresholdChecker.Api.Interfaces
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        Task<long> GetCurrentWindowTicksAsync();
    }
}