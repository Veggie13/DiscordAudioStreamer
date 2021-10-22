using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordAudioStreamer
{
    public partial class BoardForm : Form
    {
        BoardLayout _boardLayout;
        MixingSampleProvider _mixer;
        Dictionary<Guid, BoardGroupController> _groupControllers = new Dictionary<Guid, BoardGroupController>();
        Dictionary<Guid, BoardResource> _resources = new Dictionary<Guid, BoardResource>();
        Bot _bot;
        HttpListener _listener;
        Task _httpServer;

        public BoardForm()
        {
            InitializeComponent();
        }

        public void SetBoardLayout(BoardLayout boardLayout)
        {
            _boardLayout = boardLayout;
            _layoutPanel.Controls.Clear();

            _layoutPanel.ColumnCount = Math.Max(1, _boardLayout.Groups.Count);
            _layoutPanel.RowCount = 1 + (_boardLayout.Groups.Any() ? _boardLayout.Groups.Max(g => g.Resources.Count) : 1);

            int col = 0;
            foreach (var group in _boardLayout.Groups)
            {
                var header = new Label()
                {
                    Text = group.Heading,
                    AutoSize = true
                };
                _layoutPanel.Controls.Add(header, col, 0);

                var groupController = new BoardGroupController(group);
                _mixer.AddMixerInput(groupController.Mixer);
                _groupControllers[groupController.Group.ID] = groupController;

                var stopButton = new Button()
                {
                    Text = "STOP",
                    AutoSize = true
                };
                stopButton.Click += (_, _) => { group.StopEarly(); };
                _layoutPanel.Controls.Add(stopButton, col, 1);

                var slider = new TrackBar()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 100,
                    SmallChange = 1,
                    LargeChange = 10,
                    AutoSize = true
                };
                slider.ValueChanged += (_, _) => { groupController.Volume = slider.Value; };
                _layoutPanel.Controls.Add(slider, col, 2);

                int row = 3;
                foreach (var resource in group.Resources)
                {
                    _resources[resource.ID] = resource;

                    var button = new Button()
                    {
                        Text = resource.Text,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowOnly
                    };
                    var res = resource;
                    button.Click += (_, _) => res.Trigger();

                    _layoutPanel.Controls.Add(button, col, row);

                    row++;
                }
                col++;
            }
        }

        public void SetBoardLayoutRemote(string remote)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(remote)
            };

            var request = new HttpRequestMessage(HttpMethod.Get, remote);
            var response = client.Send(request);
            string responseContent = response.Content.ReadAsStringAsync().Result;

            _boardLayout = JsonSerializer.Deserialize<BoardLayout>(responseContent);
            _layoutPanel.Controls.Clear();

            _layoutPanel.ColumnCount = Math.Max(1, _boardLayout.Groups.Count);
            _layoutPanel.RowCount = 1 + (_boardLayout.Groups.Any() ? _boardLayout.Groups.Max(g => g.Resources.Count) : 1);

            int col = 0;
            foreach (var group in _boardLayout.Groups)
            {
                var header = new Label()
                {
                    Text = group.Heading,
                    AutoSize = true
                };
                _layoutPanel.Controls.Add(header, col, 0);

                var slider = new TrackBar()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 100,
                    SmallChange = 1,
                    LargeChange = 10,
                    AutoSize = true
                };
                slider.ValueChanged += (_, _) =>
                {
                    var client = new HttpClient()
                    {
                        BaseAddress = new Uri(remote)
                    };

                    var request = new HttpRequestMessage(HttpMethod.Post, remote);
                    request.Content = new StringContent($"VOL {group.ID} {slider.Value}");
                    client.Send(request);
                };

                _layoutPanel.Controls.Add(slider, col, 1);

                int row = 2;
                foreach (var resource in group.Resources)
                {
                    _resources[resource.ID] = resource;

                    var button = new Button()
                    {
                        Text = resource.Text,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowOnly
                    };
                    var res = resource;
                    button.Click += (_, _) =>
                    {
                        var client = new HttpClient()
                        {
                            BaseAddress = new Uri(remote)
                        };

                        var request = new HttpRequestMessage(HttpMethod.Post, remote);
                        request.Content = new StringContent($"RES {res.ID}");
                        client.Send(request);
                    };

                    _layoutPanel.Controls.Add(button, col, row);

                    row++;
                }
                col++;
            }
        }

        private void BoardForm_Load(object sender, EventArgs e)
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains("remote"))
            {
                string remote = ConfigurationManager.AppSettings["remote"];
                Text += " - " + remote;
                SetBoardLayoutRemote(remote);
            }
            else
            {
                Text += " - server";
                _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(22050, 2))
                {
                    ReadFully = true
                };

                string layoutFile = ConfigurationManager.AppSettings["layoutFile"];
                string content = File.ReadAllText(layoutFile);
                var boardLayout = JsonSerializer.Deserialize<BoardLayout>(content);
                SetBoardLayout(boardLayout);

                _bot = new Bot()
                {
                    Input = _mixer.ToWaveProvider()
                };
                _bot.Run();

                _httpServer = runServer();
            }
        }

        private void BoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _bot?.Stop();
            _listener?.Stop();
        }

        private async Task runServer()
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
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                processRequest(context);
            }
            _listener.Close();
        }

        private void processRequest(HttpListenerContext context)
        {
            byte[] buf = null;
            if (context.Request.HttpMethod == HttpMethod.Get.Method)
            {
                string json = JsonSerializer.Serialize(_boardLayout);
                buf = Encoding.UTF8.GetBytes(json);
            }
            else if (context.Request.HttpMethod == HttpMethod.Post.Method)
            {
                string body = new StreamReader(context.Request.InputStream).ReadToEnd();
                if (body.StartsWith("RES "))
                {
                    Guid id = new Guid(body.Substring(4));
                    _resources[id].Trigger();
                }
                else if (body.StartsWith("VOL "))
                {
                    Guid id = new Guid(body.Substring(4, Guid.Empty.ToString().Length));
                    int volume = int.Parse(body.Substring(5 + Guid.Empty.ToString().Length));
                    _groupControllers[id].Volume = volume;
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
