using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using Dapr.Client;

namespace DaprPOC.Producer.Controllers
{
    public record Order([property: JsonPropertyName("orderId")] int OrderId);

    [ApiController]
    [Route("[controller]/[action]")]
    public class DaprProducerController : Controller
    {
        private const string PUBSUBNAME = "dapr-poc-pubsub";
        private const string TOPIC = "orders";
        private const string TOPIC2 = "mangos";

        [HttpGet(Name = "SendMessagesOverHttp")]
        public async Task SendMessagesOverHttp()
        {
            var baseURL = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" + (Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"); 
            
            Console.WriteLine($"Publishing to baseURL: {baseURL}, Pubsub Name: {PUBSUBNAME}, Topic: {TOPIC} ");

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            for (int i = 1; i <= 10; i++)
            {
                var order = new Order(i);
                var orderJson = JsonSerializer.Serialize<Order>(order);
                var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

                // Publish an event/message using Dapr PubSub via HTTP Post
                var response = httpClient.PostAsync($"{baseURL}/v1.0/publish/{PUBSUBNAME}/{TOPIC}", content);
                Console.WriteLine("Published data: " + order);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        [HttpGet(Name = "SendMessagesOverDaprSdk")]
        public async Task SendMessagesOverDaprSdk()
        {
            var baseURL = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" + (Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500");

            Console.WriteLine($"Publishing to baseURL: {baseURL}, Pubsub Name: {PUBSUBNAME}, Topic: {TOPIC} ");

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            for (int i = 1; i <= 10; i++)
            {
                var order = new Order(i);
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken cancellationToken = source.Token;
                using var client = new DaprClientBuilder().Build();
                //Using Dapr SDK to publish a topic
                await client.PublishEventAsync(PUBSUBNAME, TOPIC2, order, cancellationToken);

                Console.WriteLine("Published data: " + order);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
