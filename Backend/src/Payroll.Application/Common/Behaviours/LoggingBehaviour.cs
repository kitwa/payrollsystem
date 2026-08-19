using MediatR;
using Microsoft.Extensions.Logging;

namespace Payroll.Application.Common.Behaviours;

/// <summary>Logs every request and response at Debug level.</summary>
public class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogDebug("Handling {RequestName}: {@Request}", typeof(TRequest).Name, request);
        var response = await next(ct);
        logger.LogDebug("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
