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
                string OldStateValue = jo.SelectToken("OldStateValue").ToString();
                string NewStateValue = jo.SelectToken("NewStateValue").ToString();
                string streamName = jo.SelectToken("Trigger.Dimensions[0].value").ToString();

                if(OldStateValue == "OK" && NewStateValue == "ALARM")
                    Task.Run(() => FireSHubMethodAsync(streamName, true)).Wait();
                if (OldStateValue == "ALARM" && NewStateValue == "OK")
                    Task.Run(() => FireSHubMethodAsync(streamName, false)).Wait();

            }
            return Request.CreateResponse(HttpStatusCode.OK, new { });
        }


        //[ActionName("FireSHubMethod")]
        //[HttpPost]
        //public HttpResponseMessage FireSHubMethod(string streamName)
        //{
        //    //HttpResponseMessage rm = FireSHubMethodAsync(streamName).GetAwaiter().GetResult();
        //    Task.Run(() => FireSHubMethodAsync(streamName)).Wait();
        //    //return rm;
        //    return new HttpResponseMessage(HttpStatusCode.OK);
        //}

        [ActionName("FireSHubMethodAsync")]
        [HttpPost]
        public async Task<HttpResponseMessage> FireSHubMethodAsync(string streamName, bool islive)
        {
            using (HubConnection connection = new HubConnection(System.Configuration.ConfigurationManager.AppSettings["WebPortal"]))
            {   
                IHubProxy notyHubProxy = connection.CreateHubProxy("notyHub");
                await connection.Start();
                await notyHubProxy.Invoke("Send", new object[] { streamName, islive });
                
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
