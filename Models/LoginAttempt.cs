using System;

namespace OpenRdpGuard.Models
{
    public class LoginAttempt
    {
        public string IpAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public int EventId { get; set; }
    }
}
