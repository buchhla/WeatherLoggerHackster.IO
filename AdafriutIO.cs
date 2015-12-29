using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using Windows.Devices.Geolocation;



namespace WeatherLogger
{
    class AdafruitIO
    {
        private string AIO = "ENTER YOUR CODE HERE";
        //this is not needed right now as I am doing a simple http get, when RestSharp works for UWP I will re-write this.
        //private string Feed = "ENTER FEED ID HERE";

        public async void getFeeds()
        {
            // Create a client
            HttpClient httpClient = new HttpClient();

            // Add a new Request Message
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://io.adafruit.com/api/feeds");

            // Add our custom headers
            requestMessage.Headers.Add("X-AIO-Key", AIO);
            
            // Send the request to the server
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

            // Just as an example I'm turning the response into a string here
            string responseAsString = await response.Content.ReadAsStringAsync();
            Debug.WriteLine(responseAsString);
        }

        public async void sendData(WeatherData wd)
        {
            // Create a client
            HttpClient httpClient = new HttpClient();

            // Add a new Request Message
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://io.adafruit.com/api/groups/weather/send.json?temperature=" + wd.TemperatureinF + "&pressure=" + wd.Pressureinmb );

            // Add our custom headers
            requestMessage.Headers.Add("X-AIO-Key", AIO);

            // Send the request to the server
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

            // Just as an example I'm turning the response into a string here
            string responseAsString = await response.Content.ReadAsStringAsync();
            //Debug.WriteLine(responseAsString);
 
            }

        }

    }
