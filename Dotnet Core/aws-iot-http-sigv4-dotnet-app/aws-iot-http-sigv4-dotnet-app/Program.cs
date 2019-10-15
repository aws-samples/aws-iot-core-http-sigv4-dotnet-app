using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using aws_iot_http_sigv4_dotnet_app.Utils;
using System;
using aws_iot_http_sigv4_dotnet_app.Signers;
using AWSSignatureV4_S3_Sample.Util;

namespace aws_iot_http_sigv4_dotnet_app
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    PublishMessageToAWSIoT();
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    // log the error and continue to publish
                    Logger.LogError(e.Message);
                }
            }
        }

        private static void PublishMessageToAWSIoT()
        {
            string jsonPayload = JsonHelper.GenerateRandomJsonPayload();
            var uri = new Uri("https://a2p1hwvv77f23d-ats.iot.us-east-1.amazonaws.com/topics/topic1?qos=1");
            Dictionary<string, string> headers = BuildHeaders(uri, jsonPayload);

            HttpHelpers.InvokeHttpRequest(uri, "POST", headers, jsonPayload);
        }

        private static Dictionary<string, string> BuildHeaders(Uri uri, string payload) 
        {
            byte[] contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(payload));
            string contentHashString = AWS4SignerBase.ToHexString(contentHash, true);

            var headers = new Dictionary<string, string>
            {
                {AWS4SignerBase.X_Amz_Content_SHA256, contentHashString},
                {"content-length", payload.Length.ToString()},
                {"content-type", "text/plain"}
            };

            var uriWithoutQueryString = new Uri(uri.GetLeftPart(UriPartial.Path));
            var signer = new AWS4SignerForAuthorizationHeader
            {
                EndpointUri = uriWithoutQueryString,
                HttpMethod = "POST",
                Service = "iotdevicegateway",
                Region = "us-east-1"
            };

            string AWSAccessKey = ConfigHelper.ReadSetting("accesskey");
            string AWSSecretKey = ConfigHelper.ReadSetting("secretkey");

            string queryStringWithoutLeadingQuestionMark = string.IsNullOrEmpty(uri.Query) ? string.Empty : uri.Query.Substring(1);
            var authorization = signer.ComputeSignature(headers, queryStringWithoutLeadingQuestionMark, contentHashString, AWSAccessKey, AWSSecretKey);

            // express authorization for this as a header
            headers.Add("Authorization", authorization);

            return headers;
        }
    }
}
