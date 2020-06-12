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

                int framerate = int.Parse(captureArguments[0]);
                int concurrency = int.Parse(captureArguments[1]);
                int minsave = int.Parse(captureArguments[2]);
                if (File.Exists(MainWindow.filePath + fileName.Text + ".json"))
                {
                    //stopButtonClick(this, new RoutedEventArgs());
                    DebugLog.Text = "\nFile already exists!";

                    fileName.Text = fileName.Text + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                }
                MainWindow.fName = fileName.Text;
                using (StreamWriter sw = File.AppendText(MainWindow.filePath + fileName.Text + ".json"))
                {
                    sw.Write("{\"frames\": [");
                }

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

                    if (currcap.totalFrameCount > 0)
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
            }
            else
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

        public List<string> sFrames = new List<string>();
        const int _frameBufferCount = 1000;
        public long totalFrameCount = 0;
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
        //Finalize the save of the capture, must be done for the JSON to be valid
        public async Task<String> save_game()
        {
            string fname = MainWindow.fName + ".json";
            double actual_caprate = 1.0 / (this.totalframetime / (this.totalFrameCount + 1));

            using (StreamWriter sw = File.AppendText(MainWindow.filePath + fname))
            {
                string lastFrame = this.sFrames.Last();
                foreach (var line in this.sFrames)
                {
                    sw.Write(line);
                    if (line != lastFrame)
                    {
                        sw.WriteLine(",");
                    }
                }
                sw.WriteLine("],");
                sw.Write(string.Format("\"caprate\":{0},\"nframes\":{1} }}", actual_caprate, this.totalFrameCount));
            }

            return fname;

        }
        //Check if save is valid (frametime > minsavetime etc...)
        public async Task check_save()
        {
            Console.WriteLine(totalframetime.ToString());
            bool save = this.totalframetime >= this.minsavetime;
            string fname = MainWindow.fName + ".json";
            if (save)
            {
                Console.WriteLine("\nSaving game...\n");
                tb.Text = "Saving game...";
                fname = await save_game();
                Console.WriteLine("Done\n");
                Console.WriteLine(string.Format("Saved {0} frames to {1}\n", this.totalFrameCount.ToString(), MainWindow.filePath + fname));
                tb.Text = string.Format("Saved {0} frames to {1}\n", this.totalFrameCount.ToString(), MainWindow.filePath + fname);
            }
            else
            {
                if (File.Exists(MainWindow.filePath + fname))
                {
                    File.Delete(MainWindow.filePath + fname);
                }
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
            sFrames.Add(responseBody);
            totalFrameCount++;

            JObject joResp = JObject.Parse(responseBody);
            JObject frame = (JObject)joResp;
            this.laststate = this.state;
            this.state = frame["game_status"].ToString();

            if (this.state != this.laststate && !(this.state.Equals("post_match") && totalFrameCount == 0))
            {
                statusText.Text = (string.Format("{0} -> {1}\n", this.laststate, this.state));
            }

            if (this.state != "post_match" && this.recordstates.Any(s => s == this.state))
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                double currtime = (double)t.TotalSeconds;
                if (!this.recordstates.Any(s => s == this.laststate))
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

                    double avg_ft = this.totalframetime / (totalFrameCount + 1);
                    //UI Text per frame%3
                    if (totalFrameCount % 3 == 0)
                    {
                        tb.Text = (string.Format("Captured frame {0} ({1:1.##} avg fps ({2:2.######} curr), min {3:3.####}, cfg {4:4.####})\n",
                            totalFrameCount,
                            1.0 / avg_ft,
                            1.0 / elapsed,
                            min_elapsed,
                            cfg_min_elapsed));
                    }
                }
            }

        }

        public void WriteCurrentFrames(List<string> frames)
        {
            string fname = MainWindow.fName + ".json";
            using (StreamWriter sw = File.AppendText(MainWindow.filePath + fname))
            {
                foreach (var line in frames)
                {
                    sw.Write(line);
                    sw.WriteLine(",");
                }
            }
        }

        //First function for capture class
        public async Task capture()
        {
            session = new HttpClient();
            session.BaseAddress = new Uri("http://127.0.0.1:6721");
            session.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //Exit condition
            while (this.state != "post_match" && MainWindow.captureRunning)
            {
                // Every X Frames, append the frames to the current file and clear them out of RAM
                if (sFrames.Count > _frameBufferCount)
                {
                    List<string> writeFrames = sFrames;
                    sFrames = new List<string>();
                    await Task.Run(() => WriteCurrentFrames(writeFrames));
                }
                //Foreach concurrency, run a capture thread
                for (int _ = 0; _ < this.concurrency; _++)
                {
                    await rx_frame();
                }
            }
        }
    }
}
