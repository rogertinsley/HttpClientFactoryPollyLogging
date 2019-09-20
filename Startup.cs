using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using System;
using Polly.CircuitBreaker;

[assembly: FunctionsStartup(typeof(Company.Function.Startup))]

namespace Company.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddHttpClient("pollyClient")
                            .AddPolicyHandler(GetRetryPolicy());
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handle 500/408 responses
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                
                .WaitAndRetryAsync(
                        3, // Retry count
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,retryAttempt)), // Exponential
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            var log = context.GetLogger();
                            log?.LogInformation(
                                $"Request failed with status code {outcome.Result.StatusCode} delaying for {timespan.TotalMilliseconds} milliseconds then making retry {retryAttempt}"
                            );
                        }
                );
        }
    }
}