using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Kajabity.Tools.Java;
using System.IO;

namespace Logger
{
    class PropertiesWrapper
    {
        private JavaProperties props;
        private String propertiesFile = "logger.properties";

        public PropertiesWrapper()
        {
            initProps();
        }

        private void initProps()
        {
            Hashtable hash = new Hashtable();
            hash.Add("Directory", "c:/temp");
            hash.Add("BaseFileName", "logFile");
           // hash.Add("LogFileName", "logFile");
            hash.Add("LogFile", "logFile");
            hash.Add("FileSize", 1000);
            hash.Add("MaxSize", 1000);
            hash.Add("MaxFiles", 100);
            hash.Add("NumFiles", 0);
            hash.Add("LastRollover", DateTime.Now);
            //private HashSet<String> loggerClasses = new HashSet<String>();
            //private HashSet<String> loggerEnabled = new HashSet<String>();
            hash.Add("LEVEL", Logger.LEVEL.INFO.ToString());
            hash.Add("ROLLOVER", Logger.ROLLOVER.TIME.ToString());
            hash.Add("FileCount", 0);
            hash.Add("lastTime", DateTime.Today);
            hash.Add("DateFormat", "{0:s}");
            hash.Add("ErrorFormat", "DATE : {0} - Level : {1} - Message: {2}");
            props = new JavaProperties(hash);
            
            Update();
        }

        public void Update()
        {
            FileStream stream = File.OpenRead(propertiesFile);
            props.Load(stream);
            stream.Close();
            stream = File.OpenWrite(propertiesFile);
            props.Store(stream, "");
            stream.Flush();
            stream.Close();
        }

        public void Save()
        {
            FileStream stream = File.OpenWrite(propertiesFile);
            props.Store(stream, "");
            stream.Flush();
            stream.Close();
        }
        public String getValue(String key)
        {
            return props.GetProperty(key);
        }

        public void setValue(String key, String value)
        {
            props.SetProperty(key, value);
        }

        public int getIntValue(String key)
        {
            return Convert.ToInt32(props.GetProperty(key));
        }

        public void setIntValue(String key, int value)
        {
            props.SetProperty(key, value.ToString());
        }

        
    }
}
