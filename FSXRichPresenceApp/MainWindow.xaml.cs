using FSUIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DiscordRPC;
using DiscordRPC.Logging;

namespace FSXRichPresenceApp
{
    enum simVersion
    {
        FSX,
        FSXSE,
        P3Dv1,
        P3Dv2,
        P3Dv3,
        P3Dv4,
        XPlane
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DiscordRpcClient client;
        private simVersion version = simVersion.P3Dv4;

        // Set up a main timer
        private DispatcherTimer timerMain = new DispatcherTimer();
        // And another to look for a connection
        private DispatcherTimer timerConnection = new DispatcherTimer();
        // Timer for updating 
        private DispatcherTimer timerDiscord = new DispatcherTimer();

        private System.Windows.Forms.NotifyIcon ni = null;

        private double oldVelocity = 0;
        private bool flightStartSet;
        private DateTime startTime;
        AircraftParse parse = new AircraftParse();

        struct Data
        {
            public double parsedAltitude;
            public double parsedAirspeed;
            public double parsedGS;
            public double parsedVertSpd;
            public double parsedHDG;
            public double parsedMach;
            public bool parsedGround;
            public bool parsedPause;
            public bool parsedMenu;
            public bool parsedGear;
            public String aircraft;
        }

        // =====================================
        // DECLARE OFFSETS YOU WANT TO USE HERE
        // =====================================
        private Offset<int> simStupidNumber = new Offset<int>(0x3124);
        private Offset<double> altitude = new Offset<double>(0x34B0);
        private Offset<uint> airspeed = new Offset<uint>(0x02BC);
        private Offset<uint> gs = new Offset<uint>(0x02B4);
        private Offset<int> vertSpd = new Offset<int>(0x02C8);
        private Offset<double> heading = new Offset<double>(0x02CC);
        private Offset<double> mach = new Offset<double>(0x35A0);
        private Offset<short> onPause = new Offset<short>(0x0264);
        private Offset<short> onGround = new Offset<short>(0x0366);
        private Offset<short> onMenu = new Offset<short>(0x3365);
        private Offset<short> gear = new Offset<short>(0x0BE8);
        private Offset<String> aircraftNameX = new Offset<String>(0x3D00, 256);
        private Offset<String> aircraft = new Offset<String>(0x3500, 24);

        public MainWindow()
        {
            InitializeComponent();
            configureForm();

            this.chkClose.Click += ChkClose_Click;
            this.chkMinimize.Click += ChkMinimize_Click;
            this.chkClose.IsChecked = Properties.Settings.Default.CloseWithSim;
            this.chkMinimize.IsChecked = Properties.Settings.Default.StartMinimzed;

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("Main.ico");
            ni.Visible = true;
            ni.DoubleClick += delegate (object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            ni.Click += delegate (object sender, EventArgs args)
            {
                System.Windows.Controls.ContextMenu menu = (System.Windows.Controls.ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;
            };

            if (Properties.Settings.Default.StartMinimzed)
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }

            //Gui timer
            timerMain.Interval = TimeSpan.FromMilliseconds(50);
            timerMain.Tick += timerMain_Tick;

            //Connection Timer (start)
            timerConnection.Interval = TimeSpan.FromMilliseconds(1000);
            timerConnection.Tick += timerConnection_Tick;
            timerConnection.Start();

            //Discord Timer
            timerDiscord.Interval = TimeSpan.FromMilliseconds(1000);
            timerDiscord.Tick += discordUpdate_Tick;
        }

        private void ChkMinimize_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.StartMinimzed = chkMinimize.IsChecked.Value;
        }

        private void ChkClose_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CloseWithSim = chkClose.IsChecked.Value;
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Show(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if(WindowState == WindowState.Minimized)
                this.Hide();

            System.Windows.Controls.ContextMenu menu = (System.Windows.Controls.ContextMenu)this.FindResource("NotifierContextMenu");
            menu.IsOpen = false;

            base.OnStateChanged(e);
        }

        private void timerConnection_Tick(object sender, EventArgs e)
        {
            // Try to open the connection
            try
            {

                FSUIPCConnection.Open();
                // If there was no problem, stop this timer and start the main timer
                this.timerConnection.Stop();
                this.timerMain.Start();
                // Update the connection status
                configureForm();
                //Start Discord
                startDiscord();
            }
            catch
            {
                // No connection found. Don't need to do anything, just keep trying
            }
        }

        // This method runs 20 times per second (every 50ms). This is set in the form constructor above.
        private void timerMain_Tick(object sender, EventArgs e)
        {
            // Call process() to read/write data to/from FSUIPC
            // We do this in a Try/Catch block incase something goes wrong
            try
            {
                FSUIPCConnection.Process();

                // Update the information on the form
                // (See the Examples Application for more information on using Offsets).

                Data data = parseData();
                
                this.txtAirspeed.Text = data.parsedAirspeed.ToString();
                this.txtGroundspeed.Text = data.parsedGS.ToString("F0");
                this.txtVertspeed.Text = data.parsedVertSpd.ToString("F0");
                this.txtHeading.Text = data.parsedHDG.ToString("F0");
                this.txtSimNum.Text = simStupidNumber.Value.ToString();
                this.txtOnGround.Text = data.parsedGround.ToString();
            }
            catch (Exception ex)
            {
                // An error occured. Tell the user and stop this timer.
                this.timerMain.Stop();
                System.Windows.MessageBox.Show("Flight Simulator closed! Error:" + ex.Message, "FSUIPC", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                if(Properties.Settings.Default.CloseWithSim)
                    System.Windows.Application.Current.Shutdown();
                // Update the connection status
                configureForm();
            }
        }

        private void discordUpdate_Tick(object sender, EventArgs e)
        {
            String details;
            String state;

            try
            {
                Data data = parseData();


                if (data.parsedAltitude > 18000)
                {
                    state = String.Format("Aircraft: {0} | Ground Spd: {1} | Mach: {2} | Hdg: {3}",
                        data.aircraft.Length > 20 ? data.aircraft.Substring(0, 20) : data.aircraft,
                        data.parsedGS.ToString("F0"),
                        data.parsedMach.ToString("F2"),
                        data.parsedHDG.ToString("F0"));

                    details = String.Format("Sim: {0} | {1} | FL{2}",
                        version.ToString(),
                        getState(data),
                        (data.parsedAltitude / 100).ToString("F0"));
                }
                else
                {
                    state = String.Format("Aircraft: {0} | Ground Spd: {1} | IAS: {2} | Hdg: {3}",
                        data.aircraft.Length > 20 ? data.aircraft.Substring(0, 20) : data.aircraft,
                        data.parsedGS.ToString("F0"),
                        data.parsedAirspeed.ToString("F0"),
                        data.parsedHDG.ToString("F0"));

                    details = String.Format("Sim: {0} | {1} | {2}ft",
                        version.ToString(),
                        getState(data),
                        data.parsedAltitude.ToString("F0"));
                }

                RichPresence presence = new RichPresence();
                presence.Details = details;
                presence.State = state;
                presence.Assets = new Assets()
                {
                    LargeImageKey = parse.parsePlane(data.aircraft.ToLower()),
                    LargeImageText = data.aircraft
                };

                client.SetPresence(presence);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Discord Tick Error! Error:" + ex.Message, "FSUIPC", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            
            
        }


        private String getState(Data data)
        {
            string state = "";

            double changeInVelocity = data.parsedAirspeed - oldVelocity;

            if ((data.parsedVertSpd > 50 && data.parsedGear) || (changeInVelocity > 0 && data.parsedGround && data.parsedGS > 50))
            {
                state = "Takeoff";

                if (!flightStartSet)
                {
                    flightStartSet = true;
                    startTime = DateTime.Now;
                }
            } else if (data.parsedVertSpd > 250) {
                state = "Climbing";
            } else if (data.parsedVertSpd < -50 && data.parsedGear) {
                state = "Approach";
            } else if (changeInVelocity < 0 && data.parsedGround && data.parsedGS > 50) {
                state = "Landing (Rollout)";
            } else if (data.parsedVertSpd < -250) {
                state = "Descending";
            } else if(!data.parsedGround && data.parsedVertSpd < 250 && data.parsedVertSpd > -250) {
                state = "Cruising";
            } else if (data.parsedGround)
            {
                state = "On the ground";
                flightStartSet = false;
            }


            if(data.parsedMenu) {
                state += " | In Menu";
            } else if (data.parsedPause) {
                state += " | Paused";
            }

            if (data.aircraft.Equals("Not Loaded"))
            {
                state = "In Main Menu";
            }

            return state;
        }

        private Data parseData()
        {
            Data data = new Data();

            data.parsedAltitude = this.altitude.Value * 3.2808399;
            data.parsedAirspeed = (double)this.airspeed.Value / 128d;
            data.parsedGS = (double)this.gs.Value / 65536d / 0.51444d;
            data.parsedVertSpd = (double)this.vertSpd.Value * 60 * 3.28084 / 256;
            data.parsedHDG = this.heading.Value;
            data.parsedMach = this.mach.Value;
            data.parsedGround = this.onGround.Value == 1;
            data.parsedPause = this.onPause.Value == 1;
            data.parsedMenu = this.onMenu.Value == 1 || this.onMenu.Value == 2;
            data.parsedGear = this.gear.Value != 0;
            data.aircraft = this.aircraft.Value.Equals("") ? "Not Loaded" : this.aircraft.Value;

            if (version == simVersion.XPlane)
                data.aircraft = this.aircraftNameX.Value;

            return data;
        }

        // Configures the button and status label depending on if we're connected or not 
        private void configureForm()
        {
            if (FSUIPCConnection.IsOpen)
            {
                this.lblConnectionStatus.Text = "Connected to " + FSUIPCConnection.FlightSimVersionConnected.ToString();
                this.lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                this.lblConnectionStatus.Text = "Disconnected. Looking for Flight Simulator...";
                this.lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void startDiscord()
        {

            FSUIPCConnection.Process();
            if (simStupidNumber.Value > 0 && simStupidNumber.Value < 5)
            {
                version = simVersion.FSX;
            }
            else if (simStupidNumber.Value > 100 && simStupidNumber.Value < 110)
            {
                version = simVersion.FSXSE;
            }
            else if (simStupidNumber.Value > 9 && simStupidNumber.Value < 15)
            {
                version = simVersion.P3Dv1;
            }
            else if (simStupidNumber.Value > 19 && simStupidNumber.Value < 26)
            {
                version = simVersion.P3Dv2;
            }
            else if (simStupidNumber.Value > 29 && simStupidNumber.Value < 35)
            {
                version = simVersion.P3Dv3;
            }
            else if (simStupidNumber.Value == 0)
            {
                version = simVersion.XPlane;
            } else {
                version = simVersion.P3Dv4;
            }

            string cID = version == simVersion.FSX || version == simVersion.FSXSE ? "447447349594292226" : "466739324537405441";
            cID = version == simVersion.XPlane ? "480825144265277461" : cID;

            try
            {
                client = new DiscordRpcClient(cID, true, -1);
                client.Initialize();
                client.SetPresence(new RichPresence()
                {
                    Details = version.ToString(),
                    State = "Booting up - Don't worry, we're getting things running!"
                });

                timerDiscord.Start();
            } catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Discord Start Error! Error:" + ex.Message, "FSUIPC", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        // Window closing so stop all the timers and close the connection
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.timerDiscord.Stop();
            this.timerConnection.Stop();
            this.timerMain.Stop();
            if(this.client != null)
                this.client.Dispose();
            FSUIPCConnection.Close();
            Properties.Settings.Default.Save();
        }
    }
}
