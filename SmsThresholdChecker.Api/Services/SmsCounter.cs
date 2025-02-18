using SmsThresholdChecker.Api.Interfaces;

namespace SmsThresholdChecker.Api.Services
{
    public class SmsCounter : ISmsCounter
    {
        private readonly ITimeProvider _timeProvider;
        private long _windowTicks;
        private int _countPerSecond;
        private DateTime _lastAccessed;
        public DateTime LastAccessed => _lastAccessed;
        public SmsCounter(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }
        public async Task<long> TryIncrementAsync(int limit)
        {
            long currentWindowTicks = await _timeProvider.GetCurrentWindowTicksAsync();
            long oldWindow;
            int oldCount;

            do
            {
                oldWindow = _windowTicks;
                oldCount = _countPerSecond;

                if (currentWindowTicks > oldWindow)
                {
                    if (Interlocked.CompareExchange(ref _windowTicks, currentWindowTicks, oldWindow) == oldWindow)
                    {
                        Interlocked.Exchange(ref _countPerSecond, 0);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (oldCount >= limit)
                {
                    return 0;
                }

                int newCount = oldCount + 1;
                if (Interlocked.CompareExchange(ref _countPerSecond, newCount, oldCount) == oldCount)
                {
                    _lastAccessed = _timeProvider.UtcNow;
                    return _windowTicks;
                }
            } while (true);
        }

        public async Task<long> TryDecrementAsync()
        {
            long currentWindowTicks = await _timeProvider.GetCurrentWindowTicksAsync();
            long oldWindow;
            int oldCount;

            do
            {
                oldWindow = _windowTicks;
                oldCount = _countPerSecond;

                if (currentWindowTicks != oldWindow)
                {
                    return 0;
                }

                int newCount = oldCount - 1;
                if (newCount < 0)
                {
                    return 0;
                }

                if (Interlocked.CompareExchange(ref _countPerSecond, newCount, oldCount) == oldCount)
                {
                    _lastAccessed = _timeProvider.UtcNow;
                    return _windowTicks;
                }
            } while (true);
        }
    }
}
