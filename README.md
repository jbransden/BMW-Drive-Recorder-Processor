# BMW Drive Recorder Processor

## Overview
A GUI that can be used to process the exported ts and json file from BMW Drive Recorder into an mp4 file.
It utilises [ffmpeg](https://ffmpeg.org/) to process the video into the desired output file.

<img width="398" height="610" alt="image" src="https://github.com/jbransden/BMW-Drive-Recorder-Processor/blob/main/Screenshot-main-window.png" />

### Features:
* Converts drive recorder .ts files into any video format supported by ffmpeg
* Adds the following metadata (or additional information) to an output video:
	* Driver name
	* Registration
	* VIN
	* Camera titles (front, rear, left and right)
	* Speed
	* GPS location (long. and lat.)
	* Date and time
* Frame interpolation to increase framerate
* Splitting the one video into different files for each camera
* Flipping the rear camera image, as it is mirrored by default
* Trimming the video
* Upscale video
* Save last settings

### Requirements:
* BMW iDrive major versions 7 and 8 supported

	* Other version of iDrive may be supported for newer models depending on the exported files.
	* You can attempt to select iDrive 8 for newer versions and if there is an error please add it.

All required tools are bundled with the exe for ease which includes:
* ffmpeg
* .NET 10

### Known Bugs:
* The tool creates a log file if ffmpeg finishes with an error, but it will not write the output of ffmpeg into it
* On low-resolution screens the window may be too large

## Download
Download a zipped exe from the releases section: https://github.com/jbransden/BMW-Drive-Recorder-Processor/releases
