using System;
using System.IO;

namespace OpenNos.Core.Helper
{
    public class LogHelper
    {
        public enum LogType
        {
            Command = 1
        }

        #region Members

        private object _lockObject = new object();

        private static LogHelper _instance;

        #endregion

        #region Properties

        public static LogHelper Instance => _instance ?? (_instance = new LogHelper());

        #endregion

        #region Methods

        private string Now() => DateTime.Now.ToString("MM/dd/yyyy hh:mm tt").Trim();

        public void Log(LogType logType, object sender, string contents)
        {
            lock (_lockObject)
            {
                switch (logType)
                {
                    case LogType.Command:
                        File.AppendAllText("C:\\CommandLog.txt", $"[{Now()}] <COMMAND> {sender.ToString()}, Command = {contents}\n");
                        break;
                }
            }
        }

        #endregion
    }
}

