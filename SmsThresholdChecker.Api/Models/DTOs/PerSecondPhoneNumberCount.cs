namespace SmsThresholdChecker.Api.Models.DTOs
{
    public class PerSecondPhoneNumberCount
    {
        public DateTime Time { get; set; }
        public IEnumerable<PhoneNumberRecord> PhoneNumbers { get; set; }
    }
}
