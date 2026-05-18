using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services;

public class MailKitEmailService : IEmailService
{
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(ILogger<MailKitEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SIMULATED EMAIL SENT TO: {to} | SUBJECT: {subject} | BODY: {body}", to, subject, body);
        return Task.CompletedTask;
    }
}
