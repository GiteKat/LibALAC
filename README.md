# LibALAC

Microsoft Windows dynamic-link library (DLL) implementation of the Apple Lossless Audio Codec (ALAC).
Mirror copy of the ALAC encoder and decoder from http://alac.macosforge.org/ with changes for compiling as DLL in Visual Studio 2017.

# LibALAC .NET

.NET standard wrapper for the native dynamic-link library (DLL) implementation of the Apple Lossless Audio Codec (ALAC).

### Installation ###

Use Git or SVN to download the source code using the web URL:

    https://github.com/GiteKat/LibALAC.git

Install the [LibALAC NuGet package](https://www.nuget.org/packages/LibALAC/):

    Install-Package LibALAC

### C# code sample ###

The following example show resampling, ALAC-encoding and decoding of a .mp3 input file using the excellent [CSCore .NET Audio Library](https://github.com/filoe/cscore) and LibALAC:

```c#
using CSCore;
using CSCore.Codecs;
using CSCore.DSP;
using System;
using System.Linq;

namespace Demo
{
    class Program
    {
        const string FileName = @"C:\Music\demo.mp3";
        const int SampleRate = 44100;
        const int Channels = 2;
        const int BitsPerSample = 16;
        const int FramesPerPacket = 4096;

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            LibALAC.Encoder encoder = new LibALAC.Encoder(SampleRate, Channels, BitsPerSample, FramesPerPacket);
            LibALAC.Decoder decoder = new LibALAC.Decoder(SampleRate, Channels, BitsPerSample, FramesPerPacket);
            using (IWaveSource waveSource = CodecFactory.Instance.GetCodec(FileName))
            {
                WaveFormat waveFormat = new WaveFormatExtensible(SampleRate, BitsPerSample, Channels, AudioSubTypes.Pcm);
                using (DmoResampler resampler = new DmoResampler(waveSource, waveFormat))
                {
                    int read;
                    byte[] buffer = new byte[FramesPerPacket * Channels * (BitsPerSample / 8)];
                    while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] encoded = encoder.Encode(buffer, read);
                        byte[] decoded = decoder.Decode(encoded);
                        if (read < buffer.Length)
                            Array.Resize(ref buffer, read);
                        if (!decoded.SequenceEqual(buffer))
                            throw new Exception("Encoding/Decoding error!");
                    }
                }
            }
            Console.WriteLine("Encoding/Decoding-Time: " + DateTime.Now.Subtract(startTime));
            Console.ReadLine();
        }
    }
}
```

### License ###
	
All sources are available under the Apache license 2.0.
