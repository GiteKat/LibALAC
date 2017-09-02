// LibALAC.cpp : Defines the exported functions for the DLL.
#include <utility>
#include <limits.h> 
#include "LibALAC.h"
#include "ALACEncoder.h"
#include "ALACDecoder.h"
#include "ALACBitUtilities.h"

// DLL internal state variables:
static ALACEncoder * encoder = NULL;
static AudioFormatDescription encInputFormat;
static AudioFormatDescription encOutputFormat;

static ALACDecoder * decoder = NULL; 
static int32_t decChannels;
static int32_t decBytesPerFrame;
static int32_t decFramesPerPacket;

int InitializeEncoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode)
{
	if (encoder != NULL)
		return -1;
	if (channels < 1 || channels > 8)
		return -1;

	encInputFormat.mSampleRate = sampleRate;
	encOutputFormat.mSampleRate = sampleRate;

	encInputFormat.mFormatID = kALACFormatLinearPCM;
	encOutputFormat.mFormatID = kALACFormatAppleLossless;

	encInputFormat.mFormatFlags = kALACFormatFlagsNativeEndian;
	switch (bitsPerSample)
	{
		case 16:
			encOutputFormat.mFormatFlags = 1;
			break;
		case 20:
			encOutputFormat.mFormatFlags = 2;
			break;
		case 24:
			encOutputFormat.mFormatFlags = 3;
			break;
		case 32:
			encOutputFormat.mFormatFlags = 4;
			break;
		default:
			return -1;
			break;
	}
	
	encInputFormat.mBytesPerPacket = bitsPerSample != 20 ? channels * (bitsPerSample >> 3) : (int32_t)(bitsPerSample * 2.5 + .5);
	encOutputFormat.mBytesPerPacket = 0;

	encInputFormat.mFramesPerPacket = 1;
	encOutputFormat.mFramesPerPacket = framesPerPacket;

	encInputFormat.mBytesPerFrame = encInputFormat.mBytesPerPacket;
	encOutputFormat.mBytesPerFrame = 0;

	encInputFormat.mChannelsPerFrame = channels;
	encOutputFormat.mChannelsPerFrame = channels;

	encInputFormat.mBitsPerChannel = bitsPerSample;
	encOutputFormat.mBitsPerChannel = 0;

	encInputFormat.mReserved = 0;
	encOutputFormat.mReserved = 0;

	encoder = new ALACEncoder;
	encoder->SetFastMode(useFastMode);
	encoder->SetFrameSize(encOutputFormat.mFramesPerPacket);
	return encoder->InitializeEncoder(encOutputFormat);
}

int GetMagicCookieSize()
{
	if (encoder == NULL)
		return -1;
	return encoder->GetMagicCookieSize(encOutputFormat.mChannelsPerFrame);
}

int GetMagicCookie(unsigned char * outCookie)
{
	if (encoder == NULL)
		return -1;
	uint32_t ioNumBytes;
	encoder->GetMagicCookie(outCookie, &ioNumBytes);
	return ioNumBytes;
}

int Encode(unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes)
{
	if (encoder == NULL)
		return -1;
	return encoder->Encode(encInputFormat, encOutputFormat, inBuffer, outBuffer, ioNumBytes);
}

int FinishEncoder()
{
	if (encoder == NULL)
		return -1;
	int32_t result = encoder->Finish();
	delete encoder;
	encoder = NULL;
	return result;
}

int InitializeDecoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket)
{
	if (decoder != NULL)
		return -1;
	if (channels < 1 || channels > 8)
		return -1;

	decChannels = channels;
	decBytesPerFrame = bitsPerSample != 20 ? channels * (bitsPerSample >> 3) : (int32_t)(bitsPerSample * 2.5 + .5);
	decFramesPerPacket = framesPerPacket;

	AudioFormatDescription format;
	format.mSampleRate = sampleRate;
	format.mFormatID = kALACFormatAppleLossless;
	switch (bitsPerSample)
	{
		case 16:
			format.mFormatFlags = 1;
			break;
		case 20:
			format.mFormatFlags = 2;
			break;
		case 24:
			format.mFormatFlags = 3;
			break;
		case 32:
			format.mFormatFlags = 4;
			break;
		default:
			return -1;
			break;
	}
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

	decoder = new ALACDecoder;
	int32_t result = decoder->Init(theMagicCookie, theMagicCookieSize);
	free(theMagicCookie);
	return result;
}

int Decode(unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes)
{
	if (decoder == NULL)
		return -1;
	BitBuffer bitBuffer;
	BitBufferInit(&bitBuffer, inBuffer, *ioNumBytes);
	uint32_t numFrames;
	int32_t result = decoder->Decode(&bitBuffer, outBuffer, decFramesPerPacket, decChannels, &numFrames);
	*ioNumBytes = numFrames * decBytesPerFrame;
	return result;
}

int FinishDecoder()
{
	if (decoder == NULL)
		return -1;
	delete decoder;
	decoder = NULL;
	return 0;
}