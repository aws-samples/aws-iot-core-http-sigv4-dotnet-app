using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json;


namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class Sigv4util
    {
        public const string ISO8601BasicFormat = "yyyyMMddTHHmmssZ";
        public const string DateStringFormat = "yyyyMMdd";
        public const string EmptyBodySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        public static HashAlgorithm CanonicalRequestHashAlgorithm = HashAlgorithm.Create("SHA-256");
        // the name of the keyed hash algorithm used in signing
        public const string HmacSha256 = "HMACSHA256";
        public const string XAmzSignature = "X-Amz-Signature";




        private static byte[] HmacSHA256(String data, byte[] key)
        {
            String algorithm = "HmacSHA256";
            KeyedHashAlgorithm keyHashAlgorithm = KeyedHashAlgorithm.Create(algorithm);
            keyHashAlgorithm.Key = key;

            
            return keyHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static byte[] ComputeKeyedHash(string algorithm, byte[] key, byte[] data)
        {
            var kha = KeyedHashAlgorithm.Create(algorithm);
            kha.Key = key;
            return kha.ComputeHash(data);
        }

        public static string ToHexString(byte[] data, bool lowerCase)
        {
            StringBuilder stringBuilder = new StringBuilder();

            try
            {
                for (var i = 0; i < data.Length; i++)
                {
                    stringBuilder.Append(data[i].ToString(lowerCase ? "x2" : "X2"));
                }

            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex.Message);
            }

            return stringBuilder.ToString();
        }

        private static byte[] getSignatureKey(String key, String dateStamp, String regionName, String serviceName)
        {
            byte[] kSecret = Encoding.UTF8.GetBytes(("AWS4" + key).ToCharArray());
            byte[] kDate = HmacSHA256(dateStamp, kSecret);
            byte[] kRegion = HmacSHA256(regionName, kDate);
            byte[] kService = HmacSHA256(serviceName, kRegion);
            byte[] kSigning = HmacSHA256("aws4_request", kService);

            return kSigning;
        }

        

        public static Dictionary<string,string> GetHttpHeaderForSigv4HttpPost(string body)
        {
            Dictionary<string, string> finalHeaders=null;
            try
            {

                DateTime requestDateTime = DateTime.UtcNow;
                string datetime = requestDateTime.ToString(ISO8601BasicFormat, CultureInfo.InvariantCulture);

               
                
                var date = requestDateTime.ToString(DateStringFormat, CultureInfo.InvariantCulture);

                string host = ConfigHelper.ReadSetting("host");
                string region = ConfigHelper.ReadSetting("region");
                string accessKey = ConfigHelper.ReadSetting("accesskey");
                string secretKey = ConfigHelper.ReadSetting("secretkey");

                string method = ConfigHelper.ReadSetting("method");

                string canonicalUri = ConfigHelper.ReadSetting("canonicaluri");

                string service = ConfigHelper.ReadSetting("service");

                string algorithm = ConfigHelper.ReadSetting("algorithm");
                string contentType = ConfigHelper.ReadSetting("contenttype");

                string credentialScope = date + "/" + region + "/" + service + "/" + "aws4_request";
                string canonicalQuerystring = ConfigHelper.ReadSetting("canonicalquerystring");

                string signedHeaders = "content-type;host;x-amz-date";
               

                string canonicalHeaders = "content-type:" + contentType + "\n" + "host:" + host + "\n" + "x-amz-date:" + datetime + "\n";
                                
                 var contentHashString = Sigv4util.GenerateSHA256HashWithoutKey(body);


                var canonicalRequest = method + "\n" + canonicalUri + "\n" + canonicalQuerystring + "\n" + canonicalHeaders + "\n" + signedHeaders + "\n" + contentHashString;

                
                string byteString = Sigv4util.GenerateSHA256HashWithoutKey(canonicalRequest);
                

                
                var stringToSign = algorithm + "\n" + datetime + "\n" + credentialScope + "\n" + byteString;
                KeyedHashAlgorithm keyedHashAlgorithm = KeyedHashAlgorithm.Create(HmacSha256);

                keyedHashAlgorithm.Key = getSignatureKey(secretKey, date, region, service);

                var signingKey = keyedHashAlgorithm.Key;

                var signature = ComputeKeyedHash(HmacSha256, signingKey, Encoding.UTF8.GetBytes(stringToSign));


                var signatureString = ToHexString(signature, true);
  
                

                string authorizationHeader = algorithm + " "+ "Credential=" + accessKey + "/" + credentialScope + ", " + "SignedHeaders=" + signedHeaders + ", " + "Signature=" + signatureString;

                finalHeaders = new Dictionary<string, string>();
                finalHeaders.Add("Content-Type", contentType);
                finalHeaders.Add("X-Amz-Date", datetime);
                finalHeaders.Add("Authorization", authorizationHeader);

                

            }

            catch (Exception ex)
            {
                Logger.LogError(ex.Message);

            }

            return finalHeaders;



        }

        public static string GenerateSHA256HashWithoutKey(string body)
        {

            SHA256 sHA256 = SHA256.Create();

            byte[] bytes = sHA256.ComputeHash(Encoding.UTF8.GetBytes(body));

            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            string result = builder.ToString();

            return result;
        }


        
    }
}
