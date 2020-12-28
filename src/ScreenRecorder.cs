using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace WindowsScreenRecorder
{
    public sealed class ScreenRecorder : IDisposable
    {
        public static ScreenRecorder Create( Stream output, ScreenRecorderOptions? options = null )
        {
            options ??= ScreenRecorderOptions.Default;
            var device = Direct3D11Helpers.CreateDevice();

            try
            {
                var recorder = new ScreenRecorder( device, options );
                var prepareTranscode = recorder.Transcoder
                    .PrepareMediaStreamSourceTranscodeAsync( recorder.Source, output.AsRandomAccessStream(), recorder.Profile );

                if ( prepareTranscode.Status == AsyncStatus.Started )
                {
                    using var waitHandle = new ManualResetEvent( false );
                    prepareTranscode.Completed = ( _, __ ) => waitHandle.Set();
                    waitHandle.WaitOne();
                }

                prepareTranscode.GetResults().TranscodeAsync().Completed = ( progress, status ) =>
                {
                    try
                    {
                        progress.GetResults();
                    }
                    catch ( Exception ex )
                    {
                        recorder.m_exception = ExceptionDispatchInfo.Capture( ex );
                    }
                    finally
                    {
                        recorder.m_stopped.Set();
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

        private ScreenRecorder( IDirect3DDevice device, ScreenRecorderOptions options )
        {
            var captureItem = Monitor.CreateCaptureItem( options.MonitorDeviceName );

            m_device = device;
            m_generator = new MediaSampleGenerator( device, captureItem );

            var width = captureItem.Size.Width;
            var height = captureItem.Size.Height;

            var videoProperties = VideoEncodingProperties.CreateUncompressed( MediaEncodingSubtypes.Bgra8, (uint)width, (uint)height );

            Profile = MediaEncodingProfile.CreateMp4( options.Quality );
            Transcoder = new MediaTranscoder
            {
                HardwareAccelerationEnabled = options.HardwareAccelerationEnabled
            };
            Source = new MediaStreamSource( new VideoStreamDescriptor( videoProperties ) )
            {
                IsLive = true
            };
            Source.Starting += OnStarting;
            Source.SampleRequested += OnSampleRequested;
        }

        public MediaStreamSource Source { get; }

        public MediaTranscoder Transcoder { get; }

        public MediaEncodingProfile Profile { get; }

        public void Dispose()
        {
            if ( m_disposed )
                return;

            m_disposed = true;
            m_generator.Stop();

            try
            {
                if ( !m_stopped.WaitOne( millisecondsTimeout: 30_000 ) )
                    throw new InvalidProgramException( "The recorder failed to stop in allotted time." );

                m_stopped.Dispose();
                m_exception?.Throw();
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
        private ExceptionDispatchInfo? m_exception;
        private readonly ManualResetEvent m_stopped = new ManualResetEvent( false );
    }
}
