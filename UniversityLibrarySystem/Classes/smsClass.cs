using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace Classes
{
    /// <summary>
    /// SmsSender - URLs externalized to AWS Systems Manager Parameter Store.
    /// Load SMS API URL and key from environment variables or AWS SSM Parameter Store at runtime.
    /// </summary>
    public class SmsSender
    {
        private readonly string apiKey;
        private readonly string apiUrl;

        public SmsSender()
        {
            // Load from environment variables (set from AWS SSM Parameter Store at startup)
            this.apiKey = Environment.GetEnvironmentVariable("SMS_API_KEY") ?? "";
            this.apiUrl = Environment.GetEnvironmentVariable("SMS_API_URL") ?? "https://api.sms.ir/v1/send";
        }

        public async Task<string> SendSmsAsync(string phoneNumber, string message)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var smsData = new
                {
                    Mobile = phoneNumber,
                    Message = message
                };

                var json = JsonSerializer.Serialize(smsData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
        }

        /*
        SmsSender smsSender = new SmsSender();
        // SMS_API_KEY and SMS_API_URL are loaded from environment variables
        // which are populated from AWS SSM Parameter Store at application startup.

        string phoneNumber = "09123456789";
        string message = "Hello, this is a test message.";

        var result = smsSender.SendSmsAsync(phoneNumber, message).Result;         
         */
    }

    public class SmsIr
    {
        private readonly string apiKey;
        private readonly string apiUrl;

        public SmsIr(string apiKey)
        {
            this.apiKey = apiKey;
            // Load verify URL from environment variable (set from AWS SSM Parameter Store)
            this.apiUrl = Environment.GetEnvironmentVariable("SMS_VERIFY_API_URL") ?? "https://api.sms.ir/v1/send/verify";
        }

        public async Task<string> SendVerifyAsync(string mobile, int templateId, VerifySendParameterModel[] parameters)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var model = new VerifySendModel
                {
                    Mobile = mobile,
                    TemplateId = templateId,
                    Parameters = parameters
                };

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
        }
    }
    /*
            string apiKey = Environment.GetEnvironmentVariable("SMS_API_KEY");
            SmsIr smsIr = new SmsIr(apiKey);

            string mobile = "9120000000";
            int templateId = 123456;
            VerifySendParameterModel[] parameters = new VerifySendParameterModel[]
            {
                new VerifySendParameterModel { Name = "CODE", Value = "1234" }
            };

            var result = smsIr.SendVerifyAsync(mobile, templateId, parameters).Result;
            Response.Write(result);*/
    #region Models
    public class VerifySendParameterModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class VerifySendModel
    {
        public string Mobile { get; set; }
        public int TemplateId { get; set; }
        public VerifySendParameterModel[] Parameters { get; set; }
    }

    #endregion
}
