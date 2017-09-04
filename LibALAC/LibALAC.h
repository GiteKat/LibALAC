// LibALAC.h - Contains declarations of LibALAC functions
#pragma once

#ifdef LIBALAC_EXPORTS  
#define LIBALAC_API __declspec(dllexport)   
#else  
#define LIBALAC_API __declspec(dllimport)   
#endif  

// Encoder-Constructor 
// This function must be called before any other encoder function
extern "C" LIBALAC_API void* InitializeEncoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode);

// Get size of magic cookie
extern "C" LIBALAC_API int GetMagicCookieSize(void* encoder);

// Get the magic cookie
extern "C" LIBALAC_API int GetMagicCookie(void* encoder, unsigned char * outCookie);

// Encode the next block of samples
extern "C" LIBALAC_API int Encode(void* encoder, unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes);

// Drain out any leftover samples
extern "C" LIBALAC_API int FinishEncoder(void* encoder);

// Decoder-Constructor 
// This function must be called before any other decoder function
extern "C" LIBALAC_API void* InitializeDecoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket);

// Decode the next block of samples
extern "C" LIBALAC_API int Decode(void* decoder, unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes);

// Free the memory
extern "C" LIBALAC_API int FinishDecoder(void* decoder);
