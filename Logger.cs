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
        private String dirname = "c:/temp";
        public string directory
        {
            get
            {
                return dirname;
            }
            set { dirname = value; }
        }
        private String baseFilename = "logFile";
        private String logFilename = "logFile";
        public string logfile
        {
            get
            {
                if (logFilename == null)
                {
                    logFilename = baseFilename;
                }
                return logFilename;
            }
            set { logFilename = value; }
        }

        private long filesize = 1000;
        public long maxSize { get { return filesize; } set { filesize = value; } }
        private int maxFiles = 100;
        public int numFiles { get { return maxFiles; } set { maxFiles = value; } }
        public enum ROLLOVER { TIME, SIZE, CIRCULAR, NONE };
        public enum LEVEL { INFO, WARN, DEBUG, ERROR, FATAL, VERBOSE };
        private DateTime lastRollover = DateTime.Now;
        private Logger instance;
        private HashSet<String> loggerClasses = new HashSet<String>();
        private HashSet<String> loggerEnabled = new HashSet<String>();
        private LEVEL level = LEVEL.INFO;
        private ROLLOVER rollover = ROLLOVER.TIME;
        private bool doAnyClass = false;
        private int filecount = 0;
        private DateTime lastTime = DateTime.Today;
        private String dateformat = "{0:s}";
        private String errorformat = "DATE : {0} - Level : {1} - Message: {2}";
 
        public Logger getInstance()
        {
            lock (Lock)
            {
                if (instance == null && logfile != null)
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
            directory = direct;
            baseFilename = logName;
            instance = this;
            saveProperty();
        }

        public Logger(String direct, String log, LEVEL lvl, ROLLOVER rollovr)
        {
            init();
            level = lvl;
            rollover = rollovr;
            directory = direct;
            baseFilename = log;
            instance = this;
            saveProperty();
        }

        public Logger(String direct, String logName, LEVEL lvl, ROLLOVER rollovr, long maxFileSize, int numFiles)
        {
            init();
            level = lvl;
            rollover = rollovr;
            directory = direct;
            baseFilename = logName;
            instance = this;
            maxSize = maxFileSize;
            maxFiles = numFiles;

            saveProperty();
        }

        private void init()
        {
            String classes = Properties.Settings.Default.enabledClasses;
            if (classes.Split(',').Length > 0)
            {
                foreach (String clazz in classes.Split(','))
                {
                    loggerEnabled.Add(clazz);
                }
            }
            else
            {
                doAnyClass = true;
            }
            dirname = Properties.Settings.Default.directory;
            maxFiles = Properties.Settings.Default.maxFiles;
            filesize = Properties.Settings.Default.fileSize;
            level = (LEVEL)Enum.Parse(typeof(LEVEL), ((String)Properties.Settings.Default.level));
            rollover = (ROLLOVER)Enum.Parse(typeof(ROLLOVER), ((String)Properties.Settings.Default.rollover));
            doAnyClass = Properties.Settings.Default.doAnyClass;
            logFilename = Properties.Settings.Default.logFilename;
            filecount = Properties.Settings.Default.filecount;
            baseFilename = Properties.Settings.Default.baseFileName;
            dateformat = Properties.Settings.Default.dateformat;
            errorformat = Properties.Settings.Default.errorformat;
        }

        private void saveProperty()
        {
            Properties.Settings.Default.directory = dirname;
            Properties.Settings.Default.maxFiles = maxFiles;
            Properties.Settings.Default.fileSize = filesize;
            Properties.Settings.Default.level = level.ToString();
            Properties.Settings.Default.rollover = rollover.ToString();
            Properties.Settings.Default.doAnyClass = doAnyClass;
            Properties.Settings.Default.logFilename = logFilename;
            Properties.Settings.Default.filecount = filecount;
            Properties.Settings.Default.baseFileName = baseFilename;

            Properties.Settings.Default.dateformat = dateformat;
            Properties.Settings.Default.errorformat = errorformat;
            Properties.Settings.Default.Save();
        }


        private void doRollOver()
        {
            if (checkRollOver(logFilename))
            {
                if (rollover.Equals(ROLLOVER.TIME) || rollover.Equals(ROLLOVER.SIZE))
                {
                    logfile = directory + "/" + baseFilename + "_" + String.Format("{0:yyyyMMdd-HHmmss}", DateTime.Now);
                }
                if (rollover.Equals(ROLLOVER.CIRCULAR))
                {
                    int num = getLastFileNumber();
                    filecount = num + 1;
                    if (filecount > maxFiles)
                        filecount = 0;
                    logfile = directory + "/" + baseFilename + "_" + filecount;
                }
                int count = getFileCount();
                if ( count > maxFiles)
                {
                    String file = getEarlyOrLatestFile(true);
                    if (file != null)
                        File.Delete(directory + "/" + file);
                }
                saveProperty();
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
            if (level >= this.level)
            {
                StreamWriter log;
                if (!File.Exists(logfile + ".log"))
                {
                    log = new StreamWriter(logfile + ".log");
                }
                else
                {
                    log = File.AppendText(logfile + ".log");
                }

                log.WriteLine(String.Format(errorformat, String.Format(dateformat, DateTime.Now), level.ToString(), message));
                log.Flush();
                log.Close();
                log = null;
            }
        }

        //return true if need to rollover
        private bool checkRollOver(String logFileName)
        {
            if (!File.Exists(logfile + ".log"))
            {
                if (rollover.Equals(ROLLOVER.TIME) || rollover.Equals(ROLLOVER.SIZE))
                {
                    logfile = directory + "/" + baseFilename + "_" + String.Format("{0:yyyyMMdd-HHmmss}", DateTime.Now);
                }
                if (rollover.Equals(ROLLOVER.CIRCULAR))
                {                   
                    logfile = directory + "/" + baseFilename + "_" + filecount;
                }
                saveProperty();
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
                result = info.Length > maxSize;
            }
            
            return result;
        }

        private int getFileCount()
        {
            DirectoryInfo info = Directory.GetParent(logfile + ".log");
            FileInfo[] files = info.GetFiles();
            int count = 0;
            foreach (FileInfo i in files)
            {
                if (i.Name.Contains(baseFilename))
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
            DirectoryInfo info = Directory.GetParent(logfile);
            FileInfo[] files = info.GetFiles();

            DateTime latest = DateTime.MaxValue;
            String file = null;
            foreach (FileInfo i in files)
            {
                if (i.Name.Contains(baseFilename))
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
                    DateTime dt = DateTime.Parse(dateStr);
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
