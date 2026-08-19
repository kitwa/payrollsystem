using FluentValidation;
using MediatR;
using Payroll.Shared;

namespace Payroll.Application.Common.Behaviours;

/// <summary>Runs FluentValidation validators before the handler; short-circuits with Fail on any error.</summary>
public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next(ct);

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (failures.Count == 0) return await next(ct);

        var errors = failures.Select(f => f.ErrorMessage).ToList();

        // Return Result<T>.Fail or Result.Fail depending on TResponse
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failMethod = responseType.GetMethod("Fail", [typeof(IEnumerable<string>)])!;
            return (TResponse)failMethod.Invoke(null, [errors])!;
        }
        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Fail(errors);

        throw new ValidationException(failures);
    }
}
