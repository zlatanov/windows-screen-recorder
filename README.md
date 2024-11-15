# Windows Screen Recorder
Screen recording library based on Windows.Graphics.Capture features in Windows 10. Uses .NET Core 5.0 to call into native Windows UWP APIs.

## How to use
Install the latest version of **WindowsScreenRecorder** from NuGet.
```c#
// Use any stream abstraction, in this example we're saving into file.
using var stream = File.Create( Path.Combine( Environment.CurrentDirectory, "recording.mp4" ) );
await using var recorder = await ScreenRecorder.CreateAsync( stream, new ScreenRecorderOptions
{
    HardwareAccelerationEnabled = true,
    MonitorDeviceName = null, // Primary
    Quality = Windows.Media.MediaProperties.VideoEncodingQuality.HD720p
} );

// Dispose the instance when we want to complete the recording.
```

At the moment the library only supports recording mp4 video.
