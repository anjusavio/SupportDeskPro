using SupportDeskPro.Domain.Enums;

namespace SupportDeskPro.Domain.Entities
{
    // Logs background job execution details for monitoring and debugging.
    // Tracks job status, execution time, processed records, and error information
    // to support observability and job analytics.
    public class BackgroundJobLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string JobName { get; set; } = string.Empty;
        public JobStatus Status { get; set; } = JobStatus.Running;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public int? DurationMs { get; set; }
        public int ProcessedCount { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
