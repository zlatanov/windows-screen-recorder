using System;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace WindowsScreenRecorder
{
    internal static class Direct3D11Helpers
    {
        [DllImport( "d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall )]
        private static extern uint CreateDirect3D11DeviceFromDXGIDevice( IntPtr dxgiDevice, out IntPtr graphicsDevice );

        public static IDirect3DDevice CreateDevice()
        {
            var d3dDevice = new SharpDX.Direct3D11.Device(
                driverType: SharpDX.Direct3D.DriverType.Hardware,
                flags: SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport );

            // Acquire the DXGI interface for the Direct3D device.
            using var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device3>();

            // Wrap the native device using a WinRT interop object.
            var hr = CreateDirect3D11DeviceFromDXGIDevice( dxgiDevice.NativePointer, out IntPtr abi );

            if ( hr != 0 )
                throw new InvalidProgramException( $"CreateDirect3D11DeviceFromDXGIDevice failed with error code {hr}." );

            return MarshalInterface<IDirect3DDevice>.FromAbi( abi );
        }
    }
}
