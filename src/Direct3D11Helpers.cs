using System;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace WindowsScreenRecorder
{
    internal static unsafe partial class Direct3D11Helpers
    {
        [LibraryImport( "d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice" )]
        private static partial uint CreateDirect3D11DeviceFromDXGIDevice( IntPtr dxgiDevice, out IntPtr graphicsDevice );

        [LibraryImport( "d3d11" )]
        private static partial uint D3D11CreateDevice( IDXGIAdapter* pAdapter, D3D_DRIVER_TYPE DriverType, nint Software, uint Flags, D3D_FEATURE_LEVEL* pFeatureLevels, uint FeatureLevels, uint SDKVersion, ID3D11Device** ppDevice, D3D_FEATURE_LEVEL* pFeatureLevel, ID3D11DeviceContext** ppImmediateContext );

        public static unsafe IDirect3DDevice CreateDevice()
        {
            ID3D11Device* device;
            ReadOnlySpan<D3D_FEATURE_LEVEL> features =
            [
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1
            ];

            uint hr;

            fixed ( D3D_FEATURE_LEVEL* pFeatureLevels = features )
            {
                ID3D11DeviceContext* immediateContext;
                D3D_FEATURE_LEVEL featureLevel = default;
                hr = D3D11CreateDevice( pAdapter: null,
                                   D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                                   Software: default,
                                   (uint)D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                                   pFeatureLevels,
                                   FeatureLevels: (uint)features.Length,
                                   SDKVersion: 0x00000007U,
                                   &device,
                                   &featureLevel,
                                   &immediateContext );

                if ( hr != 0 )
                    throw new InvalidProgramException( $"D3D11CreateDevice failed with error code {hr}." );
            }

            hr = CreateDirect3D11DeviceFromDXGIDevice( (nint)device, out var graphicsDevice );
            Marshal.Release( (nint)device );

            if ( hr != 0 )
                throw new InvalidProgramException( $"CreateDirect3D11DeviceFromDXGIDevice failed with error code {hr}." );

            return MarshalInterface<IDirect3DDevice>.FromAbi( graphicsDevice );
        }
    }
}
