# Low-Latency Audio Monitor

A minimal, standalone C# Windows application that captures audio from a selected microphone and plays it back through a selected output device (headphones/speakers) with real-time volume control.

## 🚀 Why This Was Built

Many users need to monitor their own microphone audio (to check levels, listen for background noise, or just hear themselves) without the lag introduced by standard Windows "Listen to this device" feature or heavy DAW software. 

This app was built to provide:
- **Near-Zero Latency**: By bypassing the high-level Windows audio stack and using low-level WinMM (Windows Multimedia) APIs.
- **Simplicity**: No complex configuration or external drivers needed.
- **Portability**: A single standalone executable with no external dependencies besides the .NET runtime.
- **Efficiency**: Minimal CPU and memory footprint, designed to run in the background.

## ✨ Features

- **Device Selection**: Choose exactly which input and output devices to use.
- **Real-Time Volume Control**: Adjust the monitored audio volume independently of system volume.
- **Minimize to Tray**: Keep the app running in the background without cluttering your taskbar.
- **Visual Feedback**: Simple UI with start/stop controls.

## 🛠️ Technical Details

The application is implemented using:
- **WinForms**: For the graphical user interface.
- **P/Invoke & WinMM**: Direct calls to `winmm.dll` for high-performance audio capture (`waveIn`) and playback (`waveOut`).
- **Buffer Management**: Uses multiple small (20ms) buffers to ensure continuous, low-latency audio streaming.
- **PCM Processing**: Raw 16-bit PCM audio data is processed and scaled in real-time for volume adjustment.

## 📥 How to Build

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or later recommended).

### Build Instructions
1. Clone this repository or download the source code.
2. Open a terminal in the project folder.
3. Run the following command:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true
   ```
4. Find the standalone `.exe` in `bin/Release/netX.X-windows/win-x64/publish/`.

## 🖱️ Usage
1. Launch the application.
2. Select your **Input Device** (Microphone).
3. Select your **Output Device** (Speakers/Headphones).
4. Adjust the **Volume Slider**.
5. Click **Start Monitoring**.
6. (Optional) Minimize the window to hide it in the system tray. Right-click the tray icon to restore or exit.

---
*Built with ❤️ for high-performance audio monitoring.*
