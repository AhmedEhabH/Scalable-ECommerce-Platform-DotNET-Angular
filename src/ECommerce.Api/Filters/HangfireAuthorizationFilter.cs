using Hangfire.Dashboard;

namespace ECommerce.Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var isLocal = httpContext.Connection.RemoteIpAddress != null &&
                      (httpContext.Connection.RemoteIpAddress.Equals(httpContext.Connection.LocalIpAddress) ||
                       System.Net.IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress));

        if (isLocal) return true;

        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
