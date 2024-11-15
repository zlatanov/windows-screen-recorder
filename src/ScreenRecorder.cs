using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace WindowsScreenRecorder
{
    public sealed partial class ScreenRecorder : IAsyncDisposable
    {
        public static async Task<ScreenRecorder> CreateAsync( Stream output, ScreenRecorderOptions? options = null )
        {
            options ??= ScreenRecorderOptions.Default;
            var device = Direct3D11Helpers.CreateDevice();

            try
            {
                var recorder = new ScreenRecorder( device, options );
                var transcodeResult = await recorder.Transcoder.PrepareMediaStreamSourceTranscodeAsync(
                    recorder.Source, output.AsRandomAccessStream(), recorder.Profile );

                if ( !transcodeResult.CanTranscode )
                    throw new InvalidProgramException( $"The transcoder failed to prepare with error code {transcodeResult.FailureReason}." );

                recorder.m_transcodeAction = transcodeResult.TranscodeAsync();

                return recorder;
            }
            catch
            {
                device.Dispose();
                throw;
            }
        }

        private ScreenRecorder( IDirect3DDevice device, ScreenRecorderOptions options )
        {
            var captureItem = Monitor.CreateCaptureItem( options.MonitorDeviceName );

            m_device = device;
            m_generator = new MediaSampleGenerator( device, captureItem );

            var width = captureItem.Size.Width;
            var height = captureItem.Size.Height;

            var videoProperties = VideoEncodingProperties.CreateUncompressed( MediaEncodingSubtypes.Bgra8, (uint)width, (uint)height );

            Profile = MediaEncodingProfile.CreateMp4( options.Quality );
            Profile.Audio = null;
            Transcoder = new MediaTranscoder
            {
                HardwareAccelerationEnabled = options.HardwareAccelerationEnabled
            };
            Source = new MediaStreamSource( new VideoStreamDescriptor( videoProperties ) )
            {
                IsLive = true,
                CanSeek = false,
                BufferTime = TimeSpan.Zero
            };
            Source.Starting += OnStarting;
            Source.SampleRequested += OnSampleRequested;
        }

        public MediaStreamSource Source { get; }

        public MediaTranscoder Transcoder { get; }

        public MediaEncodingProfile Profile { get; }

        public async ValueTask DisposeAsync()
        {
            if ( m_disposed )
                return;

            m_disposed = true;
            m_generator.Stop();

            try
            {
                if ( m_transcodeAction is not null )
                    await m_transcodeAction;
            }
            finally
            {
                m_generator.Dispose();
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
        private IAsyncActionWithProgress<double>? m_transcodeAction;
    }
}
