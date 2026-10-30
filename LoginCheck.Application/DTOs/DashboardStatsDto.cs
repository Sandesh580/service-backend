namespace LoginCheck.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalActiveRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int InProgressServices { get; set; }
    public int CompletedJobs { get; set; }
    public double ActiveRequestsGrowth { get; set; } = 12.0;
}
