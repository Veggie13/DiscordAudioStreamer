using System;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;

namespace DiscordAudioStreamer
{
    public partial class BoardForm : Form
    {
        IControllerProvider _controllerProvider;

        public BoardForm()
        {
            InitializeComponent();
        }

        public void RenderLayout()
        {
            var boardLayoutController = _controllerProvider.GetLayoutController();
            var boardLayout = boardLayoutController.Layout;

            _layoutPanel.Controls.Clear();

            _layoutPanel.ColumnCount = 1 + Math.Max(1, boardLayout.Groups.Count);
            _layoutPanel.RowCount = 1 + (boardLayout.Groups.Any() ? boardLayout.Groups.Max(g => g.Resources.Count) : 1);

            int col = 0;
            foreach (var group in boardLayout.Groups)
            {
                var groupController = boardLayoutController.GetGroupController(group.ID);

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
                stopButton.Click += (_, _) => { groupController.StopEarly(); };
                _layoutPanel.Controls.Add(stopButton, col, 1);

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
                    var resourceController = groupController.GetResourceController(resource.ID);

                    var button = new Button()
                    {
                        Text = resource.Text,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowOnly,
                        Font = new System.Drawing.Font("Arial", 30)
                    };
                    button.Click += (_, _) => resourceController.Trigger();

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
                _controllerProvider.Reload();
                RenderLayout();
            };

            _layoutPanel.Controls.Add(reloadButton, col, 0);
        }

        private void BoardForm_Load(object sender, EventArgs e)
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains("remote"))
            {
                string remote = ConfigurationManager.AppSettings["remote"];
                _controllerProvider = new RemoteControllerProvider(remote);
            }
            else
            {
                string layoutFile = ConfigurationManager.AppSettings["layoutFile"];
                string botToken = ConfigurationManager.AppSettings["token"];
                _controllerProvider = new LocalControllerProvider(layoutFile, botToken);
            }

            Text += $" - {_controllerProvider.Name}";
            RenderLayout();
        }

        private void BoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _controllerProvider.Shutdown();
        }
    }
}
