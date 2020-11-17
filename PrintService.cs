using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Win32;

namespace HttpPrint
{
    public partial class PrintService : ServiceBase
    {
        private HttpListener listener;
        private Thread httpThread;
        private int Runing = 0;
        string printerStatus = "ONLINE";
        Timer timer;
        public  PrintService()
        {
            InitializeComponent();
            httpThread = new Thread(new ThreadStart(HttpListenerRun));
            listener = new HttpListener();
            timer = new Timer(UploadStatus, null, 5000, 60000);
            listener.Prefixes.Add("http://+:5000/");
        }
        protected override void OnStart(string[] args)
        {
            Interlocked.CompareExchange(ref Runing, 1, 0);
            listener.Start(); //开始监听端口，接收客户端请求
            httpThread.Start();
        }
        protected void UploadStatus(object status)
        {
            var config = Program.config;
            string printerName = config.ReadKey("printer_name");
            string printerCode = config.ReadKey("printer_code"); 
            string port = config.ReadKey("listen_port");
            string hostName = Dns.GetHostName();//Hostname
            string upload_path = config.ReadKey("upload_path");
            string printerIp = GetLocalIP();     //IP Address    
            string plate_code = config.ReadKey("plant_code");
            var liveTime = DateTime.Now;//Latest online time
            Data data = new Data();
            data.printer_name = printerName;
            data.plant_code = plate_code;
            data.printer_ip = printerIp;
            data.live_time = liveTime;
            data.service_url = $"http://{printerIp}:{port}/print";
            data.status_url =  $"http://{printerIp}:{port}/status";
            data.printer_status = "ONLINE";
            data.printer_code = printerCode ?? hostName;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var json = JsonConvert.SerializeObject(data);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    var task = client.PostAsync(upload_path, content);
                    task.Wait();
                    var response = task.Result;
                    response.EnsureSuccessStatusCode();
                    var t = response.Content.ReadAsStringAsync();
                    t.Wait();
                    var res = t.Result;
                    Program.writeLog(res);
                }
                catch (Exception e)
                {
                    Program.writeLog(e.Message, e);
                }
            }
        }
        protected void HttpListenerRun()
        {
            var config = Program.config;
            while(Runing == 1)
            {
                try
                {
                    //阻塞主函数至接收到一个客户端请求为止
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    //获取浏览器的post请求
                    Stream postStream = request.InputStream;
                    StreamReader sr = new StreamReader(postStream);
                    string postMsg = sr.ReadToEnd();
                    sr.Close();

                    var uri = request.Url.AbsoluteUri;
                    uri = uri.Substring(uri.LastIndexOf("/")+1);
                    var output = response.OutputStream;
                    var res = new ApiResult();
                    byte[] buffer;
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    if (request.HttpMethod.ToUpper() == "OPTIONS")
                    {
                        response.StatusCode = 204;
                        output.Flush();
                        output.Close();
                        continue;
                    }
                    if (uri == "print") {
                        try
                        {                           
                            Print print = new Print();
                            res.message = "print success";
                            print.print(postMsg);
                        }catch(Exception ex)
                        {
                            res.code = 500;
                            res.message = $"{ex.Message}\n{ex.StackTrace}";
                            Program.writeLog(ex.Message, ex);
                            
                        }
                        finally
                        {
                            //关闭输出流，释放相应资源
                            buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(res));
                            response.ContentLength64 = buffer.Length;
                            output.Write(buffer, 0, buffer.Length);
                            output.Flush();
                            output.Close();
                        }
                        continue;
                    }
                    if (uri == "status") {
                        string printerCode = config.AppSettings.Settings["printer_code"]?.Value;
                        string printerName = config.AppSettings.Settings["printer_name"]?.Value;
                        string port = config.AppSettings.Settings["listen_port"]?.Value;
                        string hostName = Dns.GetHostName(); //Hostname
                        string printerIp = GetLocalIP();     //IP Address         
                        string liveTime = DateTime.Now.ToString(); //Latest online time
                        res.data = new{ host_name =  hostName, printer_ip = printerIp ,
                            printer_name = printerName ,
                            live_time= liveTime ,printer_status = printerStatus};
                        res.message = "printer is ready";
                        buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(res));
                        response.ContentLength64 = buffer.Length;
                        output.Write(buffer, 0, buffer.Length);
                        output.Flush();
                        output.Close();
                        continue;
                    }

                    response.StatusCode= 404;
                    buffer = System.Text.Encoding.UTF8.GetBytes("Service Not Found");
                    output.Write(buffer,0, buffer.Length);
                    output.Flush();
                    output.Close();
                }
                catch (Exception ex)
                {
                    Program.writeLog($"Listener Error {ex.Message}", ex);
                    Console.WriteLine(ex);
                }
            }
        }
        protected override void OnStop()
        {
            Interlocked.CompareExchange(ref Runing, 0, 1);
            listener.Stop(); //关闭HttpListener
        }

        public void Stop()
        {
            Interlocked.CompareExchange(ref Runing, 0, 1);
            listener.Stop(); //关闭HttpListener
        }
        public void Start()
        {
            Interlocked.CompareExchange(ref Runing, 1, 0);
            listener.Start(); //开始监听端口，接收客户端请求
            httpThread.Start();
        }
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "";
            }
        }
        class Data
        {
            public string printer_code { get; set; } = "0";
            public string plant_code { get; set; } 
            public string printer_ip { get; set; }
            public string service_url { get; set; } 
            public string status_url { get; set; } 
            public string printer_name { get; set; }
            public string printer_status { get; set; }
            public DateTime live_time { get; set; }
            public string oper { get; set; } = null;
        }
        public class JsonContent : StringContent
        {
            //用以接受对象变量
            public JsonContent(object obj) :
               base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
            { }
        }
        class ApiResult{
            public int code{get;set;} = 200;
            public string message {get;set;}
            public object data {get;set;}
        }
    }
}
