using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordAudioStreamer
{
    public partial class BoardForm : Form
    {
        BoardLayout _boardLayout;
        MixingSampleProvider _mixer;
        List<BoardGroupController> _groupControllers = new List<BoardGroupController>();
        Bot _bot;
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
                _groupControllers.Add(groupController);

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
                    button.Click += (_, _) => res.Trigger();

                    _layoutPanel.Controls.Add(button, col, row);

                    row++;
                }
                col++;
            }
        }

        private void BoardForm_Load(object sender, EventArgs e)
        {
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
        }

        private void BoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _bot.Stop();
        }
    }
}
