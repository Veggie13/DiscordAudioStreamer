using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordAudioStreamer
{
    class HttpServer
    {
        HttpListener _listener;
        IBoardLayout _boardLayoutController;

        public HttpServer(IBoardLayout boardLayoutController)
        {
            _boardLayoutController = boardLayoutController;
        }

        public void Run()
        {
            var prefix = "http://+:4333/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            try
            {
                _listener.Start();
            }
            catch
            {
                _listener = null;
                return;
            }

            _ = Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    processRequest(context);
                }
                _listener.Close();
            });
        }

        public void Stop()
        {
            _listener?.Stop();
        }

        private void processRequest(HttpListenerContext context)
        {
            byte[] buf = null;
            if (context.Request.HttpMethod == HttpMethod.Get.Method)
            {
                string json = JsonSerializer.Serialize(_boardLayoutController.Layout);
                buf = Encoding.UTF8.GetBytes(json);
            }
            else if (context.Request.HttpMethod == HttpMethod.Post.Method)
            {
                string body = new StreamReader(context.Request.InputStream).ReadToEnd();
                if (body.StartsWith("RES "))
                {
                    Guid id = new Guid(body.Substring(4));
                    _boardLayoutController.GetResourceController(id).Trigger();
                }
                else if (body.StartsWith("VOL "))
                {
                    Guid id = new Guid(body.Substring(4, Guid.Empty.ToString().Length));
                    int volume = int.Parse(body.Substring(5 + Guid.Empty.ToString().Length));
                    _boardLayoutController.GetGroupController(id).Volume = volume;
                }

                buf = Encoding.UTF8.GetBytes("ACK");
            }
            else
            {
                return;
            }

            context.Response.StatusCode = 200;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = buf.Length;

            var output = context.Response.OutputStream;
            output.Write(buf, 0, buf.Length);
            context.Response.Close();
        }
    }
}
