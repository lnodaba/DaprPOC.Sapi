Pre Requirements:
- Latest Visual Studio
- Docker for desktop 

Steps:

1) Create as many project as you want in visual studio.
2) Dockerize the APPs in visual studio.
3) Add Docker compose support for the projects in visual studio.
4) Add centralized logging so we can see the logs for the cluster

```
  dozzle:
    container_name: dozzle
    image: 'amir20/dozzle:latest'
    volumes:
      - '/var/run/docker.sock:/var/run/docker.sock'
    ports:
      - '8082:8080'
    environment:
        DOZZLE_USERNAME: test
        DOZZLE_PASSWORD: test

```

5) Add postgress DB and Postgress PG Admin Support to docker compose:

```
  pgadmin:
    image: dpage/pgadmin4
    container_name: pg-admin
    environment:
        PGADMIN_DEFAULT_EMAIL: test@test.com
        PGADMIN_DEFAULT_PASSWORD: test
        PGADMIN_LISTEN_PORT: 8081
    ports:
      - '8081:8081'
    depends_on:
      - postgres-db

  postgres-db:
    container_name: postgres-db
    image: 'postgres:13.9-alpine'
    restart: always
    environment:
    - POSTGRES_USER=test
    - POSTGRES_PASSWORD=test
    ports:
    - '5432:5432'
    volumes:
    - 'db:/var/lib/postgresql/data'

volumes:
    db:
```

5) Add rabbit mq.

```
  rabbit-mq:
    container_name: rabbit-mq
    image: rabbitmq:3.9.4-management
    restart: always
    environment:
        - RABBITMQ_DEFAULT_USER=test
        - RABBITMQ_DEFAULT_PASS=test
    ports:
        - '5672:5672'
        - '5673:5673'
        - '15672:15672'
        - '15674:15674'
    healthcheck:
        test: ["CMD", "rabbitmqctl", "status"]
        interval: 30s
        timeout: 30s
        retries: 3
```

6) Add Redis

```
  redis:
    image: "redis:alpine"
    hostname: redisstate
    ports:
        - "6379:6379"
```

7) Add dappr sidecards for the 2 projects.

```

  daprpoc-dapr:
    image: "daprio/daprd:latest"
    container_name: daprpoc-dapr
    command: [ "./daprd", 
    "-app-id", 
    "daprpoc", 
    "-app-port", 
    "80", 
    "-components-path", 
    "./components" ]
    volumes:
      - "./components/:/components"
    depends_on:
      daprpoc:
        condition: service_started
      redis:
        condition: service_started
      rabbit-mq:
        condition: service_healthy
    network_mode: "service:daprpoc"

  daprpoc-dapr-producer:
    image: "daprio/daprd:latest"
    container_name: daprpoc-dapr-producer
    command: [ "./daprd", 
    "-app-id", 
    "daprpoc-producer", 
    "-app-port", 
    "80", 
    "-components-path", 
    "./components" ]
    volumes:
      - "./components/:/components"
    depends_on:
      daprpoc.producer:
        condition: service_started
      redis:
        condition: service_started
      rabbit-mq:
        condition: service_healthy
    network_mode: "service:daprpoc.producer"
```

8) Now we need to add the components we want to use, Redis and Rabit MQ so we can add that.

Create a components folder in the docker compose project and add the 2 components we will use.

pubsub.yaml

```
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: dapr-poc-pubsub
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
  - name: connectionString
    value: "amqp://test:test@rabbit-mq:5672"
  - name: durable
    value: "false"
  - name: deletedWhenUnused
    value: "false"
  - name: autoAck
    value: "false"
  - name: reconnectWait
    value: "0"
  - name: concurrency
    value: parallel
```

statestore.yaml

```
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: default
spec:
  type: state.redis
  metadata:
  - name: redisHost
    value: redis:6379
  - name: redisPassword
    value: ""
  - name: actorStateStore
    value: "true"
```

9) Add the subscription endpoint to the consumer project. And remove http redirects:

app.UseHttpsRedirection();


And the controller:

```
public class DaprSubscription
    {
        public string pubsubname { get; set; }
        public string topic { get; set; }
        public string route { get; set; }
    }

    public class DaprData<T>
    {
        public T data { get; set; }
    }

    public class Order
    {
        public int orderId { get; set; }
    }
	
	[ApiController]
    public class DaprController : Controller
    {
        public DaprController()
        {
        }

        //https://stackoverflow.com/a/40927517/1649967
        [Route("dapr/subscribe")]
        [HttpGet]
        public IActionResult Get()
        {
            var model = new List<DaprSubscription>()
            {
                new DaprSubscription()
                {
                    pubsubname = "dapr-poc-pubsub",
                    topic = "orders",
                    route = "orders"
                },
                new DaprSubscription()
                {
                    pubsubname = "dapr-poc-pubsub",
                    topic = "mangos",
                    route = "mangos"
                }
            };

            return Json(model);
        }

        [Route("orders")]
        [HttpPost]
        public IActionResult Post(DaprData<Order> requestData)
        {
            return Json(requestData);
        }

        [Route("mangos")]
        [HttpPost]
        public IActionResult Mangos(DaprData<Order> requestData)
        {
            return Json(requestData);
        }
    }
```

10) Then 2 ways for pushing messages to the queue, http and sdk:

```
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
```

11) Add Redis Cache Read Write Example.

12) Add Posgres Support.

13) Add Migrations.

14) Test everything again from begining to end.