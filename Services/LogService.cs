using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using OpenRdpGuard.Models;

namespace OpenRdpGuard.Services
{
    public interface ILogService
    {
        Task<List<LoginAttempt>> GetLoginAttemptsAsync(TimeSpan duration);
        string? LastError { get; }
    }

    public class LogService : ILogService
    {
        public string? LastError { get; private set; }

        public Task<List<LoginAttempt>> GetLoginAttemptsAsync(TimeSpan duration)
        {
            return Task.Run(() =>
            {
                var results = new List<LoginAttempt>();
                LastError = null;
                try
                {
                    var milliseconds = (long)duration.TotalMilliseconds;
                    var query = $"*[System[(EventID=4625 or EventID=4624) and TimeCreated[timediff(@SystemTime) <= {milliseconds}]]]";
                    var elq = new EventLogQuery("Security", PathType.LogName, query);

                    using (var reader = new EventLogReader(elq))
                    {
                        for (var eventInstance = reader.ReadEvent(); eventInstance != null; eventInstance = reader.ReadEvent())
                        {
                            var attempt = ParseEvent(eventInstance);
                            if (attempt != null)
                            {
                                results.Add(attempt);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Error reading logs: {ex.Message}");
                }
                return results;
            });
        }

        private LoginAttempt? ParseEvent(EventRecord record)
        {
            try
            {
                var attempt = new LoginAttempt
                {
                    Timestamp = record.TimeCreated ?? DateTime.MinValue,
                    EventId = record.Id,
                    IsSuccess = record.Id == 4624
                };

                // Parse Properties. Note: Index varies by OS version sometimes, but usually stable.
                // Better to use XML parsing for robustness, but Properties is faster.
                // 4625: IpAddress is usually index 19.
                // 4624: IpAddress is usually index 18.
                // Let's use generic XML parsing for safety.

                var xml = record.ToXml();
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var data = doc.Descendants().Where(d => d.Name.LocalName == "Data").ToList();

                var ipNode = data.FirstOrDefault(x => x.Attribute("Name")?.Value == "IpAddress");
                var userNode = data.FirstOrDefault(x => x.Attribute("Name")?.Value == "TargetUserName");

                if (ipNode != null) attempt.IpAddress = ipNode.Value;
                if (userNode != null) attempt.Username = userNode.Value;

                // Filter out local/internal IPs if needed, or empty ones (common in 4624 for system services)
                if (string.IsNullOrWhiteSpace(attempt.IpAddress) || attempt.IpAddress == "-") return null;

                return attempt;
            }
            catch
            {
                return null;
            }
        }
    }
}
