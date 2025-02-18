using System.Collections.Concurrent;

namespace SmsThresholdChecker.Api.Models
{
    public class PhoneNumberCount
    {
        public ConcurrentDictionary<string, int> PhoneNumberCounts { get; } = new();
    }
}