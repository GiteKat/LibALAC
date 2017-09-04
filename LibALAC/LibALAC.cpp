// LibALAC.cpp : Defines the exported functions for the DLL.
#include <utility>
#include <limits.h> 
#include "LibALAC.h"
#include "ALACEncoder.h"
#include "ALACDecoder.h"
#include "ALACBitUtilities.h"

struct EncoderInfo
{
	ALACEncoder * encoder;
	AudioFormatDescription inputFormat;
	AudioFormatDescription outputFormat;
};

struct DecoderInfo
{
	ALACDecoder * decoder;
	int32_t channels;
	int32_t bytesPerFrame;
	int32_t framesPerPacket;
};

void* InitializeEncoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode)
{
	if (channels < 1 || channels > 8)
		return NULL;
	uint32_t flags;
	switch (bitsPerSample)
	{
	case 16:
		flags = 1;
		break;
	case 20:
		flags = 2;
		break;
	case 24:
		flags = 3;
		break;
	case 32:
		flags = 4;
		break;
	default:
		return NULL;
		break;
	}

	EncoderInfo * encoder = (EncoderInfo *)calloc(sizeof(EncoderInfo), 1);

	encoder->inputFormat.mSampleRate = sampleRate;
	encoder->outputFormat.mSampleRate = sampleRate;

	encoder->inputFormat.mFormatID = kALACFormatLinearPCM;
	encoder->outputFormat.mFormatID = kALACFormatAppleLossless;

	encoder->inputFormat.mFormatFlags = kALACFormatFlagsNativeEndian;
	encoder->outputFormat.mFormatFlags = flags;
	
	encoder->inputFormat.mBytesPerPacket = bitsPerSample != 20 ? channels * (bitsPerSample >> 3) : (int32_t)(bitsPerSample * 2.5 + .5);
	encoder->outputFormat.mBytesPerPacket = 0;

	encoder->inputFormat.mFramesPerPacket = 1;
	encoder->outputFormat.mFramesPerPacket = framesPerPacket;

	encoder->inputFormat.mBytesPerFrame = encoder->inputFormat.mBytesPerPacket;
	encoder->outputFormat.mBytesPerFrame = 0;

	encoder->inputFormat.mChannelsPerFrame = channels;
	encoder->outputFormat.mChannelsPerFrame = channels;

	encoder->inputFormat.mBitsPerChannel = bitsPerSample;
	encoder->outputFormat.mBitsPerChannel = 0;

	encoder->inputFormat.mReserved = 0;
	encoder->outputFormat.mReserved = 0;

	encoder->encoder = new ALACEncoder;
	encoder->encoder->SetFastMode(useFastMode);
	encoder->encoder->SetFrameSize(encoder->outputFormat.mFramesPerPacket);
	encoder->encoder->InitializeEncoder(encoder->outputFormat);

	return encoder;
}

int GetMagicCookieSize(void* encoder)
{
	if (encoder == NULL)
		return -1;
	return ((EncoderInfo *)encoder)->encoder->GetMagicCookieSize(((EncoderInfo *)encoder)->outputFormat.mChannelsPerFrame);
}

int GetMagicCookie(void* encoder, unsigned char * outCookie)
{
	if (encoder == NULL)
		return -1;
	uint32_t ioNumBytes;
	((EncoderInfo *)encoder)->encoder->GetMagicCookie(outCookie, &ioNumBytes);
	return ioNumBytes;
}

int Encode(void* encoder, unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes)
{
	if (encoder == NULL)
		return -1;
	return ((EncoderInfo *)encoder)->encoder->Encode(((EncoderInfo *)encoder)->inputFormat, ((EncoderInfo *)encoder)->outputFormat, inBuffer, outBuffer, ioNumBytes);
}

int FinishEncoder(void* encoder)
{
	if (encoder == NULL)
		return -1;
	int32_t result = ((EncoderInfo *)encoder)->encoder->Finish();
	delete ((EncoderInfo *)encoder)->encoder;
	free(encoder);
	return result;
}

void* InitializeDecoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket)
{
	if (channels < 1 || channels > 8)
		return NULL;
	uint32_t flags;
	switch (bitsPerSample)
	{
	case 16:
		flags = 1;
		break;
	case 20:
		flags = 2;
		break;
	case 24:
		flags = 3;
		break;
	case 32:
		flags = 4;
		break;
	default:
		return NULL;
		break;
	}

	DecoderInfo * decoder = (DecoderInfo *)calloc(sizeof(DecoderInfo), 1);

	decoder->channels = channels;
	decoder->bytesPerFrame = bitsPerSample != 20 ? channels * (bitsPerSample >> 3) : (int32_t)(bitsPerSample * 2.5 + .5);
	decoder->framesPerPacket = framesPerPacket;

	AudioFormatDescription format;
	format.mSampleRate = sampleRate;
	format.mFormatID = kALACFormatAppleLossless;
	format.mFormatFlags = flags;
	format.mBytesPerPacket = 0;
	format.mFramesPerPacket = framesPerPacket;
	format.mBytesPerFrame = 0;
	format.mChannelsPerFrame = channels;
	format.mBitsPerChannel = 0;
	format.mReserved = 0;

	ALACEncoder * _encoder = new ALACEncoder;
	_encoder->SetFrameSize(framesPerPacket);
	_encoder->InitializeEncoder(format);
	uint32_t theMagicCookieSize = _encoder->GetMagicCookieSize(channels);
	uint8_t * theMagicCookie = (uint8_t *)calloc(theMagicCookieSize, 1);
	_encoder->GetMagicCookie(theMagicCookie, &theMagicCookieSize);
	delete _encoder;

	decoder->decoder = new ALACDecoder;
	int32_t result = decoder->decoder->Init(theMagicCookie, theMagicCookieSize);
	free(theMagicCookie);
	return decoder;
}

int Decode(void* decoder, unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes)
{
	if (decoder == NULL)
		return -1;
	BitBuffer bitBuffer;
	BitBufferInit(&bitBuffer, inBuffer, *ioNumBytes);
	uint32_t numFrames;
	int32_t result = ((DecoderInfo *)decoder)->decoder->Decode(&bitBuffer, outBuffer, ((DecoderInfo *)decoder)->framesPerPacket, ((DecoderInfo *)decoder)->channels, &numFrames);
	*ioNumBytes = numFrames * ((DecoderInfo *)decoder)->bytesPerFrame;
	return result;
}

int FinishDecoder(void* decoder)
{
	if (decoder == NULL)
		return -1;
	delete ((DecoderInfo *)decoder)->decoder;
	free(decoder);
	return 0;
}