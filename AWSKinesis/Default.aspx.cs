using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Amazon.KinesisVideo;
using Amazon.KinesisVideo.Model;
using Amazon.KinesisVideoArchivedMedia;
using Amazon.KinesisVideoArchivedMedia.Model;
using Amazon.KinesisVideoMedia;
using Amazon.CloudWatch;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace AWS_Kinesis_POC
{
    public partial class Default : Page
    {

        #region Properties
        List<string> streams = new List<string>();
        public List<String> Streams
        {
            get { return streams; }
            set { streams = value; }
        }

        public string SelectedStream
        {
            get { return ViewState["selectedStream"].ToString(); }
            set { ViewState["selectedStream"] = value; }
        }

        public string CreatedTopicARN
        {
            get { return ViewState["createdTopicARN"].ToString(); }
            set { ViewState["createdTopicARN"] = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                initialiseControls();
                DataBind();
            }
            //string jsn = "{\"AlarmName\":\"MUSTMVDEVCAM1_kvs_up\",\"AlarmDescription\":null,\"AWSAccountId\":\"671473650788\",\"NewStateValue\":\"ALARM\",\"NewStateReason\":\"Threshold Crossed: 1 out of the last 1 datapoints [1.0 (02/08/21 01:09:00)] was greater than or equal to the threshold (1.0) (minimum 1 datapoint for OK -> ALARM transition).\",\"StateChangeTime\":\"2021-08-02T01:11:49.272+0000\",\"Region\":\"Asia Pacific (Sydney)\",\"AlarmArn\":\"arn:aws:cloudwatch:ap-southeast-2:671473650788:alarm:MUSTMVDEVCAM1_kvs_up\",\"OldStateValue\":\"OK\",\"Trigger\":{\"MetricName\":\"PutMedia.ActiveConnections\",\"Namespace\":\"AWS/KinesisVideo\",\"StatisticType\":\"Statistic\",\"Statistic\":\"MAXIMUM\",\"Unit\":null,\"Dimensions\":[{\"value\":\"MUSTMVDEVCAM1\",\"name\":\"StreamName\"}],\"Period\":60,\"EvaluationPeriods\":1,\"ComparisonOperator\":\"GreaterThanOrEqualToThreshold\",\"Threshold\":1.0,\"TreatMissingData\":\"- TreatMissingData:                    missing\",\"EvaluateLowSampleCountPercentile\":\"\"}}";
            //Newtonsoft.Json.Linq.JObject jo = Newtonsoft.Json.Linq.JObject.Parse(jsn);
            //string streamName = jo.SelectToken("Trigger.Dimensions[0].value").ToString();

            //string streamName = msg["Trigger"]["Dimensions"][0]["value"];

        }

        private void initialiseControls()
        {
            TB_st.Text = DateTime.Now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm");
            TB_et.Text = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            source.Attributes.Add("src", "");
            foreach (Amazon.RegionEndpoint rep in Amazon.RegionEndpoint.EnumerableAllRegions)
                DDL_Region.Items.Add(new ListItem(rep.ToString(), rep.SystemName));
            TB_Expires.Text = "300";
        }

        #region Page Events
        protected void PlayStreamLive(string streamName)
        {
            playStream(streamName);
        }

        protected void BTN_GetClip_Click(object sender, EventArgs e)
        {
            GetClip(SelectedStream, DateTime.Parse(TB_st.Text), DateTime.Parse(TB_et.Text));
        }

        protected void BTN_playStream_Click(object sender, EventArgs e)
        {
            playStream(SelectedStream);
        }


        protected void LBDescribeStream_Command(object sender, CommandEventArgs e)
        {
            SelectedStream = e.CommandArgument.ToString();
            DescribeStream(SelectedStream);
            
        }

        protected void BTN_FetchStreams_Click(object sender, EventArgs e)
        {
            FetchStreams();
        }

        protected void BT_CreateStream_Click(object sender, EventArgs e)
        {
            CreateStream(TB_StreamName.Text, 2);
        }
        #endregion

        #region AWS Kinesis Video API Calls.
        /// <summary>
        /// Fetching AKV Streams based on Credentials entered.
        /// </summary>
        protected void FetchStreams()
        {
            try
            {
                Amazon.RegionEndpoint rep = Amazon.RegionEndpoint.GetBySystemName(DDL_Region.SelectedValue);

                AmazonSimpleNotificationServiceClient amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);
                CreateTopicRequest createTopicRequest = new CreateTopicRequest()
                {
                    Name = "Altitude_UAT_KVS"
                };
                CreateTopicResponse createTopicResponse = amazonSimpleNotificationServiceClient.CreateTopic(createTopicRequest);
                LogInfo("Topic Creation Successful with ARN: " + createTopicResponse.TopicArn);
                CreatedTopicARN = createTopicResponse.TopicArn;

                SubscribeRequest subscribeRequest = new SubscribeRequest()
                {
                    Endpoint = System.Configuration.ConfigurationManager.AppSettings["SNSSubscriptionAPI"],
                    TopicArn = createTopicResponse.TopicArn,
                    Protocol = "https"
                };
                SubscribeResponse subscribeResponse = amazonSimpleNotificationServiceClient.Subscribe(subscribeRequest);
                LogInfo("Topic Subsciption Created with ARN: " + subscribeResponse.SubscriptionArn);


                AmazonKinesisVideoClient amazonKinesisVideoClient;
                if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                    amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);
                else
                    amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);

                ListStreamsRequest listStreamsRequest = new ListStreamsRequest()
                {
                    MaxResults = 100
                };

                ListStreamsResponse listStreamsResponse = amazonKinesisVideoClient.ListStreams(listStreamsRequest);
                LogInfo(string.Format("{0} Stream(s) fetched.", listStreamsResponse.StreamInfoList.Count.ToString()));
                foreach (StreamInfo streamInfo in listStreamsResponse.StreamInfoList)
                {
                    Streams.Add(streamInfo.StreamName);
                }
                RepStreams.DataBind();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return;
            }
        }

        /// <summary>
        /// We get stream related information from AWS and display it in panel.
        /// </summary>
        /// <param name="streamName"></param>
        protected void DescribeStream(string streamName)
        {
            Amazon.RegionEndpoint rep = Amazon.RegionEndpoint.GetBySystemName(DDL_Region.SelectedValue);
            AmazonKinesisVideoClient amazonKinesisVideoClient;
            if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);
            else
                amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);

            DescribeStreamRequest describeStreamRequest = new DescribeStreamRequest() { StreamName = streamName };
            DescribeStreamResponse describeStreamResponse = amazonKinesisVideoClient.DescribeStream(describeStreamRequest);
            LBL_Details.Text = "Name: " + describeStreamResponse.StreamInfo.StreamName + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" +
                               "Retention (Hours): " + describeStreamResponse.StreamInfo.DataRetentionInHours + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; " +
                               "Creation Time: " + describeStreamResponse.StreamInfo.CreationTime + "<br/>" +
                               "ARN: " + describeStreamResponse.StreamInfo.StreamARN + "<br/>" +
                               "Media Type: " + describeStreamResponse.StreamInfo.MediaType + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" +
                               "Device Name: " + describeStreamResponse.StreamInfo.DeviceName;
        }

        /// <summary>
        /// Can play stream in both live and archived mode.
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="hLSPlaybackMode"></param>
        /// <param name="st"></param>
        /// <param name="et"></param>
        protected void playStream(string streamName)
        {
            try
            {
                HLSPlaybackMode hLSPlaybackMode = (HLSPlaybackMode)DDL_PlaybackMode.SelectedValue;
                Amazon.RegionEndpoint rep = Amazon.RegionEndpoint.GetBySystemName(DDL_Region.SelectedValue);
                DateTime st = DateTime.Parse(TB_st.Text);
                DateTime et = DateTime.Parse(TB_et.Text);

                AmazonKinesisVideoClient amazonKinesisVideoClient;
                if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                    amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);
                else
                       amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);

                GetDataEndpointRequest endpointRequest = new GetDataEndpointRequest()
                {
                    StreamName = streamName,
                    APIName = "GET_HLS_STREAMING_SESSION_URL"
                };

                GetDataEndpointResponse endpointResponse = amazonKinesisVideoClient.GetDataEndpoint(endpointRequest);

                AmazonKinesisVideoArchivedMediaClient amazonkinesisVideoArchivedMediaClient;
                if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                    amazonkinesisVideoArchivedMediaClient = new AmazonKinesisVideoArchivedMediaClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, endpointResponse.DataEndpoint);
                else
                    amazonkinesisVideoArchivedMediaClient = new AmazonKinesisVideoArchivedMediaClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, endpointResponse.DataEndpoint);

                HLSTimestampRange hLSTimestampRange = new HLSTimestampRange()
                {
                    StartTimestamp = st,
                    EndTimestamp = et
                };

                HLSFragmentSelector hLSFragmentSelector = new HLSFragmentSelector()
                {
                    TimestampRange = hLSPlaybackMode != HLSPlaybackMode.LIVE ? hLSTimestampRange : null,
                    FragmentSelectorType = HLSFragmentSelectorType.PRODUCER_TIMESTAMP,
                };

                int expiry = 300;
                int.TryParse(TB_Expires.Text, out expiry);

                GetHLSStreamingSessionURLRequest getHLSStreamingSessionURLRequest = new GetHLSStreamingSessionURLRequest()
                {
                    StreamName = streamName,
                    PlaybackMode = hLSPlaybackMode,
                    HLSFragmentSelector = hLSFragmentSelector,
                    ContainerFormat = ContainerFormat.MPEG_TS,
                    DiscontinuityMode = HLSDiscontinuityMode.ALWAYS,
                    DisplayFragmentTimestamp = HLSDisplayFragmentTimestamp.ALWAYS,
                    //,MaxMediaPlaylistFragmentResults = 5
                    Expires = expiry
                };

                GetHLSStreamingSessionURLResponse getHLSStreamingSessionURLResponse = amazonkinesisVideoArchivedMediaClient.GetHLSStreamingSessionURL(getHLSStreamingSessionURLRequest);
                LogInfo(string.Format("HLS URL generated for the Stream: {0}", getHLSStreamingSessionURLResponse.HLSStreamingSessionURL));

                source.Attributes.Add("src", getHLSStreamingSessionURLResponse.HLSStreamingSessionURL);
            }
            catch (Exception ex)
            {
                LogError("Playing of stream failed with error: " + ex.Message);

            }
        }

        /// <summary>
        /// Download the Archived content between the selected dates.
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="st"></param>
        /// <param name="et"></param>
        protected void GetClip(string streamName, DateTime st, DateTime et)
        {
            try
            {
                Amazon.RegionEndpoint rep = Amazon.RegionEndpoint.GetBySystemName(DDL_Region.SelectedValue);
                AmazonKinesisVideoClient amazonKinesisVideoClient;
                if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                    amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);
                else
                       amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);
                GetDataEndpointRequest endpointRequest = new GetDataEndpointRequest()
                {
                    StreamName = streamName,
                    APIName = "GET_CLIP"
                };
                GetDataEndpointResponse endpointResponse = amazonKinesisVideoClient.GetDataEndpoint(endpointRequest);

                AmazonKinesisVideoArchivedMediaClient amazonkinesisVideoArchivedMediaClient;
                if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                amazonkinesisVideoArchivedMediaClient = new AmazonKinesisVideoArchivedMediaClient(TB_AccessKeyId.Text,
                    TB_SecretAccessKey.Text,
                    TB_SessionToken.Text,
                    endpointResponse.DataEndpoint);
                else
                    amazonkinesisVideoArchivedMediaClient = new AmazonKinesisVideoArchivedMediaClient(TB_AccessKeyId.Text,
                    TB_SecretAccessKey.Text,
                    endpointResponse.DataEndpoint);
                ClipTimestampRange clipTimestampRange = new ClipTimestampRange()
                {
                    StartTimestamp = st,
                    EndTimestamp = et
                };
                ClipFragmentSelector clipFragmentSelector = new ClipFragmentSelector()
                {
                    FragmentSelectorType = ClipFragmentSelectorType.SERVER_TIMESTAMP,
                    TimestampRange = clipTimestampRange
                };
                GetClipRequest getClipRequest = new GetClipRequest()
                {
                    ClipFragmentSelector = clipFragmentSelector,
                    StreamName = streamName
                };
                GetClipResponse getClipResponse = amazonkinesisVideoArchivedMediaClient.GetClip(getClipRequest);

                Response.Clear();
                Response.AppendHeader("Content-Disposition", "Attachment; Filename = abs_kinesis.mp4");
                Response.ContentType = "application/octet-stream";

                byte[] buffer = new byte[getClipResponse.ContentLength];
                for (int totalBytesCopied = 0; totalBytesCopied < getClipResponse.ContentLength;)
                    totalBytesCopied += getClipResponse.Payload.Read(buffer, totalBytesCopied, Convert.ToInt32(getClipResponse.ContentLength) - totalBytesCopied);

                Response.Flush();
                Response.BinaryWrite(buffer);
                Response.Flush();
                Response.End();
            }
            catch (Exception ex)
            {
                LogError("Download of clip failed with error: " + ex.Message);

            }

        }

        protected void CreateStream(string streamName, int retentionInHours)
        {
            try
            {
                Amazon.RegionEndpoint rep = Amazon.RegionEndpoint.GetBySystemName(DDL_Region.SelectedValue);
                AmazonKinesisVideoClient amazonKinesisVideoClient;
                if (!string.IsNullOrWhiteSpace(TB_SessionToken.Text))
                    amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);
                else
                    amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);

                CreateStreamRequest createStreamRequest = new CreateStreamRequest()
                {
                    StreamName = streamName,
                    DataRetentionInHours = retentionInHours
                };

                CreateStreamResponse createStreamResponse = amazonKinesisVideoClient.CreateStream(createStreamRequest);

                LogInfo(string.Format("Stream Created with ARN: {0}.", createStreamResponse.StreamARN));

                //create cloudwatch alarm for the stream above

                AmazonCloudWatchClient amazonCloudWatchClient = new AmazonCloudWatchClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, rep);
                amazonCloudWatchClient.PutMetricAlarm(new Amazon.CloudWatch.Model.PutMetricAlarmRequest()
                {
                    AlarmName = streamName + "_kvs_up",
                    ComparisonOperator = Amazon.CloudWatch.ComparisonOperator.GreaterThanOrEqualToThreshold,
                    MetricName = "PutMedia.ActiveConnections",
                    Namespace = "AWS/KinesisVideo",
                    Period = 60,
                    Statistic = Statistic.Maximum,
                    Threshold = 1,
                    ActionsEnabled = true,
                    AlarmActions = new List<string> { CreatedTopicARN },
                    OKActions = new List<string> { CreatedTopicARN },
                    TreatMissingData = "missing",
                    Dimensions = new List<Amazon.CloudWatch.Model.Dimension>(1) { new Amazon.CloudWatch.Model.Dimension { Name = "StreamName", Value = streamName } },
                    EvaluationPeriods = 1,
                    DatapointsToAlarm = 1
                });
                LogInfo(string.Format("Alarm Created for the stream: {0}.", streamName));

            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return;
            }
        }
        #endregion

        #region Helper Fucntions
        public void LogError(string error)
        {
            
            PNL_Logs.Text = "<div class=\"alert alert-danger\">[Error] " + DateTime.Now.ToString() + " : " + error + "</div>" + PNL_Logs.Text ;
        }
        public void LogInfo(string info)
        {
            
            PNL_Logs.Text = "<div class=\"alert alert-success\">[Info] " + DateTime.Now.ToString() + " : " + info + "</div>" + PNL_Logs.Text ;
        }
        #endregion


    }
}