using System;
using System;
using System.Diagnostics;
using System.IO;
using System.IO;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Company.Function {
    public static class HttpTriggerCSharp {

        [FunctionName ("HttpTriggerCSharp")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log) {
            var apiUrl = "https://api.videoindexer.ai";
            var accountId = "095a3160-6af8-4fc6-8d36-2879b7e5221d";
            // "436182c5-6687-44e5-aaaf-57142645bb7e";
            var location = "trial";
            var apiKey = "01c3fe9e9c134addb685912d1c771efc";

            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler ();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient (handler);
            client.DefaultRequestHeaders.Add ("Ocp-Apim-Subscription-Key", apiKey);

            // obtain account access token
            var accountAccessTokenRequestResult = client.GetAsync ($"{apiUrl}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true").Result;
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync ().Result.Replace ("\"", "");

            client.DefaultRequestHeaders.Remove ("Ocp-Apim-Subscription-Key");

            // upload a video
            var content = new MultipartFormDataContent ();
            Console.WriteLine ("Uploading...");
            // get the video from URL
            // var videoUrl = "https://www.w3schools.com/html/mov_bbb.mp4"; // replace with the video URL

            // as an alternative to specifying video URL, you can upload a file.
            // remove the videoUrl parameter from the query string below and add the following lines:
            FileStream video = File.OpenRead (@"C:/Users/aewt/Downloads/t_video5298516770129183463.mp4");
            byte[] buffer = new byte[video.Length];
            video.Read (buffer, 0, buffer.Length);
            content.Add (new ByteArrayContent (buffer));

            var uploadRequestResult = client.PostAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name=some_name&description=some_description&privacy=private&partition=some_partition", content).Result;
            var uploadResult = uploadRequestResult.Content.ReadAsStringAsync ().Result;

            // get the video id from the upload result
            var videoId = JsonConvert.DeserializeObject<dynamic> (uploadResult) ["id"];
            Console.WriteLine ("Uploaded");
            Console.WriteLine ("Video ID: " + videoId);

            // obtain video access token            
            client.DefaultRequestHeaders.Add ("Ocp-Apim-Subscription-Key", apiKey);
            var videoTokenRequestResult = client.GetAsync ($"{apiUrl}/auth/{location}/Accounts/{accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
            var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync ().Result.Replace ("\"", "");

            client.DefaultRequestHeaders.Remove ("Ocp-Apim-Subscription-Key");

            var videoGetIndexResult = "";

            // wait for the video index to finish
            while (true) {
                Thread.Sleep (10000);

                var videoGetIndexRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync ().Result;

                var processingState = JsonConvert.DeserializeObject<dynamic> (videoGetIndexResult) ["state"];

                Console.WriteLine ("");
                Console.WriteLine ("State:");
                Console.WriteLine (processingState);

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing") {
                    Console.WriteLine ("");
                    //Console.WriteLine ("Full JSON:");
                    //Console.WriteLine (videoGetIndexResult);
                    break;
                }
            }

            // search for the video
            var searchRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/Search?accessToken={accountAccessToken}&id={videoId}").Result;
            var searchResult = searchRequestResult.Content.ReadAsStringAsync ().Result;
            Console.WriteLine ("");
            Console.WriteLine ("Search:");
            Console.WriteLine (searchResult);

            // get insights widget url
            var insightsWidgetRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?accessToken={videoAccessToken}&widgetType=Keywords&allowEdit=true").Result;
            var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
            Console.WriteLine ("Insights Widget url:");
            Console.WriteLine (insightsWidgetLink);

            // get player widget url
            var playerWidgetRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?accessToken={videoAccessToken}").Result;
            var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
            Console.WriteLine ("");
            Console.WriteLine ("Player Widget url:");
            Console.WriteLine (playerWidgetLink);

            // log.LogInformation ("C# HTTP trigger function processed a request.");

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader (req.Body).ReadToEndAsync ();
            // dynamic data = JsonConvert.DeserializeObject (requestBody);
            // name = name ?? data?.name;

            // return name != null ?
            //     (ActionResult) new OkObjectResult ($"Hello, {name}") :
            //     new BadRequestObjectResult ("Please pass a name on the query string or in the request body");

            // job is finished
            // if (processingState != "Uploaded" && processingState != "Processing") {
            //     Console.WriteLine ("");
            //     Console.WriteLine ("Full JSON:");
            //     Console.WriteLine (videoGetIndexResult);

            // }

            dynamic items = JObject.Parse (videoGetIndexResult);

            dynamic values = new JArray ();
            foreach (var item in items.summarizedInsights.sentiments) {
                dynamic attr = new JObject ();
                attr.name = item.sentimentKey;
                attr.value = item.seenDurationRatio;
                values.Add (attr);
            }

            dynamic obj = new JObject ();
            obj.category = "sentiments";
            obj.values = values;

            // dynamic sentiments = JObject.Parse (items.transcript.summarizedInsights);
            // string obj = @"{'sentiments':" + sentiments + "}";
            // Console.WriteLine (obj);

            return new OkObjectResult (obj);
        }
    }
}