﻿using System;
using System.Diagnostics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;

namespace WindowsScreenRecorder
{
    internal sealed class MediaSampleGenerator : IDisposable
    {
        public MediaSampleGenerator( IDirect3DDevice device, GraphicsCaptureItem item )
        {
            m_item = item;
            m_framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                device,
                pixelFormat: DirectXPixelFormat.B8G8R8A8UIntNormalized,
                numberOfBuffers: 1,
                item.Size );
            m_session = m_framePool.CreateCaptureSession( m_item );
            m_session.IsCursorCaptureEnabled = true;

            m_item.Closed += OnClosed;
            m_framePool.FrameArrived += OnFrameArrived;
        }

        public void Start( MediaStreamSourceStartingRequest request )
        {
            lock ( m_lock )
            {
                Debug.Assert( !m_started );

                m_startRequest = request;
                m_startRequestCompletion = request.GetDeferral();
                m_started = true;
                m_session.StartCapture();
            }
        }

        public void Dispose()
        {
            lock ( m_lock )
            {
                if ( m_disposed )
                    return;

                m_disposed = true;
                m_item.Closed -= OnClosed;

                m_framePool.Dispose();
                m_session.Dispose();

                if ( m_started )
                {
                    if ( m_startRequest is not null )
                    {
                        m_startRequest.SetActualStartPosition( TimeSpan.Zero );
                        m_startRequestCompletion!.Complete();

                        m_startRequest = null;
                        m_startRequestCompletion = null;
                    }

                    if ( m_sampleRequest is not null )
                    {
                        m_sampleRequest.Sample = null;
                        m_sampleRequestCompletion!.Complete();

                        m_sampleRequest = null;
                        m_sampleRequestCompletion = null;
                    }

                    m_currentFrame?.Dispose();
                    m_currentFrame = null;
                }
            }
        }

        private void OnFrameArrived( Direct3D11CaptureFramePool sender, object args )
        {
            lock ( m_lock )
            {
                if ( m_disposed )
                    return;

                var frame = sender.TryGetNextFrame();

                if ( m_startRequest is not null )
                {
                    m_startRequest.SetActualStartPosition( frame.SystemRelativeTime );
                    m_startRequestCompletion!.Complete();

                    m_startRequest = null;
                    m_startRequestCompletion = null;
                }

                if ( m_sampleRequest is not null )
                {
                    m_frameAvailable = false;
                    m_sampleRequest.Sample = MediaStreamSample.CreateFromDirect3D11Surface( frame.Surface, frame.SystemRelativeTime );
                    m_sampleRequestCompletion!.Complete();

                    m_sampleRequest = null;
                    m_sampleRequestCompletion = null;
                }
                else
                {

                    m_frameAvailable = true;
                }

                m_currentFrame = frame;
            }
        }

        private void OnClosed( GraphicsCaptureItem sender, object args ) => Dispose();

        public void Generate( MediaStreamSourceSampleRequest request )
        {
            lock ( m_lock )
            {
                if ( m_disposed )
                {
                    request.Sample = null;
                    return;
                }

                if ( m_frameAvailable )
                {
                    Debug.Assert( m_currentFrame is not null );

                    m_frameAvailable = false;
                    request.Sample = MediaStreamSample.CreateFromDirect3D11Surface( m_currentFrame.Surface, m_currentFrame.SystemRelativeTime );
                }
                else
                {
                    m_currentFrame?.Dispose();
                    m_currentFrame = null;

                    m_sampleRequest = request;
                    m_sampleRequestCompletion = request.GetDeferral();
                }
            }
        }

        private readonly object m_lock = new object();
        private bool m_disposed;
        private bool m_started;

        private MediaStreamSourceStartingRequest? m_startRequest;
        private MediaStreamSourceStartingRequestDeferral? m_startRequestCompletion;

        private MediaStreamSourceSampleRequest? m_sampleRequest;
        private MediaStreamSourceSampleRequestDeferral? m_sampleRequestCompletion;

        private bool m_frameAvailable;
        private Direct3D11CaptureFrame? m_currentFrame;

        private readonly GraphicsCaptureItem m_item;
        private readonly GraphicsCaptureSession m_session;
        private readonly Direct3D11CaptureFramePool m_framePool;
    }
}
