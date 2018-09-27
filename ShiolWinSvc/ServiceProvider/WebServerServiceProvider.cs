using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using m.Config;
using m.Http;

using System.Collections.Generic;
using System.Xml;
using System.Globalization;
using m.Http.Extensions;


namespace ShiolWinSvc
{
  

    class WebServerServiceProvider:AbstractServiceProvider
    {
        class ServerConfig : IConfigurable
        {
            [EnvironmentVariable("httpListenPort")]
            public int ListenPort { get; set; } = ShiolConfiguration.Instance.Config.WebPort; // Default
        }

        private HttpBackend server = null;
        
        /// <SUMMARY>
        /// Start Service
        /// </SUMMARY>
        public override void StartService()
        {
            var procs = Environment.ProcessorCount;
            ThreadPool.SetMaxThreads(procs, Math.Max(1, procs / 4));
            ThreadPool.SetMaxThreads(procs * 2, Math.Max(1, procs / 2));

            LoggingProvider.Use(LoggingProvider.FileLoggingProvider);
                        
            var config = ConfigManager.Load<ServerConfig>();
            server = new HttpBackend(IPAddress.Any, config.ListenPort);
 
            var staticWebContentFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");

            var routeTable = new RouteTable(
                Route.Get("/*").With(new DirectoryInfo(staticWebContentFolder)),
                Route.Get("/month").With(Lift.ToJsonHandler(getShiolMonthFiles))
                                     .ApplyResponseFilter(Filters.GZip)
                                     .LimitRate(100),
                Route.Get("/date").With((req) => processRequest(req)),
                Route.Get("/trama").With((req) => processRequest(req))
            );
            server.Start(routeTable);
        }

        private HttpResponse processRequest(IHttpRequest req)
        {
            RestRequestParameters Parameters = new RestRequestParameters();
            HttpRequestExtensions. RestRequest(req, Parameters);
            
            object obj = new JsonResponse("");

            if (Parameters["trama"] != null)
            {
                ManagerDeviceProvider manager = new ManagerDeviceProvider();
                manager.UDeviceProvider_OnDataReceived(Parameters["trama"]);
                LogFile.saveRegistro("SentByHand: " + Parameters["trama"], levels.debug);
            }
            if (Parameters["type"] != null && Parameters["type"] == "date")
            {
                obj = getShiolEvents(Parameters["date"]);

            }
            return new JsonResponse(obj);            //new TextResponse(req.Query.GetValues(0).GetValue(0).ToString());
        }
        private void createHeaderFile(string file)
        {
            string path = Path.Combine(ShiolConfiguration.Instance.Config.LogDirectory, file + ".xml");
            using (TextWriter tw = new StreamWriter(path))
            {
                tw.WriteLine("<?xml version=\"1.0\"?>" + "\n"
                             + "<!DOCTYPE logfile [ " + "\n"
                            + "<!ENTITY Events" + "\n"
                            + "SYSTEM \"" + file + ".txt" + "\">" + "\n"
                            + " ] >" + "\n"
                            + "<logfile>" + "\n"
                            + "&Events;" + "\n"
                            + "</logfile>");
                tw.Close();
            }
        }
        private void deleteHeaderFile(string file)
        {
            string path = Path.Combine(ShiolConfiguration.Instance.Config.LogDirectory, file + ".xml");
            File.Delete(path);
        }
        private object getShiolEvents(string strDate)
        {
            List<DataFrameStructure> objs = new List<DataFrameStructure>();
            XmlValidatingReader vr = null;
            try
            {
                DateTime date = DateTime.ParseExact(strDate.Replace("-", ""), "yyyyMMdd", CultureInfo.InvariantCulture);

                //DateTime date = DateTime.Today; // DateTime.ParseExact("20180130", "yyyyMMdd", CultureInfo.InvariantCulture);
                //string fullPathToFile = Path.Combine(dir, fileName);
                string file = "-" + date.ToString("yyyy-MM-dd");
                
                createHeaderFile("Tramas" + file);

                vr = new XmlValidatingReader(new XmlTextReader(Path.Combine(ShiolConfiguration.Instance.Config.LogDirectory,"Tramas" + file + ".xml")));
                vr.ValidationType = ValidationType.None;
                vr.EntityHandling = EntityHandling.ExpandEntities;

                XmlDocument doc = new XmlDocument();
                doc.Load(vr);

                foreach (XmlElement element in doc.SelectNodes("//Event"))
                {
                    var Processed = element.LastChild;
                    var Received = element.FirstChild;

                    //DataFrameStructure uFrameProvider = XmlConvert.DeserializeObject<DataFrameStructure>(Processed.InnerXml);
                    DataFrameStructure obj = new DataFrameStructure()
                    {
                        Date = Processed["Date"].InnerText,
                        Time = Processed["Time"].InnerText,
                        UserID = Processed["UserID"].InnerText,
                        DialedNumber = Processed["DialedNumber"].InnerText,
                        Duration = Processed["Duration"].InnerText,
                        Anexo = Processed["Anexo"].InnerText,
                        Shiol = Processed["Shiol"] != null ? Processed["Shiol"].InnerText:""
                        
                    };
                    objs.Add(obj);

                }
                vr.Close();
                deleteHeaderFile("Tramas" + file);
            } catch {
                if (vr != null)
                vr.Close();
            }
            
            return objs;
        }
        public object getShiolMonthFiles()
        {
            List<string> files = new List<string>();

            DirectoryInfo dir = new DirectoryInfo(ShiolConfiguration.Instance.Config.LogDirectory);
            foreach (FileInfo flInfo in dir.GetFiles("Tramas*.txt"))
            {
                try
                {
                    string strDate = flInfo.Name.Mid(7, 10).Replace("-", "");
                    DateTime date = DateTime.ParseExact(strDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                    if (date.Month == DateTime.Today.Month)
                    {
                        files.Add(date.ToString("yyyy-MM-dd"));
                    }
                } catch
                {

                }
            }
            
            return files;
        }

        /// <SUMMARY>
        /// Stop Service
        /// </SUMMARY>
        public override void StopService()
        {
            if (server != null)
                server.Shutdown();
            server = null;
        }
        public static void Main(string[] args)
        {
            WebServerServiceProvider server = new WebServerServiceProvider();
            server.StartService();
            Console.ReadKey();
            server.StopService();
        }

    }
}
