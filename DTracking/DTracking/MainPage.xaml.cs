using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Pinpoint;
using Amazon.Pinpoint.Model;
using Microsoft.AppCenter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DTracking
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string region = "us-east-1";
        string appId = "xxxxx7";
        string poolId = "us-east-1:xxxxx";

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CognitoAWSCredentials credentials = new CognitoAWSCredentials(poolId, RegionEndpoint.USEast1);
                var pinpoint = new AmazonPinpointClient(credentials, RegionEndpoint.USEast1);

                var installId = await AppCenter.GetInstallIdAsync();

                EndpointDemographic endpointDemographic = new EndpointDemographic
                {
                    AppVersion = "1.0.0",
                    Locale = "zh-hk",
                    Make = "Microsoft",
                    Model = "Xbox one s",
                    ModelVersion = "19042",
                    Platform = "xbox",
                    PlatformVersion = "19042",
                };

                PublicEndpoint publicEndpoint = new PublicEndpoint
                {
                    ChannelType = ChannelType.CUSTOM,
                    Demographic = endpointDemographic,
                    //More
                };

                string myIp = "77";//await new HttpClient().GetStringAsync("https://api.ipify.org/");

                //Maximum number of attribute keys and metric keys for each event ------ 40 per request
                Dictionary<string, string> attribute = new Dictionary<string, string>
                {
                    {"event_type", "screen" },
                    {"event_screen", "Login" },
                    {"event_category", "Profile" },
                    {"event_action", "Login" },
                    {"is_interactive", "true" },
                    {"screen_referrer", "" },   //Previous screen name
                    {"device_id", "2222-0000-0000-2222" },
                    {"event_ip", myIp },
                    {"window_width", "1920" },
                    {"window_height", "1080" },
                    {"device_platform_name", "xbox" },
                    {"location", "1" },
                    {"tagging_version", "TV 1.0.0" },
                    {"user_id", "1234567890" },
                    {"user_email", "110@aaa.aa" },
                    {"user_subscription_source", "IAP" },
                    {"device_type", "TV" },
                    {"screen_inch", "" },
                    {"system_language", "EN" },
                    {"app_language", "EN" },
                    {"app_session_id", Guid.NewGuid().ToString() },         //Initialize while App open/Browser first time open VIU after software open
                    {"activity_session_id", Guid.NewGuid().ToString() },    //Change while App go to background >= 5s , Web >= 30mins
                    {"video_player_session_id",  Guid.NewGuid().ToString()},//Initialize while each time of the Video Player screen is launched.  If the current episode is finished and the next episode is auto-played, this ID should be re-generated to another value.
                    {"network_mode", "Wifi" },
                    {"category_section_name", "" },
                    {"product_id", "123456" },    //Category ID, Series ID, Episode ID, Ad ID
                    {"grid_position_identifier", "" },   //grid_id from API
                    {"grid_position", "1" },    //Grid position
                    {"grid_title", "韩剧_Grid" },
                    {"search_keyword_1", "美好的" },
                    //{"error_code", "" },
                    //{"error_message", "" },
                    {"button_name", "Play Series" },
                    {"video_play_mode", "remote" },
                    {"resolution", "1080p" },
                    {"subtitle_status", "简体中文" },
                    {"duration", "1200" },
                    {"screen_mode", "landscape" },
                    {"timeline_at", "" },//?jindu
                    //{"ad_system", "DPF" },
                    //{"ad_width", "1280" },
                    //{"ad_height", "720" },
                    //{"ad_title", "PG - 13" },
                    //{"ad_request_url", "https://www" },
                    //{"ad_space_id",  "567890"},

                };

                var current = Package.Current;
                Event @event = new Event
                {
                    Attributes = attribute,
                    EventType = "screen",
                    AppPackageName = Package.Current.Id.Name,
                    AppTitle = Package.Current.DisplayName,
                    AppVersionCode = "10700",
                    SdkName = GetAWSSDKName(pinpoint.Config.UserAgent),
                    ClientSdkVersion = GetAWSSDKVersion(pinpoint.Config.UserAgent),
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                };
                Dictionary<string, Event> events = new Dictionary<string, Event>();
                events.Add("Events", @event);

                EventsBatch eventsBatch = new EventsBatch
                {
                    Endpoint = publicEndpoint,
                    Events = events
                };

                Dictionary<string, EventsBatch> batchItem = new Dictionary<string, EventsBatch>();
                batchItem.Add(installId.ToString(), eventsBatch);

                EventsRequest eventsRequest = new EventsRequest
                {
                    BatchItem = batchItem
                };

                PutEventsRequest putEventsRequest = new PutEventsRequest
                {
                    ApplicationId = appId,
                    EventsRequest = eventsRequest
                };

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 3));
                var res = await pinpoint.PutEventsAsync(putEventsRequest, cancellationTokenSource.Token);
                if(res != null)
                {
                    Debug.WriteLine("PinPoint.PutEventsAsync: " + DateTime.UtcNow);
                    Debug.WriteLine("EndpointItemResponse: "
                        + res?.EventsResponse?.Results[installId.ToString()]?.EndpointItemResponse.StatusCode
                        + res?.EventsResponse?.Results[installId.ToString()]?.EndpointItemResponse.Message);
                    Debug.WriteLine("EndpointItemResponse: "
                        + res?.EventsResponse?.Results[installId.ToString()]?.EventsItemResponse["Events"].StatusCode
                        + res?.EventsResponse?.Results[installId.ToString()]?.EventsItemResponse["Events"].Message);
                }
            }
            catch (AmazonPinpointException ex)
            {

            }
            catch(Exception ex)
            {

            }

        }

        private string GetAWSSDKName(string value)
        {
            //aws-sdk-dotnet-coreclr/3.7.0.2 aws-sdk-dotnet-core/3.7.0.3 .NET_Core/4.6.29713.02 OS/Microsoft_Windows
            try
            {
                string[] arr = value.Split("/");
                if (arr.Length > 1)
                {
                    string[] arr1 = arr[1].Split(" ");
                    if (arr1.Length > 1)
                        return arr1[1];
                    else
                        return "aws-sdk-dotnet-core";
                }
                else
                    return "aws-sdk-dotnet-core";
            }
            catch
            {
                return "aws-sdk-dotnet-core";
            }
        }

        private string GetAWSSDKVersion(string value)
        {
            //aws-sdk-dotnet-coreclr/3.7.0.2 aws-sdk-dotnet-core/3.7.0.3 .NET_Core/4.6.29713.02 OS/Microsoft_Windows
            try
            {
                string[] arr = value.Split("/");
                if (arr.Length > 1)
                {
                    string[] arr1 = arr[1].Split(" ");
                    if (arr1.Length > 1)
                        return arr1[0];
                    else
                        return "3.7.0";
                }
                else
                    return "3.7.0";
            }
            catch
            {
                return "3.7.0";
            }
        }
    }
}
