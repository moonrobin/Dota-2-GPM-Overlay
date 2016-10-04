using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using Dota2GSI;
using Dota2GSI.Nodes;
using DOTA_2_GPM_Overlay___Main.Properties;
using MetroFramework.Forms;
using Microsoft.Win32;
using RestSharp;

// ReSharper disable InconsistentNaming

namespace DOTA_2_GPM_Overlay___Main
{
    public partial class MainForm : MetroForm
    {
        private GameStateListener gsl;
        private int selectedHeroId;
        private BenchmarkResult heroBenchmarkResult;
        private readonly OverlayForm overlayForm;

        public MainForm()
        {
            InitializeComponent();
            InitializeGameStateIntegration();
            overlayForm = new OverlayForm();
            StateLabel.Text = OverlayStatus.Status.Hidden.ToString();
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon.BalloonTipText = Resources.BalloonTipText;
            notifyIcon.BalloonTipTitle = Resources.BalloonTipTitle;
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.Icon = Resources.Alchemist_icon1;
        }

        private void InitializeGameStateIntegration()
        {
            TryCreateGsifile();

            var pname = Process.GetProcessesByName(Resources.Dota2ProcessName);
            if (pname.Length == 0)
            {
                MessageBox.Show(Resources.Dota2NotRunning, Resources.ApplicationTitle, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            gsl = new GameStateListener(4000);
            gsl.NewGameState += OnNewGameState;

            if (!gsl.Start())
            {
                MessageBox.Show(Resources.ListenerCannotStart, Resources.ApplicationTitle, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private void OnNewGameState(GameState gamestate)
        {
            var mapState = gamestate.Map.GameState;
            switch (mapState)
            {
                case DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME:
                case DOTA_GameState.DOTA_GAMERULES_STATE_POST_GAME:
                case DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS:
                {
                    var heroId = gamestate.Hero.ID;

                    if (heroId != selectedHeroId)
                    {
                        // Player has selected a hero, fetch the benchmark data from OpenDOTA API
                        selectedHeroId = heroId;
                        try
                        {
                            var client = new RestClient(Resources.OpenDotaAPIBaseURL);
                            var request = new RestRequest(Resources.OpenDotaAPIBenchmarkMethod, Method.GET);
                            request.AddParameter(Resources.OpenDotaAPIHeroIdParam, heroId.ToString());

                            var response = client.Execute<BenchmarkResult>(request);
                            heroBenchmarkResult = response.Data;
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(Resources.UnhandledException, Resources.ApplicationTitle,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    var GPM = gamestate.Player.GoldPerMinute;
                    var XPM = gamestate.Player.ExperiencePerMinute;
                    var GPMPercentile = heroBenchmarkResult.GetGPMPercentile(GPM);
                    var XPMPercentile = heroBenchmarkResult.GetXPMPercentile(XPM);

                    overlayForm.UpdateOverlay(GPM, XPM, GPMPercentile, XPMPercentile);
                    overlayForm.Show();
                    StateLabel.Text = OverlayStatus.Status.Visible.ToString();
                    StateLabel.ForeColor = Color.Green;
                    break;
                }
                default:
                {
                    selectedHeroId = 0;
                    heroBenchmarkResult = null;

                    overlayForm.UpdateOverlay(0, 0, 0, 0);
                    overlayForm.Hide();
                    StateLabel.Text = OverlayStatus.Status.Hidden.ToString();

                    break;
                }
            }
        }

        private static void TryCreateGsifile()
        {
            var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            if (regKey != null)
            {
                var gsifolder = regKey.GetValue("SteamPath") +
                                @"\steamapps\common\dota 2 beta\game\dota\cfg\gamestate_integration";
                Directory.CreateDirectory(gsifolder);
                var gsifile = gsifolder + @"\gamestate_integration_testGSI.cfg";
                if (File.Exists(gsifile))
                {
                    return;
                }
                File.Create(gsifile);

                string[] contentofgsifile =
                {
                    "\"Dota 2 Integration Configuration\"",
                    "{",
                    "    \"uri\"           \"http://localhost:4000/\"",
                    "    \"timeout\"       \"5.0\"",
                    "    \"buffer\"        \"0.1\"",
                    "    \"throttle\"      \"0.1\"",
                    "    \"heartbeat\"     \"30\"",
                    "    \"data\"",
                    "    {",
                    "        \"provider\"      \"1\"",
                    "        \"map\"           \"1\"",
                    "        \"player\"        \"1\"",
                    "        \"hero\"          \"1\"",
                    "        \"abilities\"     \"1\"",
                    "        \"items\"         \"1\"",
                    "    }",
                    "}"
                };

                File.WriteAllLines(gsifile, contentofgsifile);
            }
            else
            {
                MessageBox.Show(Resources.SteamRegistryKeyNotFound, Resources.ApplicationTitle,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                notifyIcon.Visible = true;
                ShowInTaskbar = false;
            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            ShowInTaskbar = true;
            Invalidate();
        }
    }

    public sealed class OverlayForm : Form
    {
        private int GPM;
        private int XPM;
        private int GPMPercentile;
        private int XPMPercentile;

        public OverlayForm()
        {
            TopMost = true;
            ShowInTaskbar = false;
            Visible = false;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(61, 61, 61);
            TransparencyKey = Color.FromArgb(61, 61, 61);
            StartPosition = FormStartPosition.Manual;

            Width = 100;
            Height = 50;
            Location = new Point(2065, 6);
            Paint += OverlayForm_Paint;
        }

        public void UpdateOverlay(int newGPM, int newXPM, int newGPMPercentile, int newXPMPercentile)
        {
            GPM = newGPM;
            XPM = newXPM;
            GPMPercentile = newGPMPercentile;
            XPMPercentile = newXPMPercentile;
            Invalidate();
        }

        private void OverlayForm_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            var drawString = GPM > 0
                ? string.Format("GPM  {0,4} ({1}%)\r\nXPM  {2,4} ({3}%)", GPM, GPMPercentile, XPM, XPMPercentile)
                : "GPM\r\nXPM";
            var drawFont = new Font("Segoe UI", 11);
            var drawBrush = new SolidBrush(Color.FromArgb(148, 148, 148));
            var drawPoint = new PointF(0, 0);

            e.Graphics.DrawString(drawString, drawFont, drawBrush, drawPoint);
        }
    }
}
