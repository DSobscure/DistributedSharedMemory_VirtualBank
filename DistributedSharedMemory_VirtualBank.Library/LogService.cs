namespace DistributedSharedMemory_VirtualBank.Library
{
    public class LogService
    {
        private static LogService instance;
        public static LogHandler Info { get { return instance.infoMethod; } }
        public static LogHandler Warning { get { return instance.warningMethod; } }
        public static LogHandler Error { get { return instance.errorMethod; } }

        public static void InitialService(LogHandler infoMethod, LogHandler warningMethod, LogHandler errorMethod)
        {
            instance = new LogService();
            instance.infoMethod = infoMethod;
            instance.warningMethod = warningMethod;
            instance.errorMethod = errorMethod;
        }

        public delegate void LogHandler(string message);

        private LogHandler infoMethod;
        private LogHandler warningMethod;
        private LogHandler errorMethod;
    }
}
