using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace HttpPrint
{
    static class Program
    {
        public static Configuration config;
        public static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintService.log");
        public static FileStream stream;
        public static StreamWriter logWriter;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        public static void Main()
        {
            if (!File.Exists(LogFile)) File.Create(LogFile);
            stream = new FileStream(LogFile, FileMode.Open, FileAccess.Write, FileShare.Read);
            stream.Seek(0, SeekOrigin.End);
            logWriter = new StreamWriter(stream);
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileMap = new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = $"{assembly.Location}.config",
                    LocalUserConfigFilename = $"{assembly.Location}.config"
                };
                config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                var service = new PrintService();
                var ServicesToRun = new ServiceBase[]
                {
                     service
                };
                //ServiceBase.Run(ServicesToRun);
                service.Start();
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                logWriter.Close();
                stream.Close();
            }
        }
        public static void writeLog(string message, Exception ex = null)
        {
            var size = Int32.Parse(config.ReadKey("log_size") ?? "2097152");
            if (stream.Length > size)
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
            }
            logWriter.WriteLine($"{DateTime.Now}: {message} \r\n{ (ex == null ? "" : $"Exception: {ex}") }\r\n");
            logWriter.Flush();

        }
        public static string ReadKey(this Configuration config, string key)
        {
            return config.AppSettings.Settings[key]?.Value;
        }
    }
}
