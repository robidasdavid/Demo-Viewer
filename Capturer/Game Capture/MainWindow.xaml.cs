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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

/*
 * David Robidas 2020 (sneakyevil#1967)
 * Purpose: Drive and handle all interactions with MainWindow.xaml
 * Date: 20 April 2020
 * Some code taken from Quentin Young (https://github.com/qlyoung)
 */

namespace Game_Capture
{
    public partial class MainWindow : Window
    {

        public static bool captureRunning = false;
        public static string failed = "";
        public static string filePath = "";
        public static string fName = "";

        public MainWindow()
        {
            InitializeComponent();
            stopButton.IsEnabled = false;
        }

        //Check if file directory selection is valid.
        public bool isFileSet()
        {
            Console.WriteLine(!fileName.Text.Equals("Type file name..."));
            return Directory.Exists(fileDirectory.Text) && !fileName.Text.Equals("Type file name...");
        }
        
        //Handle starting capture
        private async void startButtonClick(object sender, RoutedEventArgs e)
        {
            DebugLog.Text = "";
            failed = "";
            //Capture arguments from textboxes and check validity
            String[] captureArguments = { framerateTB.Text, concurrencyTB.Text, minSaveTimeTB.Text };
            foreach (string argument in captureArguments)
            {
                int value;
                if (!int.TryParse(argument, out value))
                    failed = "Arguments must be integers.";
            }
            if (!isFileSet())
                failed = "File directory invalid";

            //If all checks out -> start capture
            if (failed.Equals(""))
            {
                captureRunning = true;
                setStartButton(false);
                setStopButton(true);
                fileName.IsEnabled = false;
                MainWindow.fName = fileName.Text;
                int framerate = int.Parse(captureArguments[0]);
                int concurrency = int.Parse(captureArguments[1]);
                int minsave = int.Parse(captureArguments[2]);

                //Capture important numbers for tracking stats
                float cfg_min_elapsed = 1.0f / framerate;
                float greed = cfg_min_elapsed / 10.0f;
                float min_elapsed = cfg_min_elapsed - greed;
                DebugLog.Text += "~~~~~ STARTING CAPTURE ~~~~~\n\n";
                DebugLog.Text += "~~~~~ ARGUMENTS ~~~~~\n";
                DebugLog.Text += (string.Format("Capture rate: {0}fps\n", framerate));
                DebugLog.Text += (string.Format("Greed: {0:0.##} sec\n", greed));
                DebugLog.Text += (string.Format("Min frame time: {0:0.##} sec\n", min_elapsed));
                DebugLog.Text += (string.Format("Min save time: {0:0.##} sec\n", minsave));
                DebugLog.Text += (string.Format("Concurrency: {0} req\n", concurrency));
                //Start capture loop
                while (true && captureRunning)
                {
                    EchoCapturer currcap = new EchoCapturer(
                        framerate,
                        concurrency,
                        greed,
                        minsave,
                        this.frameCounter,
                        this.statusChange
                    );
                    try
                    {
                        await currcap.capture();
                        //await currcap.capture();
                    }
                    catch (HttpRequestException) 
                    {
                        await currcap.check_save();
                        stopButtonClick(this, new RoutedEventArgs());
                        DebugLog.Text = "\nCouldn't connect to API... Try again...";
                        break;
                    }
                    catch (Exception h)
                    {
                        await currcap.check_save();
                        stopButtonClick(this, new RoutedEventArgs());
                        DebugLog.Text = "New Error\nReport to sneakyevil#1967 on discord\n" + h;
                    }

                    if (currcap.frames.Count > 0)
                    {
                        try
                        {
                            await currcap.check_save();
                        }
                        catch (Exception g)
                        {
                            Console.WriteLine(g.ToString() + "\n");
                            break;
                        }
                    }
                }
            } else
            {
                DebugLog.Text = failed;
            }

        }

        //Handle stop button
        private void stopButtonClick(object sender, RoutedEventArgs e)
        {
            captureRunning = false;
            setStartButton(true);
            setStopButton(false);
            fileName.IsEnabled = true;
            DebugLog.Text = "";
        }

        //Handle start button
        public void setStartButton(bool value)
        {
            this.startButton.IsEnabled = value;
            DebugLog.Text = "";
        }

        //Set stop button when not exactly clicking it
        public void setStopButton(bool value)
        {
            this.stopButton.IsEnabled = value;
        }

        //Handle and drive directory selection
        private void fileButtonClick(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    MainWindow.filePath = fbd.SelectedPath + "\\";
                    fileDirectory.Text = MainWindow.filePath;
                }
            }
        }

        //Handle help button for arguments
        private void helpButtonClick(object sender, RoutedEventArgs e)
        {
            string message = "Framerate:\n" +
                "Desc: Target capture framerate\n" +
                "Default: 60fps\n\n" +
                "Concurrent API Calls:\n" +
                "Desc: Number of concurrent API calls\n" +
                "Default: 4\n\n" +
                "Min. Save Time:\n" +
                "Desc: Minimum length of time that must be captured in order for a capture to be eligible for saving\n" +
                "Default: 5s";
            MessageBox.Show(message, "Argument Help");
        }

        //Handle about button
        private void aboutClick(object sender, RoutedEventArgs e)
        {
            string message = "EchoVR Game Capturer V1.0\n" +
                "Developed by David Robidas (sneakyevil#1967) 2020\n" +
                "Some code taken from Quentin Young @ https://github.com/qlyoung";
            MessageBox.Show(message, "About Program");
        }
    }

    
    class EchoCapturer
    {
        public string[] recordstates = { "playing", "score", "round_start", "round_over", "pre_match" };
        private int caprate;
        private int concurrency;
        private float greed;
        private int minsavetime;

        public ArrayList frames = new ArrayList();
        private string laststate = "none";
        private string state = "none";
        private double lastclock = 0.0f;
        private double totalframetime = 0.0f;

        public TextBlock tb;
        public TextBlock statusText;

        public HttpClient session;
        public EchoCapturer(int caprate, int concurrency, float greed, int minsavetime, TextBlock fc, TextBlock statusText)
        {
            this.caprate = caprate;
            this.concurrency = concurrency;
            this.greed = greed;
            this.minsavetime = minsavetime;
            this.tb = fc;
            this.statusText = statusText;
        }
        //Stream out json file
        public async Task<String> save_game()
        {
            string fname = MainWindow.fName += ".json";
            double actual_caprate = 1.0 / (this.totalframetime / (this.frames.Count + 1));

            JObject outfile = new JObject(
                new JProperty("caprate", actual_caprate),
                new JProperty("nframes", this.frames.Count),
                new JProperty("frames", this.frames));

            using (StreamWriter file = File.CreateText(MainWindow.filePath + fname))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                outfile.WriteTo(writer);
            }

            return fname;

        }
        //Check if save is valid (frametime > minsavetime etc...)
        public async Task check_save()
        {
            Console.WriteLine(totalframetime.ToString());
            bool save = this.totalframetime >= this.minsavetime;
            if (save)
            {
                Console.WriteLine("\nSaving game...\n");
                tb.Text = "Saving game...";
                String fname = await save_game();
                Console.WriteLine("Done\n");
                Console.WriteLine(string.Format("Saved {0} frames to {1}\n", this.frames.Count.ToString(), MainWindow.filePath + fname));
                tb.Text = string.Format("Saved {0} frames to {1}\n", this.frames.Count.ToString(), MainWindow.filePath + fname);
            }
            else
            {
                Console.WriteLine(string.Format("Skipping save, less than {0} seconds of data.\n", this.minsavetime));
                tb.Text = string.Format("Skipping save, less than {0} seconds of data.\n", this.minsavetime);
            }
        }
        //Capture frame
        public async Task rx_frame()
        {
            //Connect to API and get resp
            HttpResponseMessage resp = await session.GetAsync("/session");
            string responseBody = await resp.Content.ReadAsStringAsync();
            JObject joResp = JObject.Parse(responseBody);
            JObject frame = (JObject)joResp;
            this.laststate = this.state;
            this.state = frame["game_status"].ToString();

            if (this.state != this.laststate && !(this.state.Equals("post_match") && this.frames.Count == 0))
            {
                statusText.Text = (string.Format("{0} -> {1}\n", this.laststate, this.state));
            }

            if (this.state.Equals("post_match"))
            {
                return;
            }

            if (this.recordstates.Any(this.state.Contains))
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                double currtime = (double)t.TotalSeconds;
                if (!this.recordstates.Any(this.laststate.Contains))
                {
                    this.lastclock = currtime - (1.0 / this.caprate);
                }
                double elapsed = currtime - this.lastclock;
                double cfg_min_elapsed = 1.0 / this.caprate;
                double min_elapsed = cfg_min_elapsed - this.greed;
                if (elapsed >= min_elapsed)
                {
                    this.lastclock = currtime;
                    this.totalframetime += elapsed;

                    double avg_ft = this.totalframetime / (this.frames.Count + 1);
                    this.frames.Add(frame);
                    //UI Text per frame%3
                    if (this.frames.Count % 3 == 0)
                    {
                        tb.Text = (string.Format("Captured frame {0} ({1:1.##} avg fps ({2:2.######} curr), min {3:3.####}, cfg {4:4.####})\n",
                            this.frames.Count,
                            1.0 / avg_ft,
                            1.0 / elapsed,
                            min_elapsed,
                            cfg_min_elapsed));
                    }
                }
            }
        }
        //First function for capture class
        public async Task capture()
        {
            session = new HttpClient();
            session.BaseAddress = new Uri("http://127.0.0.1");
            session.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //Exit condition
            while (this.state != "post_match" && MainWindow.captureRunning)
            {
                //Foreach concurrency, run a capture thread
                for (int _ = 0; _ < this.concurrency; _++)
                {
                    await rx_frame();
                }
            }
        }
    }
}
