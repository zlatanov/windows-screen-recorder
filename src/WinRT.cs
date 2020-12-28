using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinRT;

namespace WindowsScreenRecorder
{
    [ComImport]
    [Guid( "3628E81B-3CAC-4C60-B7F4-23CE0E0C3356" )]
    [InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
    internal interface IGraphicsCaptureItemInterop
    {
        int CreateForWindow( [In] IntPtr window, [In] ref Guid iid, out IntPtr result );

        int CreateForMonitor( [In] IntPtr monitor, [In] ref Guid iid, out IntPtr result );
    }

    [Guid( "00000035-0000-0000-C000-000000000046" )]
    internal unsafe struct IActivationFactoryVftbl
    {
        public readonly IInspectable.Vftbl IInspectableVftbl;
        private readonly void* _ActivateInstance;

        public delegate* unmanaged[Stdcall]< IntPtr, IntPtr*, int > ActivateInstance => (delegate* unmanaged[Stdcall]< IntPtr, IntPtr*, int >)_ActivateInstance;
    }

    internal class Platform
    {
        [DllImport( "api-ms-win-core-com-l1-1-0.dll" )]
        internal static extern int CoDecrementMTAUsage( IntPtr cookie );

        [DllImport( "api-ms-win-core-com-l1-1-0.dll" )]
        internal static extern unsafe int CoIncrementMTAUsage( IntPtr* cookie );

        [DllImport( "api-ms-win-core-winrt-l1-1-0.dll" )]
        internal static extern unsafe int RoGetActivationFactory( IntPtr runtimeClassId, ref Guid iid, IntPtr* factory );
    }

    internal static class WinrtModule
    {
        public static ObjectReference<IActivationFactoryVftbl> GetActivationFactory( string runtimeClassId )
        {
            var m = MarshalString.CreateMarshaler( runtimeClassId );
            
            try
            {
                var instancePtr = GetActivationFactory( MarshalString.GetAbi( m ) );

                return ObjectReference<IActivationFactoryVftbl>.Attach( ref instancePtr );
            }
            finally
            {
                m.Dispose();
            }
        }

        private static unsafe IntPtr GetActivationFactory( IntPtr hstrRuntimeClassId )
        {
            if ( s_cookie == IntPtr.Zero )
            {
                lock ( s_lock )
                {
                    if ( s_cookie == IntPtr.Zero )
                    {
                        IntPtr cookie;
                        Marshal.ThrowExceptionForHR( Platform.CoIncrementMTAUsage( &cookie ) );

                        s_cookie = cookie;
                    }
                }
            }

            Guid iid = typeof( IActivationFactoryVftbl ).GUID;
            IntPtr instancePtr;
            int hr = Platform.RoGetActivationFactory( hstrRuntimeClassId, ref iid, &instancePtr );

            if ( hr == 0 )
                return instancePtr;

            throw new Win32Exception( hr );
        }


        private static IntPtr s_cookie;
        private static readonly object s_lock = new object();
    }
}
