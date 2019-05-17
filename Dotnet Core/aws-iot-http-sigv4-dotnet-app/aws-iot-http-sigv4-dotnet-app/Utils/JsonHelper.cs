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
