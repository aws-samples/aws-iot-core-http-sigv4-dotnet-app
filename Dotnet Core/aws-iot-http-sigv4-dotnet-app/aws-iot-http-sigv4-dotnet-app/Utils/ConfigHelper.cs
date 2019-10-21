using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class ConfigHelper
    {
        public static string ReadSetting(string key)
        {
            string result;
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
                Logger.LogError(ex.Message);
                throw;
            }

            return result;
        }
    }
}