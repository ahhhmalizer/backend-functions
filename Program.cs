using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace HackathonDD {
    class Program {
        static void Main (string[] args) {
            var apiUrl = "https://api.videoindexer.ai";
            var accountId = "436182c5-6687-44e5-aaaf-57142645bb7e";
            var location = "trial";
            var apiKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJBY2NvdW50SWQiOiI0MzYxODJjNS02Njg3LTQ0ZTUtYWFhZi01NzE0MjY0NWJiN2UiLCJBbGxvd0VkaXQiOiJGYWxzZSIsIkV4dGVybmFsVXNlcklkIjoiQjQyQ0RFNDcwMUI2NDU1MjkzMkVEMUQ1MzBERjE3M0MiLCJVc2VyVHlwZSI6Ik1pY3Jvc29mdENvcnBBYWQiLCJpc3MiOiJodHRwczovL3d3dy52aWRlb2luZGV4ZXIuYWkvIiwiYXVkIjoiaHR0cHM6Ly93d3cudmlkZW9pbmRleGVyLmFpLyIsImV4cCI6MTU1NTA5NjAxMywibmJmIjoxNTU1MDkyMTEzfQ.bDXCemII5ghl4ig7Y-_-6TkjMdKhoTzEoVeDKzF7hv0";

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
            Debug.WriteLine ("Uploading...");
            // get the video from URL
            var videoUrl = "https://www.youtube.com/watch?v=7KKcbcXFNgM"; // replace with the video URL

            // as an alternative to specifying video URL, you can upload a file.
            // remove the videoUrl parameter from the query string below and add the following lines:
            //FileStream video =File.OpenRead(Globals.VIDEOFILE_PATH);
            //byte[] buffer =newbyte[video.Length];
            //video.Read(buffer, 0, buffer.Length);
            //content.Add(newByteArrayContent(buffer));

            var uploadRequestResult = client.PostAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name=some_name&description=some_description&privacy=private&partition=some_partition&videoUrl={videoUrl}", content).Result;
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

            // wait for the video index to finish
            while (true) {
                Thread.Sleep (10000);

                var videoGetIndexRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync ().Result;

                var processingState = JsonConvert.DeserializeObject<dynamic> (videoGetIndexResult) ["state"];

                Debug.WriteLine ("");
                Debug.WriteLine ("State:");
                Debug.WriteLine (processingState);

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing") {
                    Debug.WriteLine ("");
                    Debug.WriteLine ("Full JSON:");
                    Debug.WriteLine (videoGetIndexResult);
                    break;
                }
            }

            // search for the video
            var searchRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/Search?accessToken={accountAccessToken}&id={videoId}").Result;
            var searchResult = searchRequestResult.Content.ReadAsStringAsync ().Result;
            Debug.WriteLine ("");
            Debug.WriteLine ("Search:");
            Debug.WriteLine (searchResult);

            // get insights widget url
            var insightsWidgetRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?accessToken={videoAccessToken}&widgetType=Keywords&allowEdit=true").Result;
            var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
            Debug.WriteLine ("Insights Widget url:");
            Debug.WriteLine (insightsWidgetLink);

            // get player widget url
            var playerWidgetRequestResult = client.GetAsync ($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?accessToken={videoAccessToken}").Result;
            var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
            Debug.WriteLine ("");
            Debug.WriteLine ("Player Widget url:");
            Debug.WriteLine (playerWidgetLink);
        }
    }
}