using SmsThresholdChecker.Api.Interfaces;
using SmsThresholdChecker.Api.Services;

namespace SmsThresholdChecker.Api.Tests
{

    public class RateLimiterTests
    {
        private class TestTimeProvider : ITimeProvider
        {
            public DateTime UtcNow { get; set; }
            public Task<long> GetCurrentWindowTicksAsync() =>
                Task.FromResult(UtcNow.Ticks / TimeSpan.TicksPerSecond);
        }

        [Fact]
        public async Task CanSendSmsAsync_UnderLimits_ReturnsTrue()
        {
            var fixedTime = new DateTime(2024, 1, 1, 12, 0, 0);
            var timeProvider = new TestTimeProvider { UtcNow = fixedTime };
            var rateLimiter = new SmsRateLimiter(1, 1, TimeSpan.FromHours(1), timeProvider);
            Assert.True(await rateLimiter.CanSendSmsAsync("1111111111"));
        }

        [Fact]
        public async Task CanSendSmsAsync_ExceedPerNumberLimit_ReturnsFalse()
        {
            var fixedTime = new DateTime(2024, 1, 1, 12, 0, 0);
            var timeProvider = new TestTimeProvider { UtcNow = fixedTime };
            var rateLimiter = new SmsRateLimiter(1, 100, TimeSpan.FromHours(1), timeProvider);
            Assert.True(await rateLimiter.CanSendSmsAsync("1111111111"));
            Assert.False(await rateLimiter.CanSendSmsAsync("1111111111"));
        }

        [Fact]
        public async Task CanSendSmsAsync_AfterWindowReset_AllowsAgain()
        {
            var fixedTime = new DateTime(2024, 1, 1, 12, 0, 0);
            var timeProvider = new TestTimeProvider { UtcNow = fixedTime };
            var rateLimiter = new SmsRateLimiter(1, 100, TimeSpan.FromHours(1), timeProvider);
            Assert.True(await rateLimiter.CanSendSmsAsync("1111111111"));
            Assert.False(await rateLimiter.CanSendSmsAsync("1111111111"));

            timeProvider.UtcNow = timeProvider.UtcNow.AddSeconds(1);
            Assert.True(await rateLimiter.CanSendSmsAsync("1111111111"));
        }

        [Fact]
        public async Task CanSendSmsAsync_ExceedAccountLimit_ReturnsFalse()
        {
            var fixedTime = new DateTime(2024, 1, 1, 12, 0, 0);
            var timeProvider = new TestTimeProvider { UtcNow = fixedTime };
            var rateLimiter = new SmsRateLimiter(100, 1, TimeSpan.FromHours(1), timeProvider);
            Assert.True(await rateLimiter.CanSendSmsAsync("1111111111"));
            Assert.False(await rateLimiter.CanSendSmsAsync("2222222222"));
        }

        [Fact]
        public async Task Multithread_BothLimitsEnforcedSimultaneously()
        {
            int phoneLimit = 99;
            int accountLimit = 450;
            var phoneNumbers = new[] { "1111111111", "2222222222", "3333333333", "4444444444", "5555555555" };
            int requestsPerPhone = 100;

            var fixedTime = new DateTime(2024, 1, 1, 12, 0, 0);
            var timeProvider = new TestTimeProvider { UtcNow = fixedTime };
            var rateLimiter = new SmsRateLimiter(
                perNumberLimit: phoneLimit,
                accountLimit: accountLimit,
                cleanupInterval: TimeSpan.FromHours(1),
                timeProvider: timeProvider);

            var tasks = new List<Task<(string phone, bool allowed)>>();

            foreach (var phone in phoneNumbers)
            {
                for (int i = 0; i < requestsPerPhone; i++)
                {
                    tasks.Add(Task.Run(async () => (phone, allowed: await rateLimiter.CanSendSmsAsync(phone))));
                }
            }

            var results = await Task.WhenAll(tasks);

            var allowedByPhone = results
                .GroupBy(r => r.phone)
                .ToDictionary(g => g.Key, g => g.Count(r => r.allowed));
            int overallAllowed = results.Count(r => r.allowed);

            int totalRequests = phoneNumbers.Length * requestsPerPhone;

            int totalResponses = results.Length;
            int trueResponses = overallAllowed;
            int falseResponses = totalResponses - trueResponses;
            Assert.Equal(totalRequests, totalResponses);
            Assert.Equal(totalRequests, trueResponses + falseResponses);
            foreach (var kvp in allowedByPhone)
            {
                Assert.True(kvp.Value <= phoneLimit,
                    $"Phone {kvp.Key} allowed count {kvp.Value} exceeds phone limit of {phoneLimit}");
            }

            Assert.True(overallAllowed <= accountLimit,
                $"Overall allowed count {overallAllowed} exceeds account limit of {accountLimit}");
        }
    }
}