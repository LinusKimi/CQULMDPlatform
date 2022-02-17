using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys
{
    public class Logger
    {
        private static NLog.Logger _logger;

        public Logger() => _logger = NLog.LogManager.GetCurrentClassLogger();

        public void shutDown() => NLog.LogManager.Shutdown();

        public void writeToConsole(string msg) => _logger.Debug(msg);

        public void writeToFile(string msg) => _logger.Info(msg);
    }
}