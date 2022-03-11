
<img alt="Replay Viewer Logo" width="80px" src="Demo Viewer/Assets/Images/Icons/replaylogo.png" align="left" />

## DESCRIPTION
The EchoVR Replay Viewer is able to play back data from an EchoVR Arena game that was saved through the public API into a replay file by presenting it in a 3D replaying software. The Replay Viewer can read files saved in the `.echoreplay` format. A description of how to create files in this format is [below](##-echoreplay-File-Format).

## INSTALLATION

### Oculus Store Version (preferred)

The Oculus Store version will get automatic updates. Install via the Oculus Store page:

https://www.oculus.com/experiences/rift/3228453177179864 

<img alt="How to launch in 2D mode Oculus Store" width="200px" src="Demo Viewer/Assets/Images/OculusStore2D.png" />
 
### GitHub Release

We will try to keep the latest GitHub release up-to-date for people that may not have access to the Oculus desktop app (Quest users). To install, extract the zip to a folder and run the `.exe` file. To run in VR mode, run the `.exe` with the `-useVR` command-line argument.

### Capturer (legacy)

 1. Download the Game-Capturer.zip in builds/ to your computer and unzip to a folder.
 2. Edit file path and file name as well as execution arguments. Click start and stop capture to start and stop capture respectively.

## USAGE

### Loading Files

To load a file, click on the menu icon on the right side of the screen (shortcut: tilde). This menu contains a list of `.echoreplay` files in the folder that Spark saves files to by default (`C:\Users\[USERNAME]\Documents\Spark\replays\`). At the top of this menu, there is an input field, where any file path can be entered if the replay is stored somewhere else on your computer.

To save your own replays, download [Spark](https://www.ignitevr.gg/Spark)!

### File Association:

File association is supported with the replay viewer, to make it easier for you to load downloaded files, you can associate `.echoreplay` files with ReplayViewer.exe by double clicking a `.echoreplay` file. When Windows brings up the "How do you want to echoreplay files from now on?" window, click "More apps", scroll down and select "Look for another app on this PC". Navigate to where you unzipped the application (GitHub Release), or to `C:\Program Files\Oculus\Software\Software\franzco-echodata\Replay Viewer.exe` (Oculus Store), and select "ReplayViewer.exe". Once you have done this, you can open `.echoreplay` files just by opening a file directly from File Explorer.

If you do not do this and you want to open files downloaded from the internet, you will have to copy the entire file location and file name of the file you want to view, and paste it into the field after launching the app. Files recorded with Spark will save to a folder readable by the Replay Viewer and will not require this process to view.

### Controls:

#### Keyboard
* *WASD* - camera movement
* *Q/E* - descend/ascend
* *Shift* - camera speed boost
* *Mouse Wheel* - adjust camera movement speed 


#### XBOX Controller:
* *Left Stick* - camera forward/back movement
* *Right Stick* - camera pitch/yaw
* *Left Bumper/Right Bumper* - descend/ascend
* *A Button* - Play/Pause, resets back to 1x play if rewinding/fast forwarding
* *B Button* - Clear drawings on screen
* *Y Button* - Show last score details
* *Select/Left Action Button* - Enable/disable goal animations (default is enabled)
* *DPad Left* - Rewind (1x - 10x)
* *DPad Right* - Fast forward (1x - 10x)
* *Left Trigger* - Quick Scrub/Slow-Mo backwards, analog controlled on the trigger to adjust speed (less trigger pull = slower, full pull = 1.5x). Will revert to playing if replay was playing before trigger was used, can toggle whether to play or pause while scrubbing/slow-mo playing with *A*
* *Right Trigger* - Quick Scrub/Slow-Mo forwards, analog controlled on the trigger to adjust speed (less trigger pull = slower, full pull = 1.5x). Will revert to playing if replay was playing before trigger was used, can toggle whether to play or pause while scrubbing/slow-mo playing with *A*

## `.echoreplay` File Format

To record `.echoreplay` files without writing your own software, you can use Spark ([download](https://www.ignitevr.gg/Spark))

The `.echoreplay` file format is a complete storage format for time-series EchoVR API request data. There are two versions of this format - compressed and uncompressed, both with the same file extension. The compressed version is simple the uncompressed file in a renamed `.zip` file.

The format for the uncompressed file is as follows:
* One line per API request.
* Each line contains the a timestamp, the `tab` character, and the full JSON data from the game's API

Due to the high efficiency of zip compression, binary formats such as `.milk` (once modified to include all the data necessary for replays) provide only marginal or no benefits over the compressed `.echoreplay`. These formats also require modification for every API change from the game, unlike `.echoreplay`.


## QUESTIONS

If you have questions, DM **sneakyevil#1967** on discord, or join the discord server for this project at https://discord.gg/srWMCnD
 
