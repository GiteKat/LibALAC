using System;
using System.Runtime.InteropServices;

namespace LibALAC
{
    /// <summary>
    ///     .NET standard wrapper for the native dynamic-link library (DLL) implementation of the Apple Lossless Audio Codec (ALAC) encoder.
    /// </summary>
    public static class Encoder
    {
        [DllImport("LibALAC32.dll", EntryPoint = "InitializeEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int InitializeEncoder32(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode);
        [DllImport("LibALAC64.dll", EntryPoint = "InitializeEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int InitializeEncoder64(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode);

        [DllImport("LibALAC32.dll", EntryPoint = "GetMagicCookieSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookieSize32();
        [DllImport("LibALAC64.dll", EntryPoint = "GetMagicCookieSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookieSize64();

        [DllImport("LibALAC32.dll", EntryPoint = "GetMagicCookie", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookie32(byte[] outCookie);
        [DllImport("LibALAC64.dll", EntryPoint = "GetMagicCookie", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookie64(byte[] outCookie);

        [DllImport("LibALAC32.dll", EntryPoint = "Encode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Encode32(byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);
        [DllImport("LibALAC64.dll", EntryPoint = "Encode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Encode64(byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);

        [DllImport("LibALAC32.dll", EntryPoint = "FinishEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishEncoder32();
        [DllImport("LibALAC64.dll", EntryPoint = "FinishEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishEncoder64();

        private static bool Is64BitProcess => IntPtr.Size == 8;

        /// <summary>
        ///     Initializes the Apple Lossless audio encoder component with the current config. 
        ///     This function must be called before any other encoder method.
        /// </summary>
        /// <param name="sampleRate">Number of samples of audio carried per second (e.g. 44100, 48000).</param>
        /// <param name="channels">Number of audio channels (1 = mono, 2 = stereo, etc.).</param>
        /// <param name="bitsPerSample">Bit depth of audio samples (16, 20, 24 or 32 bits).</param>
        /// <param name="framesPerPacket">Default number of audio frames per packet (ALAC default: 4096, AirPlay default: 352).</param>
        /// <param name="useFastMode">Encode with maximum possible speed but less compression</param>
        public static void Initialize(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode)
        {
            int result = Is64BitProcess ? InitializeEncoder64(sampleRate, channels, bitsPerSample, framesPerPacket, useFastMode) : InitializeEncoder32(sampleRate, channels, bitsPerSample, framesPerPacket, useFastMode);
            if (result != 0)
                throw new LibALACException("InitializeEncoder failed.");
        }

        /// <summary>
        ///     Get the size of Apple lossless format "magic cookie" description.
        /// </summary>
        public static int GetMagicCookieSize()
        {
            int result = Is64BitProcess ? GetMagicCookieSize64() : GetMagicCookieSize32();
            if (result > 0)
                return result;
            throw new LibALACException("GetMagicCookieSize failed.");
        }

        /// <summary>
        ///     Get the Apple lossless codec specific information frame, often called the "magic cookie".
        /// </summary>
        public static byte[] GetMagicCookie()
        {
            int size = GetMagicCookieSize();
            byte[] outCookie = new byte[size];
            if ((Is64BitProcess ? GetMagicCookie64(outCookie) : GetMagicCookie32(outCookie)) == 0)
                return outCookie;
            throw new LibALACException("GetMagicCookie failed.");
        }

        /// <summary>
        ///     Converts raw PCM input data into an Apple Lossless audio packet.
        /// </summary>
        /// <param name="data">The data source.</param>
        /// <param name="len">Length of input data in bytes.</param>
        public static byte[] Encode(byte[] data, int len)
        {
            byte[] buffer = new byte[len + 7];
            int result = Is64BitProcess ? Encode64(data, buffer, ref len) : Encode32(data, buffer, ref len);
            if (result != 0)
               throw new LibALACException("Encode failed.");
            Array.Resize(ref buffer, len);
            return buffer;
        }

        /// <summary>
        ///     Converts raw PCM input data into an Apple Lossless audio packet.
        /// </summary>
        /// <param name="data">The data source.</param>
        public static byte[] Encode(byte[] data)
        {
            return Encode(data, data.Length);
        }

        /// <summary>
        ///     Drain out any leftover samples and close encoder.
        /// </summary>
        public static void Finish()
        {
            int result = Is64BitProcess ? FinishEncoder64() : FinishEncoder32();
            if (result != 0)
                throw new LibALACException("FinishEncoder failed.");
        }
        
    }
}
