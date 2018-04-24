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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using NAudio.Wave;

namespace Audio
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public delegate void AudioAudioInputHandler(IWaveBuffer data);

    public sealed class AudioInput
    {
        private AudioGraph m_audioGraph;
        private AudioDeviceInputNode m_deviceInputNode;
        private AudioFrameOutputNode m_frameOutputNode;
        private readonly Mutex m_waveBufferMutex = new Mutex();
        private readonly List<IWaveBuffer> m_waveBuffers = new List<IWaveBuffer>();

        public event AudioAudioInputHandler OnAudioInput;

        ~AudioInput()
        {
            Stop();
        }

        public void Stop()
        {
            if (m_audioGraph != null)
            {
                m_audioGraph.Stop();
                m_audioGraph.Dispose();
                m_audioGraph = null;
            }
        }

        private IWaveBuffer GetWaveBuffer(uint size)
        {
            IWaveBuffer waveBuffer = null;
            m_waveBufferMutex.WaitOne();
            var count = m_waveBuffers.Count;
            //Debug.WriteLine(count);

            if (m_waveBuffers.Count > 0)
            {
                waveBuffer = m_waveBuffers[0];
                m_waveBuffers.RemoveAt(0);
            }
            m_waveBufferMutex.ReleaseMutex();

            // check if the current wavebuffer is the right size
            if (waveBuffer != null)
            {
                if (waveBuffer.ByteBuffer.Length != size)
                {
                    waveBuffer = null;
                }
            }

            if (waveBuffer == null)
            {
                var byteArray = new byte[size];
                waveBuffer = new WaveBuffer(byteArray);
            }

            return waveBuffer;
        }


        public void ReturnWaveBuffer(IWaveBuffer waveBuffer)
        {
            m_waveBufferMutex.WaitOne();
            m_waveBuffers.Add(waveBuffer);
            m_waveBufferMutex.ReleaseMutex();
        }

        public async Task Start()
        {
            var pcmEncoding = AudioEncodingProperties.CreatePcm(16000, 1, 16);

            // Construct the audio graph
            // mic -> Machine Translate Service
            // Machine Translation text to speech output -> speaker
            var result = await AudioGraph.CreateAsync(
                new AudioGraphSettings(AudioRenderCategory.Speech)
                {
                    DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw,
                    AudioRenderCategory = AudioRenderCategory.Speech,
                    EncodingProperties = pcmEncoding
                });

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw new Exception("AudioGraph creation error: " + result.Status);
            }

            m_audioGraph = result.Graph;

            m_frameOutputNode = m_audioGraph.CreateFrameOutputNode(pcmEncoding);

            var inputResult = await m_audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech, pcmEncoding);
            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception("AudioGraph CreateDeviceInputNodeAsync error: " + inputResult.Status);
            }

            m_deviceInputNode = inputResult.DeviceInputNode;
            m_deviceInputNode.AddOutgoingConnection(m_frameOutputNode);
            m_audioGraph.QuantumStarted += node_QuantumStarted;
            m_audioGraph.Start();
        }

        private unsafe void node_QuantumStarted(AudioGraph graph, object args)
        {
            var frame = m_frameOutputNode.GetFrame();
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacity;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess) reference).GetBuffer(out dataInBytes, out capacity);

                if (capacity > 0)
                {
                    dataInFloat = (float*) dataInBytes;
                    var numFrames = capacity / 4;
                    var waveBuffer = GetWaveBuffer(numFrames * sizeof(short));

                    var outData = waveBuffer.ShortBuffer;
                    for (uint i = 0; i < numFrames; i++)
                    {
                        var value = dataInFloat[i];
                        if (value >= 1.0)
                        {
                            outData[i] = 32767;
                            continue;
                        }
                        if (value <= -1.0)
                        {
                            outData[i] = -32768;
                            continue;
                        }

                        outData[i] = (short) (value * 32768.0f);
                        //Debug.WriteLine(outData[i]);
                    }

                    OnAudioInput(waveBuffer);
                }
            }
        }
    }
}