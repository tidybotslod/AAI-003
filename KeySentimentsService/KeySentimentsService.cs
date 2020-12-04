using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.Net;
using System.Net.Http;

namespace Company.Function
{
    public static class KeySentimentsService
    {
        [FunctionName("KeySentiments")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function for KeySentiments.");
            //
            // Read in the content of the message, load into a single string
            String input = await req.Content.ReadAsStringAsync();
            //
            // Load up the service in a Task, this could be long running
            AAI.KeySentiments textAnalytics;
            textAnalytics = await Task.Run(() =>
            {
                return new AAI.KeySentiments();
            });
            //
            if (textAnalytics == null ||
                textAnalytics.AzureTextAnalyticsService == null)
            {
                log.LogInformation("KeySentiments: unable to create connection to Azure cognitive services.");
                return req.CreateResponse(HttpStatusCode.ServiceUnavailable, "Unable to connect to cognitive service");
            }
            //
            // Create a stream to build response content into. (Don't put into a 'using' block, framework manages lifetime and calls dispose if returned)
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                MemoryStream ms = new MemoryStream();
                response.Content = new StreamContent(ms);
                StreamWriter writer = new StreamWriter(ms);
                //
                // Call to get the sentiments could be long running, use a Task.
                await Task.Run(() =>
                {
                    textAnalytics.Sentiment(input, writer);
                });
                ms.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception)
            {
                response.Dispose();
                return req.CreateResponse(HttpStatusCode.InternalServerError, "Call to congitive service failed.");
            }
            return response;
        }
    }
}
