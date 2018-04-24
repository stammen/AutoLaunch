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
using System.Diagnostics;
using NAudio.Wave;

namespace Audio
{
    public class WaveBufferReader
    {
        private int m_bytesRemaining;
        private int m_readIndex;
        private IWaveBuffer m_waveBuffer;

        private WaveBufferReader(IWaveBuffer buffer)
        {
            m_waveBuffer = buffer;
            m_bytesRemaining = m_waveBuffer.ByteBuffer.Length;
            m_readIndex = 0;
        }

        public static WaveBufferReader Create(byte[] buffer)
        {
            IWaveBuffer waveBuffer = new WaveBuffer(buffer);
            return new WaveBufferReader(waveBuffer);
        }

        ~WaveBufferReader()
        {
            m_waveBuffer = null;
        }

        public short ReadShort(ref bool eof)
        {
            eof = m_bytesRemaining <= 0;
            if (!eof)
            {
                try
                {
                    m_bytesRemaining -= sizeof(short);
                    return m_waveBuffer.ShortBuffer[m_readIndex++];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    m_bytesRemaining = 0;
                    eof = true;
                }
            }

            return 0;
        }
    }
}