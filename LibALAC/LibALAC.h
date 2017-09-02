// LibALAC.h - Contains declarations of LibALAC functions
#pragma once

#ifdef LIBALAC_EXPORTS  
#define LIBALAC_API __declspec(dllexport)   
#else  
#define LIBALAC_API __declspec(dllimport)   
#endif  

// Encoder-Constructor 
// This function must be called before any other encoder function
extern "C" LIBALAC_API int InitializeEncoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket, bool useFastMode);

// Get size of magic cookie
extern "C" LIBALAC_API int GetMagicCookieSize();

// Get the magic cookie
extern "C" LIBALAC_API int GetMagicCookie(unsigned char * outCookie);

// Encode the next block of samples
extern "C" LIBALAC_API int Encode(unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes);

// Drain out any leftover samples
extern "C" LIBALAC_API int FinishEncoder();

// Decoder-Constructor 
// This function must be called before any other decoder function
extern "C" LIBALAC_API int InitializeDecoder(int sampleRate, int channels, int bitsPerSample, int framesPerPacket);

// Decode the next block of samples
extern "C" LIBALAC_API int Decode(unsigned char * inBuffer, unsigned char * outBuffer, int * ioNumBytes);

// Free the memory
extern "C" LIBALAC_API int FinishDecoder();
