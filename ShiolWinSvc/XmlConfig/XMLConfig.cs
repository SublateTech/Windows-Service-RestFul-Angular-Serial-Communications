using XmlConfig;
using System;

using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace ShiolWinSvc
{
    [Serializable]
    public class ServiceShiolConfiguration : IEquatable<ServiceShiolConfiguration>
    {
        public string CentralType { get; set; }
        public string LogDirectory { get; set; }
        public int MinCallDuration { get; set; }
        public bool SaveAllCalls { get; set; }
        public string ExcludedStrings { get; set; }
        public int MinLengthFrame { get; set; }
        public int TryReconnectEvery { get; set; }
        public int WebPort { get; set; }
        public ShiolSqlServer SqlServerConnection { get; set; }
        public ConectionConfiguration Communication { get; set; }
        public List<DataFrame> DataFrames { get; set; }

        public void Create()
        {
            CentralType = "TYPE1";
            LogDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            MinCallDuration = 20;
            ExcludedStrings = "Date,---";
            MinLengthFrame = 65;
            SaveAllCalls = true;
            TryReconnectEvery = 10;
            WebPort = 80;

            SqlServerConnection = new ShiolSqlServer();
            SqlServerConnection.ConnectionString = "user id=sa;password=server$123$;data source=.;initial catalog=Shiol_Datos_MLP_Cliente";
            SqlServerConnection.Connection = true;

            DataFrames = new List<DataFrame>();

            DataFrame dataFrame = new DataFrame();
            dataFrame.Prefix = "@";
            dataFrame.Name = "Trama Unica";
            dataFrame.ID = "1";
            dataFrame.Length = 65;



            //DataFrame
            DataFrameStructure dataFrameS = new DataFrameStructure();
            dataFrameS.Date = "0,8,MM/DD/YY";
            dataFrameS.DialedNumber = "26,13";
            dataFrameS.Anexo = "19,3";
            dataFrameS.UserID = dataFrameS.Anexo;
            dataFrameS.Time = "9,7,hh:mmtt";
            dataFrameS.Duration = "57,8,hh:mm'ss";

            dataFrame.Structure = dataFrameS;

            DataFrames.Add(dataFrame);


            ////
            dataFrame = new DataFrame();
            dataFrame.Prefix = "N";
            dataFrame.Name = "LLamada Saliente";
            dataFrame.ID = "3";
            dataFrame.Length = 65;

            //DataFrame
            dataFrameS = new DataFrameStructure();
            dataFrameS.Date = "0,8,MM/DD/YY";
            dataFrameS.DialedNumber = "26,13";
            dataFrameS.Anexo = "19,3";
            dataFrameS.UserID = dataFrameS.Anexo;
            dataFrameS.Time = "9,7,hh:mmtt";
            dataFrameS.Duration = "57,8,hh:mm'ss";

            dataFrame.Structure = dataFrameS;

            DataFrames.Add(dataFrame);

            Communication = new ConectionConfiguration();
            Communication.CentralName = "SUPERXLS";
            Communication.Conexion = "IP";
            Communication.IP = "127.0.0.1";
            Communication.IPPort = 2300;
            Communication.SerialSettings = "COM4,1200,8,None,None";
            Communication.User = "SMDR";
            Communication.Password = "PCCSMDR";
        }
        public void ClearRegistration()
        {
        }

        #region Implementation of IEquatable<ServerRegistrationInfo>

        public bool Equals(ServiceShiolConfiguration other)
        {
            if (other == null)
                return false;

            return true;

        }

        #endregion

    }
    public class ShiolSqlServer
    {
        public string ConnectionString {set; get; }
        public bool Connection { set; get; }

    }
    public class DataFrame
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Prefix { get; set; }
        public int Length { get; set; }
        public int MaxLength { get; set; }

        public DataFrameStructure Structure { get; set; }

        /*
        public List<string> test { get; set; }
        public List<string> test2 { get; set; }
        */
    }

    public class DataFrameStructure
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string UserID { get; set; }
        public string DialedNumber { get; set; }
        public string Duration { get; set; }
        public string Anexo { get; set; }
        public string Shiol { get; set; }

    }
    public class ConectionConfiguration
    {
        public string CentralName { get; set; }
        public string Conexion { get; set; }
        public string IP { get; set; }
        public int IPPort { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string SerialSettings { get; set; }
    }
    public class ShiolConfiguration : Settings
    {
        private static ShiolConfiguration instance;

        [SettingItem("ShiolConfigurationInfo")]
        public ServiceShiolConfiguration Config
        {
            get
            {
                var info = (ServiceShiolConfiguration)this["ShiolConfigurationInfo"];
                if (info == null)
                {
                    info = new ServiceShiolConfiguration();
                    this["ShiolConfigurationInfo"] = info;
                }
                return info;
            }
            set
            {
                this["ShiolConfigurationInfo"] = value;
            }
        }
        private ShiolConfiguration() { }

        public static ShiolConfiguration Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ShiolConfiguration();
                    SettingsManager.SetCurrentSettings<ShiolConfiguration>(System.AppDomain.CurrentDomain.BaseDirectory + "\\ShiolSettings.xml", null);
                    try
                    {
                        SettingsManager.Load();
                        
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.Message);
                        SettingsManager.GetSettings<ShiolConfiguration>().Config = new ServiceShiolConfiguration();
                        SettingsManager.GetSettings<ShiolConfiguration>().Config.Create();
                        SettingsManager.Save();
                    }
                    instance.Config = SettingsManager.GetSettings<ShiolConfiguration>().Config;

                }
                return instance;
            }
        }
        
    }
    
}
