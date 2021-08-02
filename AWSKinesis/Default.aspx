﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AWS_Kinesis_POC.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>AWS KVS Example</title>
    <!-- Video JS -->
    <link rel="stylesheet" href="https://vjs.zencdn.net/6.6.3/video-js.css" />
    <script src="https://vjs.zencdn.net/6.6.3/video.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/videojs-contrib-hls/5.14.1/videojs-contrib-hls.js"></script>
    
    <!-- Bootstrap V5.0 -->
    <!-- CSS only -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <!-- JavaScript Bundle with Popper -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/js/bootstrap.bundle.min.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        
        <div class="container-fluid">
            <nav class="navbar navbar-dark bg-dark mb-3">
    <div class="container-fluid">
      <a class="navbar-brand" href="#">Amazon Kinesis Video Streams Media Viewer in .Net</a>
    </div>
  </nav>
            <div class="main">
            <div class="row">
                <div class="col-3 gx-5">
                    <div class="mb-3">
                            <label>Region</label>
                            <asp:DropDownList runat="server" id="DDL_Region" class="form-control form-control-sm">
                            </asp:DropDownList>
                        </div>
                        <div class="mb-3">    
                        <label>AWS Access Key</label>
                            <asp:TextBox runat="server" id="TB_AccessKeyId" type="password" class="form-control form-control-sm"/>
                        </div>
                        <div class="mb-3">
                            <label>AWS Secret Key</label>
                            <asp:TextBox runat="server" id="TB_SecretAccessKey" type="password" class="form-control form-control-sm"/>
                        </div>
                        <div class="mb-3">
                            <label>AWS Session Token (Optional)</label>
                            <asp:TextBox runat="server" id="TB_SessionToken" type="password" class="form-control form-control-sm" />
                        </div>
                        <div class="mb-3">
                            <label>Endpoint (Optional)</label>
                            <asp:TextBox runat="server" id="TB_Endpoint" type="text" class="form-control form-control-sm" />
                        </div>
                  
                        <asp:Button runat="server" ID="BTN_FetchStreams" Text="Fetch Streams" OnClick="BTN_FetchStreams_Click" CssClass=" btn btn-primary"  style="margin-top:10px;"/>
                    
                    
                    <h4 style="margin-top: 20px;">Click on a Stream to play.</h4>    
                    <div class="card bg-light" >
                   
                    <ul class="list-group">
                        <asp:Repeater runat="server" DataSource="<%# Streams %>" ID="RepStreams">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" Text="<%# GetDataItem() %>" OnCommand="LBPlayLive_Command" CommandArgument="<%# GetDataItem() %>" CssClass="list-group-item list-group-item-action" ID="LBPlayLive"></asp:LinkButton>

                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                        </div>

                </div>
                <div class="col-3">
                    <div class="mb-3">
                            <label>Playback Mode</label>
                        <asp:DropDownList runat="server" ID="DDL_PlaybackMode" CssClass="form-control form-control-sm">
                            <asp:ListItem Text="Live" Value="LIVE" Selected="True"></asp:ListItem>
                            <asp:ListItem Text="Live Replay" Value="LIVE_REPLAY"></asp:ListItem>
                            <asp:ListItem Text="On Demand" Value="ON_DEMAND"></asp:ListItem>
                        </asp:DropDownList>
                        </div>
                        <div class="mb-3">
                            <label>Start Timestamp</label>
                            <asp:TextBox TextMode="DateTimeLocal" runat="server" ID="TB_st" CssClass="form-control form-control-sm"></asp:TextBox>
                        </div>
                        <div class="mb-3">
                            <label>End Timestamp</label>
                            <asp:TextBox TextMode="DateTimeLocal" runat="server" ID="TB_et" CssClass="form-control form-control-sm"></asp:TextBox>
                        </div>
                        <div class="mb-3">
                            <label>Fragment Selector Type</label>
                            <asp:DropDownList runat="server" id="DDL_FragmentSelectorType" class="form-control form-control-sm">
                                <asp:ListItem Value="SERVER_TIMESTAMP">SERVER_TIMESTAMP</asp:ListItem>
                                <asp:ListItem Value="PRODUCER_TIMESTAMP">PRODUCER_TIMESTAMP</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="mb-3">
                            <label>Container Format</label>
                            <asp:DropDownList runat="server" id="DDL_ContainerFormat" class="form-control form-control-sm">
                                <asp:ListItem Value="FRAGMENTED_MP4">FRAGMENTED_MP4</asp:ListItem>
                                <asp:ListItem Value="MPEG_TS">MPEG_TS</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="mb-3">
                            <label>Discontinuity Mode</label>
                            <asp:DropDownList runat="server" id="DDL_DiscontinuityMode" class="form-control form-control-sm">
                                <asp:ListItem Value="ALWAYS">ALWAYS</asp:ListItem>
                                <asp:ListItem Value="NEVER">NEVER</asp:ListItem>
                                <asp:ListItem Value="ON_DISCONTINUITY">ON_DISCONTINUITY</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="mb-3">
                            <label>Display Fragment Timestamp</label>
                            <asp:DropDownList runat="server" id="DDL_DisplayFragmentTimestamp" class="form-control form-control-sm">
                                <asp:ListItem Value="ALWAYS">ALWAYS</asp:ListItem>
                                <asp:ListItem Value="NEVER">NEVER</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="mb-3">
                            <label>Display Fragment Number</label>
                            <asp:DropDownList runat="server" id="DDL_DisplayFragmentNumber" class="form-control form-control-sm">
                                <asp:ListItem Value="ALWAYS">ALWAYS</asp:ListItem>
                                <asp:ListItem Value="NEVER">NEVER</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="mb-3">
                            <label>Max Manifest/Playlist Fragment Results</label>
                            <asp:TextBox runat="server" id="TB_MaxResults" type="text" class="form-control form-control-sm"/>
                        </div>
                        <div class="mb-3">
                            <label>Expires (seconds)</label>
                            <asp:TextBox runat="server" id="TB_Expires" type="text" TextMode="Number" class="form-control form-control-sm"/>
                        </div>
                     <asp:Button runat="server" ID="BTN_playArchive" Text="Play Archive" OnClick="BTN_playArchive_Click" CssClass="btn btn-primary" />
                        
                            <asp:Button runat="server" ID="BTN_GetClip" Text="Get Clip" OnClick="BTN_GetClip_Click" CssClass="btn btn-primary" />
                </div>
                <div class="col-6 gx-5">
                    <div class="card bg-light">
                        Details of the Stream
                    </div>
                    <div class="row justify-content-center align-items-center">
                        <div>
                            <br />
                            <video runat="server" id="JsPlayer" class="video-js vjs-default-skin vjs-big-play-centered" style="display: block; margin: 0 auto;" controls="controls" preload="none" height="480" width="720" data-setup='{"techOrder": ["flash", "nativeControlsForTouch": true, "progressControl": false } }'>
                                <source id="source" runat="server" type="application/x-mpegURL" />
                            </video>
                        </div>
                    </div>
                    <h4 style="margin-top: 20px;">Logs</h4>
                        <div class="card bg-light mb-3">
                            <asp:Label runat="server" id="PNL_Logs" class="card-body text-monospace" style="font-family: monospace; white-space: pre-wrap;"></asp:Label>
                        </div> 
                </div>
            </div>
            </div>
        </div>
    </form>
</body>
</html>
