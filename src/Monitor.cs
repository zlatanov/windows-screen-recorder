using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Graphics.Capture;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace WindowsScreenRecorder
{
    internal static partial class Monitor
    {
        public static unsafe GraphicsCaptureItem CreateCaptureItem( string? deviceName )
        {
            GraphicsCaptureItem? result = null;
            var info = new MONITORINFOEXW
            {
                monitorInfo =
                {
                    cbSize = (uint)sizeof( MONITORINFOEXW ),
                },
                szDevice = default
            };

            unsafe BOOL Callback( HMONITOR hMonitor, HDC param1, RECT* param2, LPARAM param3 )
            {
                if ( !PInvoke.GetMonitorInfo( hMonitor, ref Unsafe.AsRef<MONITORINFO>( Unsafe.AsPointer( ref info ) ) ) )
                    throw new Win32Exception();

                if ( deviceName is null && ( info.monitorInfo.dwFlags & 0x00000001 ) == 0x00000001 )
                {
                    result = CreateItemForMonitor( hMonitor );
                    return false;
                }
                else if ( deviceName is not null && info.szDevice.AsReadOnlySpan().StartsWith( deviceName, StringComparison.Ordinal ) )
                {
                    result = CreateItemForMonitor( hMonitor );
                    return false;
                }

                return true;
            }

            PInvoke.EnumDisplayMonitors( default, new RECT?(), new MONITORENUMPROC( Callback ), default );

            if ( result is null )
            {
                if ( deviceName is not null )
                    throw new InvalidOperationException( $"Could not find display which has device name {deviceName}." );

                throw new InvalidOperationException( "Could not find primary display." );
            }

            return result;
        }

        private static GraphicsCaptureItem CreateItemForMonitor( nint hmon )
        {
            var iid = new Guid( "79C3F95B-31F7-4EC2-A464-632EF5D30760" );
            var hr = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>().CreateForMonitor( hmon, ref iid, out var result );

            if ( hr > 0 )
                throw new Win32Exception( hr );

            return GraphicsCaptureItem.FromAbi( result );
        }
    }

    [GeneratedComInterface]
    [Guid( "3628E81B-3CAC-4C60-B7F4-23CE0E0C3356" )]
    [InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
    internal partial interface IGraphicsCaptureItemInterop
    {
        int CreateForWindow( IntPtr window, ref Guid iid, out IntPtr result );

        int CreateForMonitor( IntPtr monitor, ref Guid iid, out IntPtr result );
    }
}
