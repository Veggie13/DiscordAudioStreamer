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
        BoardLayoutController _boardLayoutController;
        Bot _bot;
        HttpServer _httpServer;

        public BoardForm()
        {
            InitializeComponent();
        }

        public void SetBoardLayout(BoardLayout boardLayout)
        {
            _layoutPanel.Controls.Clear();

            _layoutPanel.ColumnCount = 1 + Math.Max(1, boardLayout.Groups.Count);
            _layoutPanel.RowCount = 1 + (boardLayout.Groups.Any() ? boardLayout.Groups.Max(g => g.Resources.Count) : 1);

            int col = 0;
            foreach (var group in boardLayout.Groups)
            {
                var header = new Label()
                {
                    Text = group.Heading,
                    AutoSize = true
                };
                _layoutPanel.Controls.Add(header, col, 0);

                var stopButton = new Button()
                {
                    Text = "STOP",
                    AutoSize = true
                };
                stopButton.Click += (_, _) => { group.StopEarly(); };
                _layoutPanel.Controls.Add(stopButton, col, 1);

                var groupController = _boardLayoutController.GetGroupController(group.ID);

                var slider = new TrackBar()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 100,
                    SmallChange = 1,
                    LargeChange = 10,
                    AutoSize = true,
                    MinimumSize = new System.Drawing.Size(300, 0)
                };
                slider.ValueChanged += (_, _) => { groupController.Volume = slider.Value; };
                _layoutPanel.Controls.Add(slider, col, 2);

                int row = 3;
                foreach (var resource in group.Resources)
                {
                    var button = new Button()
                    {
                        Text = resource.Text,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowOnly,
                        Font = new System.Drawing.Font("Arial", 30)
                    };
                    var res = resource;
                    button.Click += (_, _) => res.Trigger();

                    _layoutPanel.Controls.Add(button, col, row);

                    row++;
                }
                col++;
            }

            var reloadButton = new Button()
            {
                Text = "RELOAD",
                AutoSize = true
            };
            reloadButton.Click += (_, _) =>
            {
                string layoutFile = ConfigurationManager.AppSettings["layoutFile"];
                string content = File.ReadAllText(layoutFile);
                _boardLayoutController.Deserialize(content);
                SetBoardLayout(_boardLayoutController.Layout);
            };

            _layoutPanel.Controls.Add(reloadButton, col, 0);
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

            var boardLayout = JsonSerializer.Deserialize<BoardLayout>(responseContent);
            _layoutPanel.Controls.Clear();

            _layoutPanel.ColumnCount = Math.Max(1, boardLayout.Groups.Count);
            _layoutPanel.RowCount = 1 + (boardLayout.Groups.Any() ? boardLayout.Groups.Max(g => g.Resources.Count) : 1);

            int col = 0;
            foreach (var group in boardLayout.Groups)
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

                string layoutFile = ConfigurationManager.AppSettings["layoutFile"];
                string content = File.ReadAllText(layoutFile);
                _boardLayoutController = new BoardLayoutController(content);
                SetBoardLayout(_boardLayoutController.Layout);

                _bot = new Bot()
                {
                    Input = _boardLayoutController.WaveProvider,
                    Layout = _boardLayoutController.Layout
                };
                _bot.Run(ConfigurationManager.AppSettings["token"]);

                _httpServer = new HttpServer(_boardLayoutController);
                _httpServer.Run();
            }
        }

        private void BoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _bot?.Stop();
            _httpServer?.Stop();
        }
    }
}
