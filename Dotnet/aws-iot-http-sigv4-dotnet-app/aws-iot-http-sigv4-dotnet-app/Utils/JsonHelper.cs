using Newtonsoft.Json;
using System;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class JsonHelper
    {
        public static string GenerateRandomJsonPayload()
        {
            Random r = new Random();

            Thermostat thermostat = new Thermostat();

            thermostat.ThermostatID = r.Next(10000);
            thermostat.SetPoint = r.Next(32, 100);

            thermostat.CurrentTemperature = r.Next(32, 100);
            return JsonConvert.SerializeObject(thermostat);
        }
    }
}
