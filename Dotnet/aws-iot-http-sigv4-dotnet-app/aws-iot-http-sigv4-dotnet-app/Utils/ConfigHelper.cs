using System.Configuration;

namespace aws_iot_http_sigv4_dotnet_app.Utils
{
    public static class ConfigHelper
    {
        public static string ReadSetting(string key)
        {
            string result;
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                result = appSettings[key] ?? "NotFound";

            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.LogError(ex.Message);
                throw;
            }

            return result;
        }
    }
}
