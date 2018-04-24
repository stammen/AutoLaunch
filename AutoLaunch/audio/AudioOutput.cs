//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Security.Cryptography;

namespace Audio
{
    public delegate void AudioAudioOutputComplete();

    public sealed class AudioOutput
    {
        private List<byte[]> m_audioData = new List<byte[]>();
        private readonly Mutex m_audioDataMutex = new Mutex();

        private AudioGraph m_audioGraph;
        private AudioDeviceOutputNode m_deviceOutputNode;
        private AudioFrameInputNode m_frameInputNode;
        private bool m_isFlushing;
        private bool m_isRunning;
        private WaveBufferReader m_waveBufferReader;
        public event AudioAudioOutputComplete OnAudioComplete;

        ~AudioOutput()
        {
            if (m_audioGraph != null)
            {
                m_isRunning = false;
                m_isFlushing = false;
                m_frameInputNode.Stop();
                m_audioGraph.Stop();
                m_audioGraph.Dispose();
                m_audioGraph = null;
            }
        }

        public bool IsPlaying()
        {
            return m_isRunning || m_isFlushing;
        }

        public void Stop()
        {
            m_audioDataMutex.WaitOne();
            m_isRunning = false;
            m_isFlushing = false;

            if (m_audioGraph != null)
            {
                m_audioGraph.Stop();
            }

            if (m_deviceOutputNode != null)
            {
                m_deviceOutputNode.Dispose();
                m_deviceOutputNode = null;
            }

            if (m_frameInputNode != null)
            {
                m_frameInputNode.Dispose();
                m_frameInputNode = null;
            }

            if (m_audioGraph != null)
            {
                m_audioGraph.Dispose();
                m_audioGraph = null;
            }
            m_audioData = null;
            m_audioDataMutex.ReleaseMutex();

        }

        public void Flush()
        {
            if (!m_isRunning)
            {
                return;
            }
            m_isRunning = false;
            m_audioDataMutex.WaitOne();
            m_isFlushing = m_audioData.Count() > 0 || m_waveBufferReader != null;
            m_audioDataMutex.ReleaseMutex();
        }

        public async Task Start()
        {
            m_isFlushing = false;
            m_isRunning = false;
            m_waveBufferReader = null;

            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            var result = await AudioGraph.CreateAsync(settings);
            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw new Exception("AudioGraph creation error: " + result.Status);
            }

            m_audioGraph = result.Graph;


            var outputDeviceResult = await m_audioGraph.CreateDeviceOutputNodeAsync();
            if (outputDeviceResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception("Unable to create audio playback device: " + result.Status);
            }

            m_deviceOutputNode = outputDeviceResult.DeviceOutputNode;

            // Create the FrameInputNode at the same format as the graph, 
            var nodeEncodingProperties = m_audioGraph.EncodingProperties;
            nodeEncodingProperties.ChannelCount = 1;
            nodeEncodingProperties.SampleRate = 16000;
            m_frameInputNode = m_audioGraph.CreateFrameInputNode(nodeEncodingProperties);
            m_frameInputNode.AddOutgoingConnection(m_deviceOutputNode);
            m_frameInputNode.QuantumStarted += OnQuantumStarted;

            m_isRunning = true;
            m_isFlushing = false;
            m_audioGraph.Start();
        }

        private void OnQuantumStarted(AudioFrameInputNode node, FrameInputNodeQuantumStartedEventArgs args)
        {
            var numSamplesNeeded = args.RequiredSamples;

            if (numSamplesNeeded != 0)
            {
                var audioData = GenerateAudioData(numSamplesNeeded);
                m_frameInputNode.AddFrame(audioData);
            }

            if (!m_isRunning && !m_isFlushing)
            {
                if(OnAudioComplete != null)
                {
                    OnAudioComplete();
                }
                m_frameInputNode.Stop();
                m_audioGraph.Stop();
            }
        }

        private unsafe AudioFrame GenerateAudioData(int samples)
        {
            var frame = new AudioFrame((uint) samples * sizeof(float));
            try
            {
                using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacity;
                    float* dataInFloat;

                    // Get the buffer from the AudioFrame
                    ((IMemoryBufferByteAccess) reference).GetBuffer(out dataInBytes, out capacity);
                    dataInFloat = (float*) dataInBytes;

                    var sampleIndex = 0;
                    while ((m_audioData.Count() > 0 || m_waveBufferReader != null) && sampleIndex < samples)
                    {
                        // extract the next audio buffer from the queue
                        if (m_waveBufferReader == null)
                        {
                            m_audioDataMutex.WaitOne();
                            m_waveBufferReader = WaveBufferReader.Create(m_audioData[0]);
                            m_audioData.RemoveAt(0);
                            m_audioDataMutex.ReleaseMutex();
                        }

                        // read samples from WaveBufferReader until output buffer is full
                        // or input buffer is empty
                        var eof = false;
                        while (sampleIndex < samples && !eof)
                        {
                            var temp = m_waveBufferReader.ReadShort(ref eof);
                            if (!eof)
                            {
                                dataInFloat[sampleIndex++] = temp / 32768.0f;
                            }
                            else
                            {
                                // current reader is empty so delete it
                                m_waveBufferReader = null;
                            }
                        }
                    }

                    // fill remainder of buffer with silence
                    while (sampleIndex < samples)
                    {
                        dataInFloat[sampleIndex++] = 0.0f;
                        // check if we are done flushing audio after Stop()
                        if (!m_isRunning && m_isFlushing && m_audioData.Count() == 0 && m_waveBufferReader == null)
                        {
                            m_isFlushing = false;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return frame;
        }

        public void Send(byte[] data)
        {
            m_audioDataMutex.WaitOne();
            if (m_isRunning)
            {
                m_audioData.Add(data);
                while (m_audioData.Count() > 10)
                {
                    m_audioData.RemoveAt(0);
                }
            }

            m_audioDataMutex.ReleaseMutex();
        }

        public void Send(Windows.Storage.Streams.IBuffer data)
        {
            m_audioDataMutex.WaitOne();
            if (m_isRunning)
            {
                byte[] buffer;
                CryptographicBuffer.CopyToByteArray(data, out buffer);
                m_audioData.Add(buffer);
            }

            m_audioDataMutex.ReleaseMutex();
        }
    }
}