using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Configuration;
using System.Globalization;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;


namespace ShiolWinSvc
{
    class LogFile
    {
        
        static XmlLogger LogTramas = null;
        //static string temp_dir = System.AppDomain.CurrentDomain.BaseDirectory;

        static readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(LogFile));
        
        static public void saveRegistro(String text, levels logLevel)
        {
            try
            {
                
                switch (logLevel)
                {
                    case levels.debug:
                        logger.Debug(text);
                        break;
                    case levels.error:
                        logger.Error(text);
                        break;
                    case levels.info:
                        logger.Info(text);
                        break;
                    case levels.warning:
                        logger.Warn(text);
                        break;
                    default:
                        logger.Info(text);
                        break;
                }

            }
            catch (Exception ex)
            {
                EventLog ev = new EventLog();
                ev.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }

        static public void saveEvent(UniversalFrameProvider processed )
        {
            try
            {
                if (LogTramas != null)
                {
                    string strDate = LogTramas.getFileName().Mid("Tramas".Length + 1, 10).Replace("-", "");
                    DateTime date = DateTime.ParseExact(strDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                    if (date != DateTime.Today)
                    {
                        LogTramas = null;
                    }
                }

                if (LogTramas == null)
                    LogTramas = new XmlLogger("Tramas", ShiolConfiguration.Instance.Config.LogDirectory);
            
                LogTramas.saveEvent(processed);
            }
            catch (Exception ex)
            {
                EventLog ev = new EventLog();
                ev.WriteEntry(ex.Message, EventLogEntryType.Error);
            }

            
        }

        
    }
}
