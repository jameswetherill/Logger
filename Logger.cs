using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace Logger
{
    public class Logger
    {
        private object Lock = new object();
        private PropertiesWrapper pw;
        //private String dirname = "c:/temp";
        //public string directory
        //{
        //    get
        //    {
        //        return dirname;
        //    }
        //    set { dirname = value; }
        //}
        //private String baseFilename = "logFile";
        //private String logFilename = "logFile";
        //public string logfile
        //{
        //    get
        //    {
        //        if (logFilename == null)
        //        {
        //            logFilename = baseFilename;
        //        }
        //        return logFilename;
        //    }
        //    set { logFilename = value; }
        //}

        //private long filesize = 1000;
        //public long maxSize { get { return filesize; } set { filesize = value; } }
        //private int maxFiles = 100;
        //public int numFiles { get { return maxFiles; } set { maxFiles = value; } }
        public enum ROLLOVER { TIME, SIZE, CIRCULAR, NONE };
        public enum LEVEL { INFO, WARN, DEBUG, ERROR, FATAL, VERBOSE };
        //private DateTime lastRollover = DateTime.Now;
        private Logger instance;
        //private HashSet<String> loggerClasses = new HashSet<String>();
        //private HashSet<String> loggerEnabled = new HashSet<String>();
        //private LEVEL level = LEVEL.INFO;
        //private ROLLOVER rollover = ROLLOVER.TIME;
        //private bool doAnyClass = false;
        //private int filecount = 0;
        //private DateTime lastTime = DateTime.Today;
        //private String dateformat = "{0:s}";
        //private String errorformat = "DATE : {0} - Level : {1} - Message: {2}";
 
        public Logger getInstance()
        {
            lock (Lock)
            {
                if (instance == null )
                {
                    instance = new Logger();
                }
            }
            return instance;
        }

        private Logger()
        {
            init();
        }

        public Logger(String direct, String logName)
        {
            init();
            pw.setValue("Directory", direct);
            pw.setValue("BaseFileName", logName);
            instance = this;
            pw.Save();
        }

        public Logger(String direct, String logName, LEVEL lvl, ROLLOVER rollovr)
        {
            init();
            instance = this;
            pw.setValue("LEVEL", lvl.ToString());
            pw.setValue("ROLLOVER", rollovr.ToString());
            pw.setValue("Directory", direct);
            pw.setValue("BaseFileName", logName);
            pw.Save();
        }

        public Logger(String direct, String logName, LEVEL lvl, ROLLOVER rollovr, int maxFileSize, int numFiles)
        {
            init();
            pw.setValue("LEVEL", lvl.ToString());
            pw.setValue("ROLLOVER", rollovr.ToString());
            pw.setValue("Directory", direct);
            pw.setValue("BaseFileName", logName);
            instance = this;
            pw.setIntValue("MaxSize", maxFileSize);
            pw.setIntValue("MaxFiles",  numFiles);
            pw.Save();
        }
        private void checkDirectoryExists(String dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private void init()
        {
            pw = new PropertiesWrapper();
        }



        private void doRollOver()
        {
            if (checkRollOver(pw.getValue("LogFile")))
            {
                ROLLOVER rollover = (ROLLOVER)Enum.Parse(typeof(ROLLOVER), pw.getValue("ROLLOVER")) ;
                if (rollover.Equals(ROLLOVER.TIME) || rollover.Equals(ROLLOVER.SIZE))
                {
                    pw.setValue("LogFile", pw.getValue("Directory") + "/" + pw.getValue("BaseFileName") + "_" + String.Format("{0:yyyyMMdd-HHmmss}", DateTime.Now));

                }
                if (rollover.Equals(ROLLOVER.CIRCULAR))
                {
                    int num = getLastFileNumber();
                    //filecount = ;
                    if (num + 1 > pw.getIntValue("MaxFiles"))
                        pw.setIntValue("FileCount",  0);
                    pw.setValue("LogFile", pw.getValue("Directory") + "/" + pw.getValue("BaseFileName") + "_" + (num+1));
                }
                int count = getFileCount();
                if ( count > pw.getIntValue("MaxFiles"))
                {
                    String file = getEarlyOrLatestFile(true);
                    if (file != null)
                        File.Delete(pw.getValue("Directory") + "/" + file);
                }
                pw.Save();
            }
        }

        private System.Object lockThis = new System.Object();



        public void LogMessage(LEVEL level, String message)
        {
            lock (lockThis)
            {
                doRollOver();
                writeLog(message, level);
            }
        }

        private void writeLog(String message, LEVEL level)
        {
            LEVEL Lvl = (LEVEL)Enum.Parse(typeof(LEVEL), pw.getValue("LEVEL"));
            if (Lvl.CompareTo(level) > -1)
            {
                StreamWriter log;
                checkDirectoryExists(pw.getValue("Directory"));
                if (!File.Exists(pw.getValue("LogFile") + ".log"))
                {
                    log = new StreamWriter(pw.getValue("LogFile") + ".log");
                }
                else
                {
                    log = File.AppendText(pw.getValue("LogFile") + ".log");
                }

                log.WriteLine(String.Format(pw.getValue("ErrorFormat"), String.Format(pw.getValue("DateFormat"), DateTime.Now), level.ToString(), message));
                log.Flush();
                log.Close();
                log = null;
            }
        }

        //return true if need to rollover
        private bool checkRollOver(String logFileName)
        {
            ROLLOVER rollover = (ROLLOVER)Enum.Parse(typeof(ROLLOVER), pw.getValue("ROLLOVER"));
               
            if (!File.Exists(pw.getValue("LogFile") + ".log"))
            {
               
                if (rollover.Equals(ROLLOVER.TIME) || rollover.Equals(ROLLOVER.SIZE))
                {
                    pw.setValue("LogFile", pw.getValue("Directory") + "/" + pw.getValue("BaseFileName") + "_" + String.Format("{0:yyyyMMdd-HHmmss}", DateTime.Now));

                }
                if (rollover.Equals(ROLLOVER.CIRCULAR))
                {

                    pw.setValue("LogFile", pw.getValue("Directory") + "/" + pw.getValue("BaseFileName") + "_" + pw.getIntValue("FileCount"));
                }
                pw.Save();
                return false;
            }
            bool result = false;
            if (rollover.Equals(ROLLOVER.TIME))
            {
                result = checkTime(logFileName);
            }
            if (rollover.Equals(ROLLOVER.SIZE) || rollover.Equals(ROLLOVER.CIRCULAR))
            {
                FileInfo info = new FileInfo(logFileName + ".log");
                result = info.Length > pw.getIntValue("MaxSize");
            }
            
            return result;
        }

        private int getFileCount()
        {
            DirectoryInfo info = Directory.GetParent(pw.getValue("LogFile") + ".log");
            FileInfo[] files = info.GetFiles();
            int count = 0;
            foreach (FileInfo i in files)
            {
                if (i.Name.Contains(pw.getValue("BaseFileName")))
                {
                    count++;
                }
            }
            return count;
        }

        private int getLastFileNumber()
        {
            String file = getEarlyOrLatestFile(false);
            int count = 0;
            if (file != null)
            {
                String num = file.Split('.')[0].Split('_')[1];
                count = Convert.ToInt32(num);
            }
            return count;
        }

        String getEarlyOrLatestFile(bool earlyLate)
        {
            DirectoryInfo info = Directory.GetParent(pw.getValue("LogFile"));
            FileInfo[] files = info.GetFiles();

            DateTime latest = DateTime.MaxValue;
            String file = null;
            foreach (FileInfo i in files)
            {
                if (i.Name.Contains(pw.getValue("BaseFileName")))
                {
                    DateTime last = i.LastWriteTime;
                    if (latest == DateTime.MaxValue)
                    {
                        latest = last;
                        file = i.Name;
                    }
                    else if (earlyLate && latest.Ticks > last.Ticks)
                    {
                        latest = last;
                        file = i.Name;
                    }
                    else if (!earlyLate && latest.Ticks < last.Ticks)
                    {
                        latest = last;
                        file = i.Name;
                    }
                }
            }
            return file;
        }


        private bool checkTime(String fileName)
        {
            if (!fileName.Contains("_"))
            {
                return false;
            }
            else
            {
                String dateStr = fileName.Split('_')[1];
                try
                {
                    DateTime dt = DateTime.ParseExact(dateStr, "yyyyMMdd-HHmmss", null);
                    dt = dt.AddDays(1);

                    DateTime now = DateTime.Today;
                    if (now >= dt)
                        return true;
                }
                catch (FormatException ex)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
