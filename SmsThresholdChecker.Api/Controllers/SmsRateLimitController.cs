using Microsoft.AspNetCore.Mvc;
using SmsThresholdChecker.Api.Interfaces;
using SmsThresholdChecker.Api.Services;

namespace SmsThresholdChecker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmsRateLimitController : ControllerBase
    {
        private readonly ISmsRateLimiter _smsRateLimiter;
        private readonly ILogger<SmsRateLimitController> _logger;

        public SmsRateLimitController(ISmsRateLimiter rateLimiter, ILogger<SmsRateLimitController> logger)
        {
            _smsRateLimiter = rateLimiter;
            _logger = logger;
        }

        [HttpGet("check")]
        public async Task<IActionResult> Check(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    _logger.LogWarning("Phone number is missing.");
                    return BadRequest(new { Error = "Phone number is required." });
                }

                bool allowed = await _smsRateLimiter.CanSendSmsAsync(phoneNumber);
                return Ok(new { Allowed = allowed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while checking SMS allowance for {phoneNumber}");

                return StatusCode(500, new { Error = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("account-counts")]
        public async Task<IActionResult> GetAccountLastHourTotalCounts()
        {
            try
            {
                var status = await _smsRateLimiter.GetAccountCountsAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving account status.");
                return StatusCode(500, new { Error = "An unexpected error occurred while retrieving account status." });
            }
        }

        [HttpGet("phone-numbers-counts")]
        public async Task<IActionResult> GetPhoneNumberLastHourCounts()
        {
            try
            {
                var statuses = await _smsRateLimiter.GetPhoneNumberCountsAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving number statuses.");
                return StatusCode(500, new { Error = "An unexpected error occurred while retrieving number statuses." });
            }
        }
    }
}
