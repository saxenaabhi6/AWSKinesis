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
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                initialiseControls();
                DataBind();
            }

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
            playStream(streamName, HLSPlaybackMode.LIVE, DateTime.MinValue, DateTime.MinValue);
        }

        protected void BTN_GetClip_Click(object sender, EventArgs e)
        {
            GetClip(SelectedStream, DateTime.Parse(TB_st.Text), DateTime.Parse(TB_et.Text));
        }

        protected void BTN_playArchive_Click(object sender, EventArgs e)
        {
            playStream(SelectedStream, HLSPlaybackMode.ON_DEMAND, DateTime.Parse(TB_st.Text), DateTime.Parse(TB_et.Text));
        }


        protected void LBPlayLive_Command(object sender, CommandEventArgs e)
        {
            SelectedStream = e.CommandArgument.ToString();
            PlayStreamLive(SelectedStream);
        }

        protected void BTN_FetchStreams_Click(object sender, EventArgs e)
        {
            FetchStreams();
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
                AmazonKinesisVideoClient amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);
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
        /// Can play stream in both live and archived mode.
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="hLSPlaybackMode"></param>
        /// <param name="st"></param>
        /// <param name="et"></param>
        protected void playStream(string streamName, HLSPlaybackMode hLSPlaybackMode, DateTime st, DateTime et)
        {
            try
            {
                Amazon.RegionEndpoint rep = Amazon.RegionEndpoint.GetBySystemName(DDL_Region.SelectedValue);
                AmazonKinesisVideoClient amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);

                GetDataEndpointRequest endpointRequest = new GetDataEndpointRequest()
                {
                    StreamName = streamName,
                    APIName = "GET_HLS_STREAMING_SESSION_URL"
                };

                GetDataEndpointResponse endpointResponse = amazonKinesisVideoClient.GetDataEndpoint(endpointRequest);

                AmazonKinesisVideoArchivedMediaClient amazonkinesisVideoArchivedMediaClient = new AmazonKinesisVideoArchivedMediaClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, endpointResponse.DataEndpoint);

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
                    ContainerFormat = ContainerFormat.FRAGMENTED_MP4,
                    DiscontinuityMode = HLSDiscontinuityMode.ALWAYS,
                    DisplayFragmentTimestamp = HLSDisplayFragmentTimestamp.ALWAYS
                    //,MaxMediaPlaylistFragmentResults = 5
                    ,
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
                AmazonKinesisVideoClient amazonKinesisVideoClient = new AmazonKinesisVideoClient(TB_AccessKeyId.Text, TB_SecretAccessKey.Text, TB_SessionToken.Text, rep);

                GetDataEndpointRequest endpointRequest = new GetDataEndpointRequest()
                {
                    StreamName = streamName,
                    APIName = "GET_CLIP"
                };
                GetDataEndpointResponse endpointResponse = amazonKinesisVideoClient.GetDataEndpoint(endpointRequest);

                AmazonKinesisVideoArchivedMediaClient amazonkinesisVideoArchivedMediaClient = new AmazonKinesisVideoArchivedMediaClient(TB_AccessKeyId.Text,
                    TB_SecretAccessKey.Text,
                    TB_SessionToken.Text,
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

        #endregion

        #region Helper Fucntions
        public void LogError(string error)
        {
            PNL_Logs.BackColor = System.Drawing.ColorTranslator.FromHtml("#ffcccc");
            PNL_Logs.Text += "[Error] " + DateTime.Now.ToString() + " : " + error + "\n";
        }
        public void LogInfo(string info)
        {
            PNL_Logs.BackColor = System.Drawing.Color.LightGreen;
            PNL_Logs.Text += "[Info] " + DateTime.Now.ToString() + " : " + info + "\n";
        }
        #endregion
    }
}