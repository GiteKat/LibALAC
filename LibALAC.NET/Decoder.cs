using System;
using System.Runtime.InteropServices;

namespace LibALAC
{
    /// <summary>
    ///     .NET standard wrapper for the native dynamic-link library (DLL) implementation of the Apple Lossless Audio Codec (ALAC) decoder.
    /// </summary>
    public static class Decoder
    {
        [DllImport("LibALAC32.dll", EntryPoint = "InitializeDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int InitializeDecoder32(int sampleRate, int channels, int bitsPerSample, int framesPerPacket);
        [DllImport("LibALAC64.dll", EntryPoint = "InitializeDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int InitializeDecoder64(int sampleRate, int channels, int bitsPerSample, int framesPerPacket);

        [DllImport("LibALAC32.dll", EntryPoint = "Decode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Decode32(byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);
        [DllImport("LibALAC64.dll", EntryPoint = "Decode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Decode64(byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);

        [DllImport("LibALAC32.dll", EntryPoint = "FinishDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishDecoder32();
        [DllImport("LibALAC64.dll", EntryPoint = "FinishDecoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishDecoder64();

        private static int DecoderBytesPerPacket;
        private static bool Is64BitProcess => IntPtr.Size == 8;

        /// <summary>
        ///     Initializes the Apple Lossless audio decoder component with the current config. 
        ///     This function must be called before any other decoder method.
        /// </summary>
        /// <param name="sampleRate">Number of samples of audio carried per second (e.g. 44100, 48000).</param>
        /// <param name="channels">Number of audio channels (1 = mono, 2 = stereo, etc.).</param>
        /// <param name="bitsPerSample">Bit depth of audio samples (16, 20, 24 or 32 bits).</param>
        /// <param name="framesPerPacket">Default number of audio frames per packet (ALAC default: 4096, AirPlay default: 352).</param>
        public static void Initialize(int sampleRate, int channels, int bitsPerSample, int framesPerPacket)
        {
            int result = Is64BitProcess ? InitializeDecoder64(sampleRate, channels, bitsPerSample, framesPerPacket) : InitializeDecoder32(sampleRate, channels, bitsPerSample, framesPerPacket);
            if (result != 0)
                throw new LibALACException("InitializeDecoder failed.");
            DecoderBytesPerPacket = (bitsPerSample != 20 ? channels * (bitsPerSample / 8) : (int)(bitsPerSample * 2.5 + .5)) * framesPerPacket;
        }

        /// <summary>
        ///     Converts an Apple Lossless audio packet into raw PCM output data.
        /// </summary>
        /// <param name="data">The data source.</param>
        /// <param name="len">Length of input data in bytes.</param>
        public static byte[] Decode(byte[] data, int len)
        {
            byte[] buffer = new byte[DecoderBytesPerPacket];
            int result = Is64BitProcess ? Decode64(data, buffer, ref len) : Decode32(data, buffer, ref len);
            if (result != 0)
                throw new LibALACException("Decode failed.");
            if (len < DecoderBytesPerPacket)
                Array.Resize(ref buffer, len);
            return buffer;
        }

        /// <summary>
        ///     Converts an Apple Lossless audio packet into raw PCM output data.
        /// </summary>
        /// <param name="data">The data source.</param>
        public static byte[] Decode(byte[] data)
        {
            return Decode(data, data.Length);
        }

        /// <summary>
        ///     Close the decoder.
        /// </summary>
        public static void Finish()
        {
            int result = Is64BitProcess ? FinishDecoder64() : FinishDecoder32();
            if (result != 0)
                throw new LibALACException("FinishDecoder failed.");
        }

    }
}
