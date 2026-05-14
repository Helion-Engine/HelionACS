#pragma once

#if defined(_WIN32)
	#if defined(HELIONACS_BUILD)
		#define HELIONACS_API extern "C" __declspec(dllexport)
	#else
		#define HELIONACS_API extern "C" __declspec(dllimport)
	#endif
#else
	#define HELIONACS_API extern "C"
#endif
