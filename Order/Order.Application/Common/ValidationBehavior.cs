using FluentValidation;
using MediatR;

namespace Order.Application.Common;

/// <summary>
/// Executes FluentValidation validators before dispatching an order request handler.
/// </summary>
/// <typeparam name="TRequest">The request type being validated.</typeparam>
/// <typeparam name="TResponse">The response type returned by the pipeline.</typeparam>
/// <param name="validators">The validators applicable to the request type.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var errors = results.SelectMany(result => result.Errors).Where(error => error is not null).ToArray();
        if (errors.Length != 0)
        {
            throw new ValidationException(errors);
        }

        return await next();
    }
}