using System;
using System.Runtime.InteropServices;

namespace LibALAC
{
    /// <summary>
    ///     .NET standard wrapper for the native dynamic-link library (DLL) implementation of the Apple Lossless Audio Codec (ALAC) decoder.
    /// </summary>
    public class Decoder : IDisposable
    {
        [DllImport("LibALAC32.dll", EntryPoint = "InitializeDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr InitializeDecoder32(int sampleRate, int channels, int bitsPerSample, int framesPerPacket);
        [DllImport("LibALAC64.dll", EntryPoint = "InitializeDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr InitializeDecoder64(int sampleRate, int channels, int bitsPerSample, int framesPerPacket);

        [DllImport("LibALAC32.dll", EntryPoint = "Decode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Decode32(IntPtr decoder, byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);
        [DllImport("LibALAC64.dll", EntryPoint = "Decode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Decode64(IntPtr decoder, byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);

        [DllImport("LibALAC32.dll", EntryPoint = "FinishDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishDecoder32(IntPtr decoder);
        [DllImport("LibALAC64.dll", EntryPoint = "FinishDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishDecoder64(IntPtr decoder);

        [DllImport("LibALAC32.dll", EntryPoint = "ParseMagicCookie", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ParseMagicCookie32(byte[] inMagicCookie, int inMagicCookieSize, ref int outSampleRate, ref int outChannels, ref int outBitsPerSample, ref int outFramesPerPacket);
        [DllImport("LibALAC64.dll", EntryPoint = "ParseMagicCookie", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ParseMagicCookie64(byte[] inMagicCookie, int inMagicCookieSize, ref int outSampleRate, ref int outChannels, ref int outBitsPerSample, ref int outFramesPerPacket);

        private static bool Is64BitProcess => IntPtr.Size == 8;

        /// <summary>
        ///     Get audio format description out of an Apple lossless "magic cookie".
        /// </summary>
        /// <param name="inMagicCookie">The magic cookie data.</param>
        /// <param name="outSampleRate">Number of samples of audio carried per second (e.g. 44100, 48000).</param>
        /// <param name="outChannels">Number of audio channels (1 = mono, 2 = stereo, etc.).</param>
        /// <param name="outBitsPerSample">Bit depth of audio samples (16, 20, 24 or 32 bits).</param>
        /// <param name="outFramesPerPacket">Default number of audio frames per packet.</param>
        public static int ParseMagicCookie(byte[] inMagicCookie, ref int outSampleRate, ref int outChannels, ref int outBitsPerSample, ref int outFramesPerPacket)
        {
            return Is64BitProcess ? ParseMagicCookie64(inMagicCookie, inMagicCookie.Length, ref outSampleRate, ref outChannels, ref outBitsPerSample, ref outFramesPerPacket) : ParseMagicCookie32(inMagicCookie, inMagicCookie.Length, ref outSampleRate, ref outChannels, ref outBitsPerSample, ref outFramesPerPacket);
        }

        private IntPtr intPtr;
        private bool disposed = false;
        private int decoderBytesPerPacket;

        /// <summary>
        ///     Initializes the Apple Lossless audio decoder component with the current config. 
        /// </summary>
        /// <param name="sampleRate">Number of samples of audio carried per second (e.g. 44100, 48000).</param>
        /// <param name="channels">Number of audio channels (1 = mono, 2 = stereo, etc.).</param>
        /// <param name="bitsPerSample">Bit depth of audio samples (16, 20, 24 or 32 bits).</param>
        /// <param name="framesPerPacket">Default number of audio frames per packet (ALAC default: 4096, AirPlay default: 352).</param>
        public Decoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket)
        {
            intPtr = Is64BitProcess ? InitializeDecoder64(sampleRate, channels, bitsPerSample, framesPerPacket) : InitializeDecoder32(sampleRate, channels, bitsPerSample, framesPerPacket);
            if (intPtr == null)
                throw new LibALACException("InitializeDecoder failed.");
            decoderBytesPerPacket = (bitsPerSample != 20 ? channels * (bitsPerSample / 8) : (int)(bitsPerSample * 2.5 + .5)) * framesPerPacket;
        }

        /// <summary>
        ///     Converts an Apple Lossless audio packet into raw PCM output data.
        /// </summary>
        /// <param name="data">The data source.</param>
        /// <param name="count">Length of input data in bytes.</param>
        public byte[] Decode(byte[] data, int count)
        {
            byte[] buffer = new byte[decoderBytesPerPacket];
            int result = Is64BitProcess ? Decode64(intPtr, data, buffer, ref count) : Decode32(intPtr, data, buffer, ref count);
            if (result != 0)
                throw new LibALACException("Decode failed.");
            if (count < decoderBytesPerPacket)
                Array.Resize(ref buffer, count);
            return buffer;
        }

        /// <summary>
        ///     Converts an Apple Lossless audio packet into raw PCM output data.
        /// </summary>
        /// <param name="data">The data source.</param>
        public byte[] Decode(byte[] data)
        {
            return Decode(data, data.Length);
        }

        /// <summary>
        ///     Converts an Apple Lossless audio packet into raw PCM output data.
        /// </summary>
        /// <param name="data">The data source.</param>
        /// <param name="offset">Input offset in bytes.</param>
        /// <param name="count">Length of input data in bytes.</param>
        public byte[] Decode(byte[] data, int offset, int count)
        {
            byte[] buffer = new byte[count];
            Buffer.BlockCopy(data, offset, buffer, 0, count);
            return Decode(buffer, count);
        }

        /// <summary>
        ///     Public implementation of Dispose pattern to close the unmanaged decoder.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Protected implementation of Dispose pattern. Close the decoder.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;
            int result = Is64BitProcess ? FinishDecoder64(intPtr) : FinishDecoder32(intPtr);
        }
    }
}
