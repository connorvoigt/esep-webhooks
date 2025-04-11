using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using System.Text;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        // Reuse the HttpClient for efficiency.
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// This Lambda function processes GitHub webhook events, extracts the issue URL, 
        /// and posts a message to Slack via the SLACK_URL environment variable.
        /// </summary>
        /// <param name="input">The GitHub webhook JSON payload</param>
        /// <param name="context">The Lambda context for logging.</param>
        /// <returns>A string indicating the result of the operation</returns>
        public async Task<string> FunctionHandler(JsonElement input, ILambdaContext context)
        {
            context.Logger.LogLine("Received payload: " + input.ToString());

            try
            {
                // Parse and extract the issue URL from the JSON object.
                if (!input.TryGetProperty("issue", out JsonElement issueElement) ||
                    !issueElement.TryGetProperty("html_url", out JsonElement htmlUrlElement))
                {
                    context.Logger.LogLine("Issue URL not found in payload.");
                    return "Issue URL not found.";
                }

                string issueUrl = htmlUrlElement.GetString();
                context.Logger.LogLine($"Extracted issue URL: {issueUrl}");

                // Retrieve the Slack webhook URL from the environment variable.
                string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    context.Logger.LogLine("Environment variable SLACK_URL is not set.");
                    return "SLACK_URL not set.";
                }

                // Construct the Slack message payload.
                var slackPayload = new { text = $"New GitHub Issue: {issueUrl}" };
                string jsonPayload = JsonSerializer.Serialize(slackPayload);
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Post the payload to Slack.
                HttpResponseMessage response = await httpClient.PostAsync(slackUrl, content);
                context.Logger.LogLine("Response from Slack: " + response.StatusCode);

                return response.IsSuccessStatusCode ? "Success" : "Failed to post message to Slack.";
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("Error: " + ex.Message);
                return $"Error: {ex.Message}";
            }
        }
    }
}