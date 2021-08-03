# AWSKinesis
Amazon Kinesis Video Streams Media Viewer in .Net

## About
This is a simple ASP.Net webform page that simplifies testing and experiments with HLS output from Amazon Kinesis Video Streams. This is based on the <a href="https://docs.aws.amazon.com/kinesisvideostreams/latest/dg/how-hls.html">documentation</a>.

## About Source Code
The code is devided in to following sections:
1. Properties
2. Page Events : Define button click events for Page interations.
4. AWS Kinesis Video API Calls : Collection of all the API Calls to Kinesis.
5. Helper Funtions : For initialisation and Logging purppose.

## What's Next
1. API Module for SNS Subscription
2. Stream Creation with Automatic Alarm Creation subscribed to a SNS Topic with an endpoint back to the API above.
3. Recieve Notifications about Streams.
4. SignalR to display alerts on the dashboard based on notifications.


## License Summary

This sample code is made available under a modified MIT license. See the LICENSE file.
