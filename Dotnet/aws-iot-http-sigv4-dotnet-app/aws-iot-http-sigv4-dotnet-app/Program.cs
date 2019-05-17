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
