using Polly;
using Polly.Extensions.Http;

namespace Order.Infrastructure.Resilience;

/// <summary>
/// Provides Polly resilience policies for outbound Order service calls.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates the retry policy used by outbound HTTP and gRPC requests.
    /// </summary>
    /// <returns>The retry policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    /// <summary>
    /// Creates the circuit breaker policy used by outbound HTTP and gRPC requests.
    /// </summary>
    /// <returns>The circuit breaker policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));
}