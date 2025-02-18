using SmsThresholdChecker.Api.Models;
using SmsThresholdChecker.Api.Models.DTOs;
using SmsThresholdChecker.Api.Services;

namespace SmsThresholdChecker.Api.Interfaces
{
    public interface ISmsRateLimiter
    {
        Task<bool> CanSendSmsAsync(string phoneNumber);
        Task<IEnumerable<AccountCount>> GetAccountCountsAsync();
        Task<IEnumerable<PerSecondPhoneNumberCount>> GetPhoneNumberCountsAsync();
    }
}