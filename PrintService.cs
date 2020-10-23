using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace HttpPrint
{
    public partial class PrintService : ServiceBase
    {
        private HttpListener listener;
        private Thread httpThread;
        private int Run = 0;
        public  PrintService()
        {
            InitializeComponent();
            listener = new HttpListener();
            listener.Prefixes.Add("http://+:5000/");
        }
        protected override void OnStart(string[] args)
        {
            Interlocked.CompareExchange(ref Run, 1, 0);
            listener.Start(); //开始监听端口，接收客户端请求
            httpThread = new Thread(new ThreadStart(HttpListenerRun));
            httpThread.Start();

        }
        protected void HttpListenerRun()
        {
            while(Run == 1)
            {
                try
                {
                    //阻塞主函数至接收到一个客户端请求为止
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    Stream postStream = request.InputStream;
                    StreamReader sr = new StreamReader(postStream);
                    string requestMsg = sr.ReadToEnd();
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
                            Print print = new Print(requestMsg);
                            res.message = "print success";
                            print.print(requestMsg);
                        }catch(Exception ex)
                        {
                            res.code = 500;
                            res.message = $"{ex.Message}\n{ex.StackTrace}";
                            
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
                    Console.WriteLine(ex);
                }
            }
        }
        protected override void OnStop()
        {
            Interlocked.CompareExchange(ref Run, 0, 1);
            listener.Stop(); //关闭HttpListener
        }

        public void Stop()
        {
            Interlocked.CompareExchange(ref Run, 0, 1);
            listener.Stop(); //关闭HttpListener
        }
        public void Start()
        {
            Interlocked.CompareExchange(ref Run, 1, 0);
            listener.Start(); //开始监听端口，接收客户端请求
            httpThread = new Thread(new ThreadStart(HttpListenerRun));
            httpThread.Start();
        }

        class ApiResult{
            public int code{get;set;} = 200;
            public string message {get;set;}
            public object data {get;set;}
        }
    }
}
