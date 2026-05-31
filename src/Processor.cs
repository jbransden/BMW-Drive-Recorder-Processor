using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace DriveRecorderConverter
{
    class Processor
    {
        private ProcessorParameter param;
        private string log;
        private string directory;

        public Processor(ProcessorParameter param, string directory) {
            this.param = param;
            this.directory = directory;
            log = "";
        }

        public string getDir() { return directory; }

        // Process video files
        public bool process()
        {
            string filename = String.Empty;

            if (directory == null) return false;
            if (param.OutputFileName == null) return false;

            foreach (string file in Directory.GetFiles(directory, "*.ts"))
            {
                filename = file;
                break;
            }

            string outputFilename = param.OutputFileName;
            string fileType = outputFilename.Substring(outputFilename.LastIndexOf('.'));
            outputFilename = outputFilename.Substring(0, outputFilename.LastIndexOf('.'));

            if (outputFilename.Contains("%"))
            {
                string originalFile = Path.GetFileName(filename);
                outputFilename = outputFilename.Replace("%file%", originalFile.Replace(".ts", ""));
            }

            if (param.AppendDateTime)
            {
                string dateSuffix = "";
                try
                {
                    string metaJson;
                    using (FileStream metaStream = File.OpenRead(directory + "\\Metadata.json"))
                    using (StreamReader metaReader = new StreamReader(metaStream))
                    {
                        metaJson = metaReader.ReadToEnd();
                    }
                    Metadata[]? metadatas = JsonSerializer.Deserialize<Metadata[]>(metaJson);
                    if (metadatas != null && metadatas.Length > 0 && metadatas[0].entries.Length > 0)
                    {
                        Entry firstEntry = metadatas[0].entries[0];
                        string entryDate = firstEntry.date ?? "";
                        string entryTime = (firstEntry.time ?? "").Replace(":", "-");

                        dateSuffix = entryDate.Replace("/", "-").Replace(".", "-").Replace(":", "-") + "_" + entryTime;
                    }
                }
                catch { }

                if (dateSuffix.Length > 0)
                {
                    outputFilename += "_" + dateSuffix;
                }
            }

            // complex filters
            string complexFilters = "";
            string fontsDir = Path.Combine(AppContext.BaseDirectory, "fonts").Replace("\\", "/\\").Replace(":", "\\\\:");
            string fontsDirOpt = "\\:fontsdir=" + fontsDir;
            string subtitlesFilter = "ass=" + directory.Replace("\\", "/\\").Replace(":", "\\\\:") + "/\\subtitles";

            string? rearName = null;
            string? frontName = null;
            string? leftName = null;
            string? rightName = null;
            string? videoName = null;
            if (param.SplitVideo || param.ScaleVideo || param.FlipRearCam)
            {
                switch (param.CameraSelection)
                {
                    case CameraSelection.AllCams:
                        complexFilters = "[0]crop=iw:ih/2:0:0[top1];[0]crop=iw:ih/2:0:0[top2];[0]crop=iw:ih/2:0:ih/2[bottom1];[0]crop=iw:ih/2:0:ih/2[bottom2];[top1]crop=iw/2:ih:0:0[front];[top2]crop=iw/2:ih:iw/2:0[rear];[bottom1]crop=iw/2:ih:0:0[left];[bottom2]crop=iw/2:ih:iw/2:0[right]";
                        rearName = "rear";
                        frontName = "front";
                        leftName = "left";
                        rightName = "right";
                        break;
                    case CameraSelection.FrontAndRear:
                        complexFilters = "[0]crop=iw:ih/2:0:0[front];[0]crop=iw:ih/2:0:ih/2[rear]";
                        frontName = "front";
                        rearName = "rear";
                        break;
                    case CameraSelection.OnlyFront:
                        complexFilters = "";
                        frontName = "0";
                        break;
                }
                if (param.FlipRearCam && rearName != null)
                {
                    complexFilters += ";[" + rearName + "]hflip[rearflip]";
                    rearName = "rearflip";
                }
                if (param.ScaleVideo)
                {
                    if (rearName != null)
                    {
                        complexFilters += ";[" + rearName + "]scale=iw*" + param.ScaleFactor + ":ih*" + param.ScaleFactor + ",setsar=1:1[rearscale]";
                        rearName = "rearscale";
                    }
                    if (frontName != null)
                    {
                        complexFilters += ";[" + frontName + "]scale=iw*" + param.ScaleFactor + ":ih*" + param.ScaleFactor + ",setsar=1:1[frontscale]";
                        frontName = "frontscale";
                    }
                    if (leftName != null)
                    {
                        complexFilters += ";[" + leftName + "]scale=iw*" + param.ScaleFactor + ":ih*" + param.ScaleFactor + ",setsar=1:1[leftscale]";
                        leftName = "leftscale";
                    }
                    if (rightName != null)
                    {
                        complexFilters += ";[" + rightName + "]scale=iw*" + param.ScaleFactor + ":ih*" + param.ScaleFactor + ",setsar=1:1[rightscale]";
                        rightName = "rightscale";
                    }
                }
                if (!param.SplitVideo)
                {
                    if (frontName != null && rearName != null && rightName != null && leftName != null)
                    {
                        complexFilters += ";[" + frontName + "][" + rearName + "]hstack[newtop]";
                        complexFilters += ";[" + leftName + "][" + rightName + "]hstack[newbottom]";
                        complexFilters += ";[newtop][newbottom]vstack[combine]";
                        videoName = "combine";
                    }
                    else if (frontName != null && rearName != null)
                    {
                        complexFilters += ";[" + frontName + "][" + rearName + "]vstack[combine]";
                        videoName = "combine";
                    }
                    else
                    {
                        videoName = frontName;
                    }

                    if (param.EnableInterpolation)
                    {
                        complexFilters += ";[" + videoName + "]" + param.InterpolationAlgo + "[interpolated]";
                        videoName = "interpolated";
                    }
                    complexFilters += ";[" + videoName + "]" + subtitlesFilter + ".ass" + fontsDirOpt + "[subs]";
                    videoName = "subs";
                }
                else
                {
                    if (param.EnableInterpolation)
                    {
                        complexFilters += ";[" + frontName + "]" + param.InterpolationAlgo + "[frontinter]";
                        frontName = "frontinter";
                    }
                    complexFilters += ";[" + frontName + "]" + subtitlesFilter + "_front.ass" + fontsDirOpt + "[frontsub]";
                    frontName = "frontsub";
                    if (rearName != null)
                    {
                        if (param.EnableInterpolation)
                        {
                            complexFilters += ";[" + rearName + "]" + param.InterpolationAlgo + "[rearinter]";
                            rearName = "rearinter";
                        }
                        complexFilters += ";[" + rearName + "]" + subtitlesFilter + "_rear.ass" + fontsDirOpt + "[rearsub]";
                        rearName = "rearsub";
                    }
                    if (leftName != null)
                    {
                        if (param.EnableInterpolation)
                        {
                            complexFilters += ";[" + leftName + "]" + param.InterpolationAlgo + "[leftinter]";
                            leftName = "leftinter";
                        }
                        complexFilters += ";[" + leftName + "]" + subtitlesFilter + "_left.ass" + fontsDirOpt + "[leftsub]";
                        leftName = "leftsub";
                    }
                    if (rightName != null)
                    {
                        if (param.EnableInterpolation)
                        {
                            complexFilters += ";[" + rightName + "]" + param.InterpolationAlgo + "[rightinter]";
                            rightName = "rightinter";
                        }
                        complexFilters += ";[" + rightName + "]" + subtitlesFilter + "_right.ass" + fontsDirOpt + "[rightsub]";
                        rightName = "rightsub";
                    }
                }
            }
            else
            {
                videoName = "0";
                if (param.EnableInterpolation)
                {
                    complexFilters = "[0]" + param.InterpolationAlgo + "[interpolated]";
                    videoName = "interpolated";
                }
                complexFilters = "[" + videoName + "]" + subtitlesFilter + ".ass" + fontsDirOpt + "[subs]";
                videoName = "subs";
            }

            string ffmpegStr = param.OverwriteExisting ? "-y " : "";
            ffmpegStr += "-i \"" + filename + "\" -filter_complex \"" + complexFilters + "\" ";
            if (param.EnableInterpolation)
            {
                ffmpegStr += "-r " + param.Fps + " ";
            }
            if (param.TrimVideo)
            {
                ffmpegStr += "-ss 00:00:" + param.TrimFrom.ToString("00") + " -to 00:00:" + param.TrimTo.ToString("00") + " ";
            }
            if (param.SplitVideo)
            {
                ffmpegStr += "-map \"[" + frontName + "]\" \"" + directory + "\\" + outputFilename + "_front" + fileType + "\" ";
                if (rearName != null)
                    ffmpegStr += " -map \"[" + rearName + "]\" \"" + directory + "\\" + outputFilename + "_rear" + fileType + "\" ";
                if (leftName != null)
                    ffmpegStr += " -map \"[" + leftName + "]\" \"" + directory + "\\" + outputFilename + "_left" + fileType + "\" ";
                if (rightName != null)
                    ffmpegStr += " -map \"[" + rightName + "]\" \"" + directory + "\\" + outputFilename + "_right" + fileType + "\" ";
            }
            else
            {
                ffmpegStr += "-map \"[" + videoName + "]\" \"" + directory + "\\" + outputFilename + fileType + "\"";
            }

            // Create subtitles
            if (param.SplitVideo)
            {
                switch (param.CameraSelection)
                {
                    case CameraSelection.AllCams:
                        createSubtitles("left");
                        createSubtitles("right");
                        createSubtitles("rear");
                        createSubtitles("front");
                        break;
                    case CameraSelection.FrontAndRear:
                        createSubtitles("rear");
                        createSubtitles("front");
                        break;
                    case CameraSelection.OnlyFront:
                        createSubtitles("front");
                        break;
                }
            }
            else
            {
                createSubtitles(null);
            }

            bool result = runFfmpeg(ffmpegStr, true);

            foreach (string file in Directory.GetFiles(directory, "subtitles*.ass"))
            {
                File.Delete(file);
            }

            return result;
        }

        private bool createSubtitles(string? camera)
        {
            string metaJson;
            using (FileStream metaStream = File.OpenRead(directory + "\\Metadata.json"))
            using (StreamReader metaReader = new StreamReader(metaStream))
            {
                metaJson = metaReader.ReadToEnd();
            }

            Metadata[]? metadatas = JsonSerializer.Deserialize<Metadata[]>(metaJson);
            if (metadatas == null || metadatas.Length <= 0) {
                return false;
            }
            Metadata metadata = metadatas[0];

            string filename = "subtitles";
            if (camera != null)
            {
                filename += "_" + camera;
            }
            filename += ".ass";

            using FileStream subtitlesStream = File.OpenWrite(directory + "\\" + filename);
            using StreamWriter subtitlesWriter = new StreamWriter(subtitlesStream);
            
            // Genereic header
            subtitlesWriter.WriteLine("[Script Info]");
            subtitlesWriter.WriteLine("Title: Driverecorder recording");
            subtitlesWriter.WriteLine("ScriptType: v4.00+");
            subtitlesWriter.WriteLine("PlayDepth: 0");

            // Resolution
            subtitlesWriter.WriteLine("PlayResX: 1280");
            subtitlesWriter.WriteLine("PlayResY: 960");
            subtitlesWriter.WriteLine();

            // Styles
            subtitlesWriter.WriteLine("[V4+ Styles]");
            subtitlesWriter.WriteLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            subtitlesWriter.WriteLine("Style: Default,Asap Condensed Medium,30,&H00FFFFFF,&H0000FFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,1,1,2,10,10,10,1");
            subtitlesWriter.WriteLine();

            // Events
            subtitlesWriter.WriteLine("[Events]");
            subtitlesWriter.WriteLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            // Driver name, Registration and VIN — top-right overlay
            if (param.ShowDriverName || param.ShowCar || param.ShowVin)
            {
                subtitlesWriter.Write("Dialogue: 0,00:00:00.00,00:01:00.00,Default,,0,0,0,,");
                if (param.ShowDriverName) subtitlesWriter.Write("{\\an9}" + param.DriverName + "\\N");
                if (param.ShowCar) subtitlesWriter.Write("{\\fs20}" + param.Car + "\\N");
                if (param.ShowVin) subtitlesWriter.Write("{\\fs20}" + (metadata.VIN ?? "") + "\\N");
                subtitlesWriter.WriteLine();
            }

            // Camera names
            if (param.ShowCameraNames)
            {
                if (camera == null)
                {
                    switch (param.CameraSelection)
                    {
                        case CameraSelection.AllCams:
                            subtitlesWriter.WriteLine("Dialogue: 0,00:00:00.00,00:01:00.00,Default,,0,0,0,,{\\an4}FRONT\\N\\NLEFT");
                            subtitlesWriter.WriteLine("Dialogue: 0,00:00:00.00,00:01:00.00,Default,,0,0,0,,{\\an6}REAR\\N\\NRIGHT");
                            break;
                        case CameraSelection.FrontAndRear:
                            subtitlesWriter.WriteLine("Dialogue: 0,00:00:00.00,00:01:00.00,Default,,0,0,0,,{\\an4}FRONT\\N\\NREAR");
                            break;
                    }
                }
                else
                {
                    subtitlesWriter.WriteLine("Dialogue: 0,00:00:00.00,00:01:00.00,Default,,0,0,0,,{\\an7}" + camera.ToUpper());
                }
            }

            // All entries
            writeEntries(metadata.entries, subtitlesWriter);

            return true;
        }

        private bool runFfmpeg(string parameters, bool window)
        {
            bool success = false;
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
                    proc.StartInfo.Arguments = parameters;
                    proc.StartInfo.CreateNoWindow = !window;
                    proc.StartInfo.UseShellExecute = window;
                    proc.StartInfo.RedirectStandardError = !window;
                    proc.StartInfo.RedirectStandardOutput = !window;
                    proc.Start();
                    proc.WaitForExit();
                    success = proc.ExitCode == 0;
                    if (!window) log = proc.StandardError.ReadToEnd();
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2 /* ERROR_FILE_NOT_FOUND */)
            {
                log = "ffmpeg not found. Please ensure ffmpeg is installed and available on your system PATH.";
                success = false;
            }
            return success;
        }

        public string getLog()
        {
            return log;
        }

        private void writeEntries(Entry[] entries, StreamWriter writer)
        {
            double frameTime = 1.0 / 30.0;

            foreach (Entry entry in entries)
            {
                int frame = entry.id;
                TimeSpan fromTime = TimeSpan.FromSeconds((frame - 1) * frameTime);
                TimeSpan toTime = TimeSpan.FromSeconds(frame * frameTime);

                string timeFromStr = fromTime.ToString("G").Substring(2, 11).Replace(",", ".");
                string timeToStr = toTime.ToString("G").Substring(2, 11).Replace(",", ".");

                string frameBaseStr = "Dialogue: 0," + timeFromStr + "," + timeToStr + ",Default,,0,0,0,,";

                double velocity = 0.0;

                if (param.IDriveVersion == 7)
                {
                    // iDrive 7 uses a single velocity field (km/h)
                    velocity = entry.velocity ?? 0.0;
                }
                else
                {
                    // iDrive 8 has separate km/h and mph fields
                    if (param.MphMode)
                    {
                        velocity = Double.Parse(entry.velocity_mph ?? "0.0", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        velocity = entry.velocity_kmh ?? 0.0;
                    }
                }




                string frameSubs = "";
                if (param.ShowDateTime)
                {
                    frameSubs = "{\\an2}" + entry.date + " - " + entry.time;
                    writer.WriteLine(frameBaseStr + frameSubs);
                }

                if (param.ShowSpeed)
                {
                    frameSubs = "{\\an3}";
                    if (param.MphMode)
                    {
                        frameSubs += velocity.ToString("0.00") + " mph";
                    }
                    else
                    {
                        frameSubs += velocity.ToString("0.00") + " km/h";
                    }
                    writer.WriteLine(frameBaseStr + frameSubs);
                }

                if (param.ShowLocation)
                {
                    frameSubs = "{\\an1}lat=" + entry.latitude.ToString("0.0000") + " long=" + entry.longitude.ToString("0.####");
                    writer.WriteLine(frameBaseStr + frameSubs);
                }
            }
        }
    }
}
