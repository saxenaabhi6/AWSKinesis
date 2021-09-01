using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace AWSKinesis
{
    public class notyHub : Hub
    {
        public void Send(string streamName)
        {
            Clients.All.broadcastMessage(streamName);
        }
    }
}