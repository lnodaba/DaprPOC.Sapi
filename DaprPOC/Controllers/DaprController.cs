using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DaprPOC.Controllers
{
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
}
