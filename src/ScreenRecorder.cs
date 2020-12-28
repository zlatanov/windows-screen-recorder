using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace WindowsScreenRecorder
{
    public sealed class ScreenRecorder : IAsyncDisposable
    {
        public static async Task<ScreenRecorder> CreateAsync( Stream output, ScreenRecorderOptions? options = null )
        {
            options ??= ScreenRecorderOptions.Default;
            var device = Direct3D11Helpers.CreateDevice();

            try
            {
                var captureItem = Monitor.CreateCaptureItem( options.MonitorDeviceName );
                var width = captureItem.Size.Width;
                var height = captureItem.Size.Height;

                var videoProperties = VideoEncodingProperties.CreateUncompressed( MediaEncodingSubtypes.Bgra8, (uint)width, (uint)height );
                var videoDescriptor = new VideoStreamDescriptor( videoProperties );

                var source = new MediaStreamSource( videoDescriptor )
                {
                    IsLive = true
                };
                var recorder = new ScreenRecorder( device, source, captureItem );
                var transcoder = new MediaTranscoder
                {
                    HardwareAccelerationEnabled = options.HardwareAccelerationEnabled
                };

                var encodingProfile = MediaEncodingProfile.CreateMp4( options.Quality );
                var transcode = await transcoder.PrepareMediaStreamSourceTranscodeAsync( source, output.AsRandomAccessStream(), encodingProfile );

                transcode.TranscodeAsync().Completed = ( progress, __ ) =>
                {
                    try
                    {
                        progress.GetResults();
                        recorder.m_stopped.SetResult();
                    }
                    catch ( Exception ex )
                    {
                        recorder.m_stopped.SetException( ex );
                    }
                };

                return recorder;
            }
            catch
            {
                device.Dispose();
                throw;
            }
        }

        private ScreenRecorder( IDirect3DDevice device, MediaStreamSource source, GraphicsCaptureItem captureItem )
        {
            m_device = device;
            m_generator = new MediaSampleGenerator( device, captureItem );

            source.Starting += OnStarting;
            source.SampleRequested += OnSampleRequested;
        }

        public async ValueTask DisposeAsync()
        {
            if ( m_disposed )
                return;

            Debug.Assert( m_generator is not null );

            m_disposed = true;
            m_generator.Dispose();

            try
            {
                await m_stopped.Task;
            }
            finally
            {
                m_device.Dispose();
            }
        }

        private void OnStarting( MediaStreamSource sender, MediaStreamSourceStartingEventArgs args )
        {
            m_generator.Start( args.Request );
        }

        private void OnSampleRequested( MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args )
        {
            m_generator.Generate( args.Request );
        }

        private readonly IDirect3DDevice m_device;
        private readonly MediaSampleGenerator m_generator;

        private bool m_disposed;
        private readonly TaskCompletionSource m_stopped = new TaskCompletionSource();
    }
}
