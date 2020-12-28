using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace WindowsScreenRecorder
{
    internal static class Monitor
    {
        public static GraphicsCaptureItem CreateCaptureItem( string? deviceName )
        {
            GraphicsCaptureItem? result = null;
            var info = new MonitorInfoEx
            {
                Size = 104
            };

            bool Callback( IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData )
            {
                if ( !GetMonitorInfoW( hMonitor, ref info ) )
                    throw new Win32Exception();

                if ( deviceName is null && info.Flags.HasFlag( MonitorFlags.Primary ) )
                {
                    result = CreateItemForMonitor( hMonitor );
                    return false;
                }
                else if ( deviceName is not null && deviceName == info.DeviceName )
                {
                    result = CreateItemForMonitor( hMonitor );
                    return false;
                }

                return true;
            }

            EnumDisplayMonitors( IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero );

            if ( result is null )
            {
                if ( deviceName is not null )
                    throw new InvalidOperationException( $"Could not find display which has device name {deviceName}." );

                throw new InvalidOperationException( "Could not find primary display." );
            }

            return result;
        }

        private static GraphicsCaptureItem CreateItemForMonitor( IntPtr hmon )
        {
            var factory = WinrtModule.GetActivationFactory( "Windows.Graphics.Capture.GraphicsCaptureItem" );
            var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();

            var graphicsCaptureItemGuid = new Guid( "79C3F95B-31F7-4EC2-A464-632EF5D30760" );
            var hr = interop.CreateForMonitor( hmon, ref graphicsCaptureItemGuid, out var result );

            if ( hr != 0 )
                throw new Win32Exception( hr );

            return GraphicsCaptureItem.FromAbi( result );
        }

        delegate bool EnumMonitorsDelegate( IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData );

        [DllImport( "user32.dll" )]
        static extern bool EnumDisplayMonitors( IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate callback, IntPtr dwData );

        [DllImport( "user32.dll", ExactSpelling = true, SetLastError = true )]
        static extern bool GetMonitorInfoW( IntPtr hMonitor, ref MonitorInfoEx lpmi );

        [StructLayout( LayoutKind.Sequential )]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [Flags]
        public enum MonitorFlags : uint
        {
            Primary = 0x00000001
        }

        [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
        private unsafe struct MonitorInfoEx
        {
            public uint Size;
            public Rect Monitor;
            public Rect Work;
            public MonitorFlags Flags;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 32 )]
            public string DeviceName;
        }
    }
}
