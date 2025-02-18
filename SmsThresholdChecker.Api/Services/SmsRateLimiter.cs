using SmsThresholdChecker.Api.Interfaces;
using SmsThresholdChecker.Api.Models;
using SmsThresholdChecker.Api.Models.DTOs;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace SmsThresholdChecker.Api.Services
{
    public class SmsRateLimiter : ISmsRateLimiter
    {
        private readonly ConcurrentDictionary<string, ISmsCounter> _phoneNumberCounters;
        private readonly ISmsCounter _accountCounter;
        private readonly int _perPhoneNumberLimit;
        private readonly int _accountLimit;
        private readonly Timer _cleanupTimer;
        private readonly ITimeProvider _timeProvider;
        private readonly ConcurrentDictionary<long, PhoneNumberCount> _lastHourPhoneNumberCount;

        public SmsRateLimiter(int perNumberLimit, int accountLimit, TimeSpan cleanupInterval, ITimeProvider timeProvider)
        {
            _perPhoneNumberLimit = perNumberLimit;
            _accountLimit = accountLimit;
            _timeProvider = timeProvider;
            _phoneNumberCounters = new ConcurrentDictionary<string, ISmsCounter>();
            _accountCounter = new SmsCounter(timeProvider);
            _cleanupTimer = new Timer(CleanupOldEntries, null, cleanupInterval, cleanupInterval);
            _lastHourPhoneNumberCount = new ConcurrentDictionary<long, PhoneNumberCount>();
        }

        public async Task<bool> CanSendSmsAsync(string phoneNumber)
        {
            var numberCounter = _phoneNumberCounters.GetOrAdd(phoneNumber, _ => new SmsCounter(_timeProvider));
            var numberTick = await numberCounter.TryIncrementAsync(_perPhoneNumberLimit);
            if (numberTick <= 0) return false;

            var accountTick = await _accountCounter.TryIncrementAsync(_accountLimit);
            if (accountTick <= 0)
            {
                await numberCounter.TryDecrementAsync();
                return false;
            }
            await UpdateLastHourPhoneNumberCountAsync(numberTick, phoneNumber, 1);
            return true;
        }
        private async Task UpdateLastHourPhoneNumberCountAsync(long tick, string phoneNumber, int delta)
        {
            _lastHourPhoneNumberCount.AddOrUpdate(tick,
                _ =>
                {
                    var phoneCount = new PhoneNumberCount();
                    phoneCount.PhoneNumberCounts.AddOrUpdate(phoneNumber, delta, (_, count) => count + delta);
                    return phoneCount;
                },
                (_, existing) =>
                {
                    existing.PhoneNumberCounts.AddOrUpdate(phoneNumber, delta, (_, count) => count + delta);
                    return existing;
                });
        }
        public async Task<IEnumerable<AccountCount>> GetAccountCountsAsync()
        {
            var result = _lastHourPhoneNumberCount.Select(kvp =>
                 new AccountCount
                 {
                     Time = new DateTime(kvp.Key * TimeSpan.TicksPerSecond, DateTimeKind.Utc),
                     Count = kvp.Value.PhoneNumberCounts.Values.Sum()
                 });

            return result;
        }
        public async Task<IEnumerable<PerSecondPhoneNumberCount>> GetPhoneNumberCountsAsync()
        {
            var result = _lastHourPhoneNumberCount.Select(kvp => new PerSecondPhoneNumberCount
            {
                Time = new DateTime(kvp.Key * TimeSpan.TicksPerSecond, DateTimeKind.Utc),
                PhoneNumbers = kvp.Value.PhoneNumberCounts.Select(phoneEntry => new PhoneNumberRecord
                {
                    PhoneNumber = phoneEntry.Key,
                    Count = phoneEntry.Value
                }).ToList()
            });
            return result;
        }
        private void CleanupOldEntries(object state)
        {
            var cutoff = _timeProvider.UtcNow.AddHours(-1);
            foreach (var entry in _phoneNumberCounters)
            {
                if (entry.Value.LastAccessed < cutoff)
                {
                    _phoneNumberCounters.TryRemove(entry.Key, out _);
                }
            }

            foreach (var entry in _lastHourPhoneNumberCount)
            {
                DateTime keyInDateTime = new DateTime(entry.Key * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                if (keyInDateTime < cutoff)
                {
                    _lastHourPhoneNumberCount.TryRemove(entry.Key, out _);
                }
            }
        }
    }
}
