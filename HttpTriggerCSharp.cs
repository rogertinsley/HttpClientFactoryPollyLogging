using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;

namespace Company.Function
{
    public class HttpTriggerCSharp
    {
        private readonly IHttpClientFactory _factory;

        public HttpTriggerCSharp(IHttpClientFactory httpClientFactory) 
        {
            _factory = httpClientFactory;
        }
        
        [FunctionName("HttpTriggerCSharp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            HttpClient pollyClient = _factory.CreateClient("pollyClient");

            CircuitBreakerPolicy breaker = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2, 
                    durationOfBreak: TimeSpan.FromSeconds(10)
                );

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://httpstat.us/408");
            request.SetPolicyExecutionContext(new Context().WithLogger(log)); 
            var response = await breaker.ExecuteAsync(() => pollyClient.SendAsync(request));

            return new OkObjectResult("Wonder what took so long?  Check the logs.");

        }
    }
}
