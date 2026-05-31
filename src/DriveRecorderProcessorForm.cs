using System.Text.Json;

namespace DriveRecorderConverter
{
    public partial class DriveRecorderProcessorForm : Form
    {
        private bool _wasFromFrontOnly = false;
        private bool _flipStateBeforeFrontOnly = false;
        private System.Windows.Forms.Timer? _statusTimer;
        public DriveRecorderProcessorForm()
        {
            InitializeComponent();
        }

        private void PathChooseBtn_Click(object sender, EventArgs e)
        {
            if (PathBox.Text != null && PathBox.Text.Length > 0)
                pathChooseDlg.InitialDirectory = PathBox.Text;

            if (pathChooseDlg.ShowDialog() == DialogResult.OK)
            {
                PathBox.Text = pathChooseDlg.SelectedPath;
            }
        }

        private void PathBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePath();
        }

        private void ValidatePath()
        {
            string path = PathBox.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                StartBtn.Enabled = false;
                PathValidationLabel.Text = "Check selected directory has one .ts and .json file.";
                PathValidationLabel.Visible = true;
                return;
            }

            // Check if the selected directory itself has exactly 1 .ts and 1 .json
            int tsCount = Directory.GetFiles(path, "*.ts").Length;
            int jsonCount = Directory.GetFiles(path, "*.json").Length;

            if (tsCount == 1 && jsonCount == 1)
            {
                StartBtn.Enabled = true;
                PathValidationLabel.Visible = false;
                return;
            }

            // Check subdirectories (multi-folder processing)
            bool hasValidSubDir = false;
            foreach (string dir in Directory.GetDirectories(path))
            {
                int subTs = Directory.GetFiles(dir, "*.ts").Length;
                int subJson = Directory.GetFiles(dir, "*.json").Length;
                if (subTs == 1 && subJson == 1)
                {
                    hasValidSubDir = true;
                }
                else if (subTs > 1 || subJson > 1)
                {
                    StartBtn.Enabled = false;
                    PathValidationLabel.Text = "Check selected directory has one .ts and .json file.";
                    PathValidationLabel.Visible = true;
                    return;
                }
            }

            StartBtn.Enabled = hasValidSubDir;
            PathValidationLabel.Visible = !hasValidSubDir;
            if (!hasValidSubDir)
                PathValidationLabel.Text = "Check selected directory has one .ts and .json file.";
        }

        private async void StartBtn_Click(object sender, EventArgs e)
        {
            ProcessorParameter param = new ProcessorParameter();
            param.Path = PathBox.Text;
            param.Car = CarNameBox.Text;
            param.DriverName = DriverNameBox.Text;

            if (AllCamsRadio.Checked) { param.CameraSelection = CameraSelection.AllCams; }
            if (RearCamRadio.Checked) { param.CameraSelection = CameraSelection.FrontAndRear; }
            if (FrontCamRadio.Checked) { param.CameraSelection = CameraSelection.OnlyFront; }

            param.ShowDriverName = DriverNameCheck.Checked;
            param.ShowCar = CarNameCheck.Checked;
            param.ShowCameraNames = CamNameCheck.Checked;
            param.ShowLocation = LocationCheck.Checked;
            param.ShowVin = VinCheck.Checked;
            param.ShowSpeed = SpeedCheck.Checked;

            param.ShowDateTime = DateTimeCheck.Checked;
            param.EnableInterpolation = InterpolateCheck.Checked;
            param.Fps = (string?)FpsCombo.SelectedItem ?? "60";
            param.InterpolationAlgo = (string?)InterpolateCombo.SelectedItem ?? "minterpolate";
            param.MphMode = MphModeCheck.Checked;
            param.IDriveVersion = int.Parse((string?)iDriveCombo.SelectedItem ?? "8");
            param.OverwriteExisting = OverwriteCheck.Checked;
            param.AppendDateTime = AppendDateCheck.Checked;
            param.OutputFileName = outputFileBox.Text;

            param.SplitVideo = SplitCheck.Checked;
            param.FlipRearCam = FlipCheck.Checked;
            param.ScaleVideo = ScaleCheck.Checked;
            param.ScaleFactor = (int)ScaleNum.Value;
            param.TrimVideo = TrimCheck.Checked;
            param.TrimFrom = (int)TrimFromNum.Value;
            param.TrimTo = (int)TrimToNum.Value;

            SaveSettings();

            if (!checkParams(param)) return;

            // Hide previous status and lock the interface
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            _statusTimer = null;
            StartBtn.Text = "Processing";
            MainFlowPanel.Enabled = false;

            List<Processor> processors = new List<Processor>();

            string[] tsFiles = Directory.GetFiles(param.Path, "*.ts");
            if (tsFiles.Length > 0) processors.Add(new Processor(param, param.Path));
            else
            {
                foreach (string dir in Directory.GetDirectories(param.Path))
                {
                    string[] tsFiles2 = Directory.GetFiles(dir, "*.ts");
                    if (tsFiles2.Length > 0) processors.Add(new Processor(param, dir));
                }
            }

            bool allSuccess = true;
            foreach (Processor processor in processors)
            {
                if (!processor.process()) allSuccess = false;
            }

            if (allSuccess)
            {
                StartBtn.Text = "Processing Complete ✓";
            }
            else
            {
                string log = "";
                foreach (Processor processor in processors)
                {
                    log += "Directory: " + processor.getDir() + "\n------------------------------------\n";
                    log += processor.getLog();
                }
                File.WriteAllText("log.log", log);
                StartBtn.Text = "Processing Failed ✗";
            }
            MainFlowPanel.Enabled = true;
            ValidatePath();

            // Clear status after 10 seconds
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 6000;
            _statusTimer.Tick += (s, ev) =>
            {
                StartBtn.Text = "Process";
                _statusTimer.Stop();
                _statusTimer.Dispose();
                _statusTimer = null;
            };
            _statusTimer.Start();
        }

        private async void DriveRecorderConverterForm_Load(object sender, EventArgs e)
        {
            _ = UpdateChecker.CheckForUpdatesAsync();

            if (File.Exists("settings.json"))
            {
                string configJson;
                using (FileStream configStream = File.OpenRead("settings.json"))
                using (StreamReader configReader = new StreamReader(configStream))
                {
                    configJson = configReader.ReadToEnd();
                }

                ProcessorParameter? param = JsonSerializer.Deserialize<ProcessorParameter>(configJson);
                if (param == null) return;

                PathBox.Text = param.Path;

                switch (param.CameraSelection)
                {
                    case CameraSelection.AllCams:
                        AllCamsRadio.Checked = true;
                        break;
                    case CameraSelection.OnlyFront:
                        FrontCamRadio.Checked = true;
                        break;
                    case CameraSelection.FrontAndRear:
                        RearCamRadio.Checked = true;
                        break;
                }

                CarNameBox.Text = param.Car;
                DriverNameBox.Text = param.DriverName;

                DriverNameCheck.Checked = param.ShowDriverName;
                CarNameCheck.Checked = param.ShowCar;
                CamNameCheck.Checked = param.ShowCameraNames;
                LocationCheck.Checked = param.ShowLocation;
                VinCheck.Checked = param.ShowVin;
                DateTimeCheck.Checked = param.ShowDateTime;
                SpeedCheck.Checked = param.ShowSpeed;

                InterpolateCheck.Checked = param.EnableInterpolation;
                SplitCheck.Checked = param.SplitVideo;
                TrimCheck.Checked = param.TrimVideo;
                FlipCheck.Checked = param.FlipRearCam;
                ScaleCheck.Checked = param.ScaleVideo;

                TrimFromNum.Value = param.TrimFrom;
                TrimToNum.Value = param.TrimTo == 0 ? 40 : param.TrimTo;
                ScaleNum.Value = param.ScaleFactor == 0 ? 2 : param.ScaleFactor;

                for (int i = 0; i < InterpolateCombo.Items.Count; i++)
                {
                    if ((string?)InterpolateCombo.Items[i] == param.InterpolationAlgo)
                    {
                        InterpolateCombo.SelectedIndex = i;
                        break;
                    }
                }
                if (InterpolateCombo.SelectedIndex == -1) InterpolateCombo.SelectedIndex = 0;
                for (int i = 0; i < FpsCombo.Items.Count; i++)
                {
                    if ((string?)FpsCombo.Items[i] == param.Fps)
                    {
                        FpsCombo.SelectedIndex = i;
                        break;
                    }
                }
                if (FpsCombo.SelectedIndex == -1) FpsCombo.SelectedIndex = 1;

                MphModeCheck.Checked = param.MphMode;
                OverwriteCheck.Checked = param.OverwriteExisting;
                AppendDateCheck.Checked = param.AppendDateTime;

                iDriveCombo.SelectedIndex = param.IDriveVersion == 7 ? 0 : 1;
                outputFileBox.Text = (param.OutputFileName == null || param.OutputFileName.Length == 0) ? "output.mp4" : param.OutputFileName;
            }
            else
            {
                FpsCombo.SelectedIndex = 1;
                InterpolateCombo.SelectedIndex = 0;
                StartBtn.Enabled = false;
            }
        }

        private void CameraSelection_CheckedChanged(object sender, EventArgs e)
        {
            if (FrontCamRadio.Checked)
            {
                _flipStateBeforeFrontOnly = FlipCheck.Checked;
                _wasFromFrontOnly = true;
                FlipCheck.Enabled = false;
                FlipCheck.Checked = false;
            }
            else
            {
                FlipCheck.Enabled = true;
                if (_wasFromFrontOnly)
                {
                    FlipCheck.Checked = _flipStateBeforeFrontOnly;
                    _wasFromFrontOnly = false;
                }
            }
        }

        private void InterpolateCheck_CheckedChanged(object sender, EventArgs e)
        {
            InterpolateCombo.Enabled = InterpolateCheck.Checked;
            FpsCombo.Enabled = InterpolateCheck.Checked;
            InterpolateDescLabel.Visible = InterpolateCheck.Checked;
        }

        private void InterpolateCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            InterpolateDescLabel.Text = (string?)InterpolateCombo.SelectedItem switch
            {
                "tblend" =>
                    "\r\ntblend (Fast & Blurry): Creates a motion blur effect by overlapping adjacent frames. " +
                    "Processes very quickly on almost any computer, but fast-moving objects will look like \"double exposure.\" " +
                    "Best for quick fixes rather than high-quality video smoothing.",
                "minterpolate" =>
                    "\r\nminterpolate (Slow & Smooth): Calculates actual movement to generate entirely new, artificial frames, " +
                    "resulting in genuinely smoother video playback. Takes significantly longer to process and requires a " +
                    "powerful computer, but delivers much higher visual quality and a truer high-framerate look.",
                _ => string.Empty
            };
        }

        private bool checkParams(ProcessorParameter param)
        {
            if (param.Path == null || param.Path.Length == 0)
            {
                MessageBox.Show("Path cannot be empty!");
                return false;
            }
            if (!Directory.Exists(param.Path))
            {
                MessageBox.Show("Path doesn't exist (anymore)");
                return false;
            }
            if (param.OutputFileName == null || param.OutputFileName.Length == 0)
            {
                MessageBox.Show("Output filename cannot be empty!");
                return false;
            }
            return true;
        }

        private void TrimCheck_CheckedChanged(object sender, EventArgs e)
        {
            TrimFromNum.Enabled = TrimCheck.Checked;
            TrimToNum.Enabled = TrimCheck.Checked;
        }

        private void ScaleCheck_CheckedChanged(object sender, EventArgs e)
        {
            ScaleNum.Enabled = ScaleCheck.Checked;
        }

        private void IDriveCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            base.OnFormClosing(e);
        }

        private void SaveSettings()
        {
            var param = new ProcessorParameter
            {
                Path = PathBox.Text,
                Car = CarNameBox.Text,
                DriverName = DriverNameBox.Text,
                CameraSelection = AllCamsRadio.Checked ? CameraSelection.AllCams
                    : RearCamRadio.Checked ? CameraSelection.FrontAndRear
                    : CameraSelection.OnlyFront,
                ShowDriverName = DriverNameCheck.Checked,
                ShowCar = CarNameCheck.Checked,
                ShowCameraNames = CamNameCheck.Checked,
                ShowLocation = LocationCheck.Checked,
                ShowVin = VinCheck.Checked,
                ShowSpeed = SpeedCheck.Checked,

                ShowDateTime = DateTimeCheck.Checked,
                EnableInterpolation = InterpolateCheck.Checked,
                Fps = (string?)FpsCombo.SelectedItem ?? "60",
                InterpolationAlgo = (string?)InterpolateCombo.SelectedItem ?? "minterpolate",
                MphMode = MphModeCheck.Checked,
                IDriveVersion = int.Parse((string?)iDriveCombo.SelectedItem ?? "8"),
                OverwriteExisting = OverwriteCheck.Checked,
                AppendDateTime = AppendDateCheck.Checked,
                OutputFileName = outputFileBox.Text,
                SplitVideo = SplitCheck.Checked,
                FlipRearCam = FlipCheck.Checked,
                ScaleVideo = ScaleCheck.Checked,
                ScaleFactor = (int)ScaleNum.Value,
                TrimVideo = TrimCheck.Checked,
                TrimFrom = (int)TrimFromNum.Value,
                TrimTo = (int)TrimToNum.Value
            };
            string settingsJson = JsonSerializer.Serialize(param);
            File.WriteAllText("settings.json", settingsJson);
        }
    }
}
