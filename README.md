# 1. Overview

There are multiple options available for publishing and subscribing messages with AWS IOT Core. The message broker supports the use of the MQTT protocol to publish and subscribe and the HTTPS protocol to publish. Both protocols are supported through IP version 4 and IP version 6. The message broker also supports MQTT over the WebSocket protocol.

Here is a simple table that shows various protocol and port options available for handshake with AWS IOT Core.

|No |Protocol        |Authentication     |Port    |
|---|----------------|-------------------|------- |
| 1 |MQTT            |ClientCertificate  |8883,443|
| 2 |HTTP            |ClientCertificate  |8443    |
| 3 |HTTP            |SigV4              |443     |
| 4 |MQTT+WebSocket  |SigV4              |443     |

More details are available here https://docs.aws.amazon.com/iot/latest/developerguide/protocols.html

In this post, we'll cover the option #3 of leveraging HTTP protocol with AWS Sigv4 authentication for communicating with AWS IOT Core using 
.NET and .NET core. 

# 2. AWS IOT .NET app using HTTP and AWS Sigv4 authentication
The following sub-sections 2a, 2b,2c,2d, 2e and 2f offer guidance on creating a .NET framework app that publishes messages to AWS IOT Core
using HTTP and AWS Sigv4 authentication.

## 2a. Development environment
- Windows 10 with latest updates
- Visual Studio 2017 with latest updates
- Windows Subsystem for Linux 

## 2b. Visual Studio Solution & Project

Create a console application in Visual Studio 2017 with name 'aws-iot-http-sigv4-dotnet-app'.

Add the following Nuget package references.

- log4net
- Newtonsoft.Json

Add a folder named 'Utils' and create a class 'ConfigHelper.cs' with the following implementation.

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class ConfigHelper
    {
        public static string ReadSetting(string key)
        {
            string result = "NotFound";
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                result = appSettings[key] ?? "NotFound";

            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.LogError(ex.Message);

            }

            return result;
        }
    }
}

``` 

Add a class 'HttpHelper.cs' in the folder Utils with the following implementation.

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class HttpHelper
    {
        // The Set of accepted and valid Url characters per RFC3986. Characters outside of this set will be encoded.
        const string ValidUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        public static string UrlEncode(string data, bool isPath = false)
        {

            var encoded = new StringBuilder(data.Length * 2);

            try
            {
                string unreservedChars = String.Concat(ValidUrlCharacters, (isPath ? "/:" : ""));

                foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
                {
                    if (unreservedChars.IndexOf(symbol) != -1)
                        encoded.Append(symbol);
                    else
                        encoded.Append("%").Append(String.Format("{0:X2}", (int)symbol));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);

            }
            return encoded.ToString();
        }
    }
}

```

Add a class 'JsonHelper.cs' in the folder Utils with the following implementation.

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
   public static class JsonHelper
    {

        public static string GetJsonPayload()
        {
            Random r = new Random();

            Thermostat th = new Thermostat();

            th.ThermostatID = r.Next(10000);
            th.SetPoint = r.Next(32, 100);

            th.CurrentTemperature = r.Next(32, 100);
            string jsonPayLoad = JsonConvert.SerializeObject(th);

            return jsonPayLoad;
        }

    }
}

```

Add a class 'Logger.cs' in the folder Utils with the following implementation. This is a wrapper for 'log4net'.

``` c#

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class Logger
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void LogInfo(string message)
        {
            log.Info(message);
        }

        public static void LogDebug(string message)
        {
            log.Debug(message);
        }


        public static void LogError(string message)
        {
            log.Error(message);
        }


        public static void LogFatal(string message)
        {
            log.Fatal(message);
        }


        public static void LogWarn(string message)
        {
            log.Warn(message);
        }


    }
}


```

Add a class 'Sigvutil.cs' in the Utils folder with the following implementation. This is reponsible for performing necessary heavy lifting for 
AWS Sigv4 authentication.

``` c#
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

```

Right click on the project and a class 'Thermostat.cs' with the following implementation.

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aws_iot_http_sigv4_dotnet_app
{
    class Thermostat
    {
        public int ThermostatID { get; set; }

        public int SetPoint { get; set; }

        public int CurrentTemperature { get; set; }


    }
}

```
Finally, modify the program.cs file to have the following implementation.

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using aws_iot_http_sigv4_dotnet_app.Utils;

namespace aws_iot_http_sigv4_dotnet_app
{
    class Program
    {
       
        static void Main(string[] args)
        {
            while(true)
            { 
            PublishMessageToAWSIot();
                Thread.Sleep(5000);   
            }


            
        }

         static void PublishMessageToAWSIot()
        {

            var CaCert = X509Certificate.CreateFromCertFile(@"C:\PythonIotsamples\root-CA.crt");
            string method = ConfigHelper.ReadSetting("method");
            string requesturl = ConfigHelper.ReadSetting("requesturl");

            string postData = JsonHelper.GetJsonPayload();
            
            
            Dictionary<string, string> finalHeaders = Sigv4util.GetHttpHeaderForSigv4HttpPost(postData);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturl);


            request.Method = method;
            request.ContentLength = byteArray.Length;
            request.ContentType = ConfigHelper.ReadSetting("contenttype");
            request.Headers.Add("Authorization", finalHeaders["Authorization"]);
            request.Headers.Add("X-Amz-Date", finalHeaders["X-Amz-Date"]);
            request.ClientCertificates.Add(CaCert);
                     
            

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string responseString;
            StreamReader responseReader = new StreamReader(response.GetResponseStream());

            responseString = responseReader.ReadToEnd();

            if (responseString.Contains("OK"))
            {
                Logger.LogDebug("Sucessful" + responseString);
            }

            else
            {
               
                Logger.LogDebug("Not Sucessful" + responseString);
            }

        }
    }
}

```


## 2c. App.Config file changes

Implement the following configuration in the App.Config file to address application specicfic configuration and log4net configuration.

``` XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
  </configSections>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.6.1" />
  </startup>
  <appSettings>
    <add key="method" value="POST" />
    <add key="requesturl" value="https://youriothost/topics/yourtopic?qos=1" />
   
    <add key="service" value="iotdevicegateway" />
    <add key="algorithm" value="AWS4-HMAC-SHA256" />
    <add key="ClientSettingsProvider.ServiceUri" value="" />
       <add key="region" value="us-east-1" />
    <add key="host" value="youriothost" />
    <add key="accesskey" value="youraccesskey" />
    <add key="secretkey" value="yoursecret" />
    <add key="canonicaluri" value="/topics/yourtopicname" />
    <add key="canonicalquerystring" value="qos=1" />
    <add key="contenttype" value="application/json" />



  </appSettings>
  <system.web>
    <membership defaultProvider="ClientAuthenticationMembershipProvider">
      <providers>
        <add name="ClientAuthenticationMembershipProvider" type="System.Web.ClientServices.Providers.ClientFormsAuthenticationMembershipProvider, System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" serviceUri="" />
      </providers>
    </membership>
    <roleManager defaultProvider="ClientRoleProvider" enabled="true">
      <providers>
        <add name="ClientRoleProvider" type="System.Web.ClientServices.Providers.ClientRoleProvider, System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" serviceUri="" cacheTimeout="86400" />
      </providers>
    </roleManager>
  </system.web>

  <log4net>
    <root>
      <level value="ALL" />
      <appender-ref ref="MyAppender" />
      <appender-ref ref="RollingFileAppender" />
    </root>
    <appender name="MyAppender" type="log4net.Appender.ConsoleAppender">
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date %level %logger - %message%newline" />
      </layout>
    </appender>
    <appender name="MyFileAppender" type="log4net.Appender.FileAppender">
      <file value="application.log" />
      <appendToFile value="true" />
      <lockingModel type="log4net.Appender.FileAppender+MinimalLock" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date %level %logger - %message%newline" />
      </layout>
    </appender>
    <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
      <file value="C:\\SundarTest2\\rolling.log" />
      <appendToFile value="true" />
      <rollingStyle value="Size" />
      <maxSizeRollBackups value="5" />
      <maximumFileSize value="10MB" />
      <staticLogFileName value="true" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date [%thread] %level %logger - %message%newline" />
      </layout>
    </appender>
  </log4net>
  <system.net>
    <settings>
      <httpWebRequest useUnsafeHeaderParsing="true" />
    </settings>
  </system.net>
</configuration>

```

## 2d. AssemblyInfo.cs file changes for log4net configuration

Add the following line in the AssemblyInfo.cs file .

``` c#
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
```

This is to enable log4net to refer app.config file for all the logging related configuration.

## 2e. Completed and working code

The completed and working code for this solution is available in the folder 'Dotnet' of this repository. You can also refer that directly.

## 2f. Compile, Run and Verify messages sent to AWS IOT Core
Compile and run the solution. You should see that messages are published successfully from .NET app to AWS Iot Core.

<p align="center">
<img src="/images/pic1.JPG">
</p>

You should also see the messages getting published in the AWS Iot Test Console.

<p align="center">
<img src="/images/pic2.png">
</p>


# 3. AWS IOT .NET app using HTTP and AWS Sigv4 authentication
The folowing sub-sections 3a,3b,3c,3d,3e and 3f offer guidance on implementing a .NET core app that publishes messages to AWS IOT Core using
HTTP and AWS Sigv4 authentication.

## 3a. Development environment
- Mac OS with latest updates 
- .NET Core 2.1 or higher
- Visual Studio for Mac

## 3b. Create Console application project in Dotnetcore

Create a .NET core console project in Visual Studio for Mac and name it as 'aws-iot-http-sigv4-dotnet-app'.

Add the following nuget package references.

- log4net
- Microsoft.Extensions.Configuration 
- Microsoft.Extensions.Configuration.FileExtensions
- Microsoft.Extensions.Configuration.Json
- Newtonsoft.Json

Add a class 'Thermostat.cs' with the following implementation.

``` c#
using System;
namespace aws_iot_http_sigv4_dotnet_app
{
    class Thermostat
    {
        public int ThermostatID { get; set; }

        public int SetPoint { get; set; }

        public int CurrentTemperature { get; set; }


    }
}

```
Create a folder called 'Utils' and add a class 'Logger.cs' with the following implementation. The Logger.cs is a wrapper over lognet for logging. 

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using log4net;
using log4net.Config;
namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class Logger
    {
        private static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static bool IsLog4netConfigured;

        public static void LogInfo(string message)
        {
            if (!IsLog4netConfigured)
            {
                ConfigureLog4Net();
            }
            log.Info(message);
        }

        public static void LogDebug(string message)
        {
            if (!IsLog4netConfigured)
            {
                ConfigureLog4Net();
            }

            log.Debug(message);
        }


        public static void LogError(string message)
        {
            if (!IsLog4netConfigured)
            {
                ConfigureLog4Net();
            }
            log.Error(message);
        }


        public static void LogFatal(string message)
        {
            if (!IsLog4netConfigured)
            {
                ConfigureLog4Net();
            }

            log.Fatal(message);
        }


        public static void LogWarn(string message)
        {
            if (!IsLog4netConfigured)
            {
                ConfigureLog4Net();
            }
            log.Warn(message);
        }

        private static void ConfigureLog4Net()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            IsLog4netConfigured = true;
        }

    }
}

```

Add a class 'ConfigHelper.cs' with the following implementation in the Utils folder. This is a utility class to read values for configuration parameters from the 'appettings.json' file.

``` c#
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;


namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class ConfigHelper
    {
        public static string ReadSetting(string key)
        {
            string result = "NotFound";

            try
            {


                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", true, true)
              .Build();

                result = config[key];

            }
            catch (Exception ex)
            {

                Logger.LogDebug(ex.Message);
            }
            return result;
        }
    }
}

```

Add a class 'HttpHelper.cs' with the following implementation in the Utils folder.

``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class HttpHelper
    {
        // The Set of accepted and valid Url characters per RFC3986. Characters outside of this set will be encoded.
        const string ValidUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        public static string UrlEncode(string data, bool isPath = false)
        {

            var encoded = new StringBuilder(data.Length * 2);

            try
            {
                string unreservedChars = String.Concat(ValidUrlCharacters, (isPath ? "/:" : ""));

                foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
                {
                    if (unreservedChars.IndexOf(symbol) != -1)
                        encoded.Append(symbol);
                    else
                        encoded.Append("%").Append(String.Format("{0:X2}", (int)symbol));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);

            }
            return encoded.ToString();
        }
    }
}

```

Add a class 'JsonHelper.cs' in the Utils folder. This is responsible for serializing Thermostat object to json.

``` c#

using System;
using Newtonsoft.Json;
namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class JsonHelper
    {

        public static string GetJsonPayload()
        {
            Random r = new Random();

            Thermostat th = new Thermostat();

            th.ThermostatID = r.Next(10000);
            th.SetPoint = r.Next(32, 100);

            th.CurrentTemperature = r.Next(32, 100);
            string jsonPayLoad = JsonConvert.SerializeObject(th);

            return jsonPayLoad;
        }

    }
}


```

Add a class 'Sigv4Util.cs' in the Utils folder with the following implementation. This is responsible for performing necesary heavy lifting for AWS Sigv4 authentication.
It returns the signed Http Header to be used with publishing messages to AWS Iot Core using Http protocol.

``` c#
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



        public static Dictionary<string, string> GetHttpHeaderForSigv4HttpPost(string body)
        {
            Dictionary<string, string> finalHeaders = null;
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



                string authorizationHeader = algorithm + " " + "Credential=" + accessKey + "/" + credentialScope + ", " + "SignedHeaders=" + signedHeaders + ", " + "Signature=" + signatureString;

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


```

Now we have created all the classes under Utils folder. Let's add the necessary logic in program.cs.

``` c#

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using aws_iot_http_sigv4_dotnet_app.Utils;

namespace aws_iot_http_sigv4_dotnet_app
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                PublishMessageToAWSIot();
                Thread.Sleep(5000);
            }
        }

        static void PublishMessageToAWSIot()
        {


            var CaCert = X509Certificate.CreateFromCertFile(@"AmazonRootCA1.pem");


            var CaCert2 = new X509Certificate2(CaCert);
            string method = ConfigHelper.ReadSetting("method");
            string requesturl = ConfigHelper.ReadSetting("requesturl");

            string postData = JsonHelper.GetJsonPayload();


            Dictionary<string, string> finalHeaders = Sigv4util.GetHttpHeaderForSigv4HttpPost(postData);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturl);


            request.Method = method;
            request.ContentLength = byteArray.Length;
            request.ContentType = ConfigHelper.ReadSetting("contenttype");
            request.Headers.Add("Authorization", finalHeaders["Authorization"]);
            request.Headers.Add("X-Amz-Date", finalHeaders["X-Amz-Date"]);


            request.ClientCertificates.Add(CaCert2);



            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string responseString;
            StreamReader responseReader = new StreamReader(response.GetResponseStream());

            responseString = responseReader.ReadToEnd();

            if (responseString.Contains("OK"))
            {
                Logger.LogDebug("Sucessful" + responseString);
            }

            else
            {

                Logger.LogDebug("Not Sucessful" + responseString);
            }

        }
    }
}

```


## 3c. AppSettings.json file configuration

The .NET core console applications does not rely on 'app.config' for application configuration, unlike .NET framework console applications. It needs 'appsettings.json', instead of 'app.config' file. Add an 'appsettings.json' like below and replace relevant details for your applications.

``` json

{
  "method": "POST",
  "requesturl": "https://youriothostname.iot.region.amazonaws.com/topics/yourtopicpath?qos=1",
  "service": "iotdevicegateway",
  "algorithm": "AWS4-HMAC-SHA256",
   "region": "us-east-1",
  "host": "youriothostname.iot.us-east-1.amazonaws.com",
  "accesskey": "youraccesskey",
  "secretkey": "yoursecretkey",
  "canonicaluri": "/topics/yourtopicpath",
  "canonicalquerystring": "qos=1",
  "contenttype": "application/json"

}

```


## 3d. Configuration for Log4net

Implement the following 'log4net.config' file to configure Log4net to perform logging for this application.

``` XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
  </configSections>  
  <log4net>
    <root>
      <level value="ALL" />
      <appender-ref ref="MyAppender" />
      <appender-ref ref="RollingFileAppender" />
    </root>
    <appender name="MyAppender" type="log4net.Appender.ConsoleAppender">
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date %level %logger - %message%newline" />
      </layout>
    </appender>
    <appender name="MyFileAppender" type="log4net.Appender.FileAppender">
      <file value="applicationnew.log" />
      <appendToFile value="true" />
      <lockingModel type="log4net.Appender.FileAppender+MinimalLock" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date %level %logger - %message%newline" />
      </layout>
    </appender>
    <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
      <file value="rolling.log" />
      <appendToFile value="true" />
      <rollingStyle value="Size" />
      <maxSizeRollBackups value="5" />
      <maximumFileSize value="10MB" />
      <staticLogFileName value="true" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date [%thread] %level %logger - %message%newline" />
      </layout>
    </appender>
  </log4net>
</configuration>

```


## 3e. Completed and working code
The completed and working code for this solution is available in the folder 'Dotnet Core' of this repository. You can also refer that.


## 3f. Compile, run and verify
Compile and run the solution. You should see that messages are published successfully from .NET app to AWS Iot Core.

<p align="center">
<img src="/images/pic3.png">
</p>

You should also see messages getting published in AWS Iot Test Console.

<p align="center">
<img src="/images/pic4.png">
</p>


## 4. Conclusion

In this post, we have developed a .NET framework app that publishes messages to AWS Iot Core using HTTP protocol and AWS Sigv4 authentication. We also developed a .NET core equivalent of the same in the later part of this.
This completes the post on building .NET framework and .NET core reference implementation for AWS Iot Core using Http and Sigv4.

## 5. License Summary

This sample code is made available under the MIT-0 license. See the LICENSE file.


