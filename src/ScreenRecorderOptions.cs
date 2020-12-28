using Windows.Media.MediaProperties;

namespace WindowsScreenRecorder
{
    public sealed class ScreenRecorderOptions
    {
        public static ScreenRecorderOptions Default { get; } = new ScreenRecorderOptions();

        public VideoEncodingQuality Quality { get; set; } = VideoEncodingQuality.HD720p;

        public bool HardwareAccelerationEnabled { get; set; } = true;

        public string? MonitorDeviceName { get; set; }
    }
}
