using System;
using System.Runtime.InteropServices;

namespace LibALAC
{
    /// <summary>
    ///     .NET standard wrapper for the native dynamic-link library (DLL) implementation of the Apple Lossless Audio Codec (ALAC) encoder.
    /// </summary>
    public class Encoder : IDisposable
    {
        [DllImport("LibALAC32.dll", EntryPoint = "InitializeEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr InitializeEncoder32(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode);
        [DllImport("LibALAC64.dll", EntryPoint = "InitializeEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr InitializeEncoder64(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode);

        [DllImport("LibALAC32.dll", EntryPoint = "GetMagicCookieSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookieSize32(IntPtr encoder);
        [DllImport("LibALAC64.dll", EntryPoint = "GetMagicCookieSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookieSize64(IntPtr encoder);

        [DllImport("LibALAC32.dll", EntryPoint = "GetMagicCookie", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookie32(IntPtr encoder, byte[] outCookie);
        [DllImport("LibALAC64.dll", EntryPoint = "GetMagicCookie", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetMagicCookie64(IntPtr encoder, byte[] outCookie);

        [DllImport("LibALAC32.dll", EntryPoint = "Encode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Encode32(IntPtr encoder, byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);
        [DllImport("LibALAC64.dll", EntryPoint = "Encode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Encode64(IntPtr encoder, byte[] readBuffer, byte[] writeBuffer, ref int ioNumBytes);

        [DllImport("LibALAC32.dll", EntryPoint = "FinishEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishEncoder32(IntPtr encoder);
        [DllImport("LibALAC64.dll", EntryPoint = "FinishEncoder", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FinishEncoder64(IntPtr encoder);

        private static bool Is64BitProcess => IntPtr.Size == 8;

        /// <summary>
        ///     Number of samples of audio carried per second (e.g. 44100, 48000).
        /// </summary>
        public int SampleRate => sampleRate;

        /// <summary>
        ///     Number of audio channels (1 = mono, 2 = stereo, etc.).
        /// </summary>
        public int Channels => channels;

        /// <summary>
        ///     Bit depth of audio samples (16, 20, 24 or 32 bits).
        /// </summary>
        public int BitsPerSample => bitsPerSample;

        /// <summary>
        ///     Default number of audio frames per packet.
        /// </summary>
        public int FramesPerPacket => framesPerPacket;

        /// <summary>
        ///    Default size of one PCM input packet.
        /// </summary>
        public int BytesPerPacket => bytesPerPacket;

        private int sampleRate;
        private int channels;
        private int bitsPerSample;
        private int framesPerPacket;
        private int bytesPerPacket;

        private IntPtr intPtr;
        private bool disposed = false;

        /// <summary>
        ///     Initializes the Apple Lossless audio encoder component with the current config. 
        /// </summary>
        /// <param name="sampleRate">Number of samples of audio carried per second (e.g. 44100, 48000).</param>
        /// <param name="channels">Number of audio channels (1 = mono, 2 = stereo, etc.).</param>
        /// <param name="bitsPerSample">Bit depth of audio samples (16, 20, 24 or 32 bits).</param>
        /// <param name="framesPerPacket">Default number of audio frames per packet (ALAC default: 4096, AirPlay default: 352).</param>
        /// <param name="useFastMode">Encode with maximum possible speed but less compression</param>
        public Encoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode = false)
        {
            intPtr = Is64BitProcess ? InitializeEncoder64(sampleRate, channels, bitsPerSample, framesPerPacket, useFastMode) : InitializeEncoder32(sampleRate, channels, bitsPerSample, framesPerPacket, useFastMode);
            if (intPtr == null)
                throw new LibALACException("InitializeEncoder failed.");
            this.sampleRate = sampleRate;
            this.channels = channels;
            this.bitsPerSample = bitsPerSample;
            this.framesPerPacket = framesPerPacket;
            bytesPerPacket = (bitsPerSample != 20 ? channels * (bitsPerSample / 8) : (int)(channels * 2.5 + .5)) * framesPerPacket;
        }

        /// <summary>
        ///     Get the size of Apple lossless format "magic cookie" description.
        /// </summary>
        public int GetMagicCookieSize()
        {
            int result = Is64BitProcess ? GetMagicCookieSize64(intPtr) : GetMagicCookieSize32(intPtr);
            if (result > 0)
                return result;
            throw new LibALACException("GetMagicCookieSize failed.");
        }

        /// <summary>
        ///     Get the Apple lossless codec specific information frame, often called the "magic cookie".
        /// </summary>
        public byte[] GetMagicCookie()
        {
            int size = GetMagicCookieSize();
            byte[] outCookie = new byte[size];
            int len = Is64BitProcess ? GetMagicCookie64(intPtr, outCookie) : GetMagicCookie32(intPtr, outCookie);
            if (len != size)
                throw new LibALACException("GetMagicCookie failed.");
            return outCookie;
        }

        /// <summary>
        ///     Converts raw PCM input data into an Apple Lossless audio packet.
        /// </summary>
        /// <param name="data">The data source.</param>
        /// <param name="count">Length of input data in bytes.</param>
        public byte[] Encode(byte[] data, int count)
        {
            byte[] buffer = new byte[count + 7];
            int result = Is64BitProcess ? Encode64(intPtr, data, buffer, ref count) : Encode32(intPtr, data, buffer, ref count);
            if (result != 0)
               throw new LibALACException("Encode failed.");
            Array.Resize(ref buffer, count);
            return buffer;
        }

        /// <summary>
        ///     Converts raw PCM input data into an Apple Lossless audio packet.
        /// </summary>
        /// <param name="data">The data source.</param>
        public byte[] Encode(byte[] data)
        {
            return Encode(data, data.Length);
        }

        /// <summary>
        ///     Converts raw PCM input data into an Apple Lossless audio packet.
        /// </summary>
        /// <param name="data">The data source.</param>
        /// <param name="offset">Input offset in bytes.</param>
        /// <param name="count">Length of input data in bytes.</param>
        public byte[] Encode(byte[] data, int offset, int count)
        {
            byte[] buffer = new byte[count];
            Buffer.BlockCopy(data, offset, buffer, 0, count);
            return Encode(buffer, count);
        }

        /// <summary>
        ///     Public implementation of Dispose pattern to drain out any leftover samples and close the unmanaged decoder.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Protected implementation of Dispose pattern. Drain out any leftover samples and close encoder.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;
            int result = Is64BitProcess ? FinishEncoder64(intPtr) : FinishEncoder32(intPtr);
        }
    }
}
