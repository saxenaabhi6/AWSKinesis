using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Util;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft;

namespace WebApi.Controllers
{
    public class SNSController : ApiController
    {

        [ActionName("AWSSNSHandler")]
        [HttpPost]
        public HttpResponseMessage AWSSNSHandler()
        {
            var jsonData = Request.Content.ReadAsStringAsync().Result;
            Message snsMessage = Message.ParseMessage(jsonData);
            //logger.Debug("SNS request recieved: " + jsonData);

            if (snsMessage.IsSubscriptionType)
            {

                try
                {
                    snsMessage.SubscribeToTopic();
                    //logger.DebugFormat("AWS Subscription confirmed for topic ARN : {0}", snsMessage.TopicArn);
                    return Request.CreateResponse(HttpStatusCode.OK, new { });
                }
                catch (Exception ex)
                {
                    //logger.ErrorFormat("AWS Subscription Failed for topic ARN : {0}", snsMessage.TopicArn);
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Subscription Failed.");
                }
            }

            if (snsMessage.IsNotificationType)
            {
                string msgjson = snsMessage.MessageText;

                Newtonsoft.Json.Linq.JObject jo = Newtonsoft.Json.Linq.JObject.Parse(msgjson);
                string streamName = jo.SelectToken("Trigger.Dimensions[0].value").ToString();

                HttpResponseMessage rm = FireSHubMethod(streamName).GetAwaiter().GetResult();

            }
            return Request.CreateResponse(HttpStatusCode.OK, new { });
        }

        [ActionName("FireSHubMethod")]
        [HttpPost]
        public async Task<HttpResponseMessage> FireSHubMethod(string streamName)
        {
            using (HubConnection connection = new HubConnection(System.Configuration.ConfigurationManager.AppSettings["WebPortal"]))
            {   
                IHubProxy notyHubProxy = connection.CreateHubProxy("notyHub");
                await connection.Start();
                await notyHubProxy.Invoke("Send", streamName);
                
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
