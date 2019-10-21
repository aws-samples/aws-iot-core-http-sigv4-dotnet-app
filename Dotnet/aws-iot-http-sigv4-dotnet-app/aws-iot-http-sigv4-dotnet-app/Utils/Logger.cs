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
