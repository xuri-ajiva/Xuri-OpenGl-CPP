#pragma once
#define GLEW_STATIC
#define SDL_MAIN_HANDLED

#include <cstdint>
#include <ctime>
#include <iomanip>
#include <iostream>
#include <GL/glew.h>
#include <SDL2/SDL.h>
#include <chrono>
#include "libs/glm/glm.hpp"
#include "Vertex.h"
#include <windows.h>

typedef int8_t  Int8;
typedef int16_t Int16;
typedef int32_t Int32;
typedef int64_t Int64;
typedef uint8_t Uint8;

typedef uint16_t Uint16;
typedef uint32_t Uint32;
typedef uint64_t Uint64;

typedef float  float32;
typedef double float64;

namespace XUIR_CONSOLE_COLOR_DEF {
	//      Name               BG
	#define BG_Black           30
	#define BG_Red             31
	#define BG_Green           32
	#define BG_Yellow          33
	#define BG_Blue            34
	#define BG_Magenta         35
	#define BG_Cyan            36
	#define BG_White           37
	#define BG_Bright_Black    90
	#define BG_Bright_Red      91
	#define BG_Bright_Green    92
	#define BG_Bright_Yellow   93
	#define BG_Bright_Blue     94
	#define BG_Bright_Magenta  95
	#define BG_Bright_Cyan     96
	#define BG_Bright_White    97
	
	//      Name               FG
	#define FG_Black           40
	#define FG_Red             41
	#define FG_Green           42
	#define FG_Yellow          43
	#define FG_Blue            44
	#define FG_Magenta         45
	#define FG_Cyan            46
	#define FG_White           47
	#define FG_Bright_Black    100
	#define FG_Bright_Red      101
	#define FG_Bright_Green    102
	#define FG_Bright_Yellow   103
	#define FG_Bright_Blue     104
	#define FG_Bright_Magenta  105
	#define FG_Bright_Cyan     106
	#define FG_Bright_White    107

	//12
	#define MINUS_COLOR  4 
	//10
	#define PLUS_COLOR  2
	//14
	#define MESSAGE_COLOR 6


}

#ifdef NO_DEINES_XURI
void PrintStatus(char SIndex, int value, const char* message, int colorStatus, int colorMessage) {
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);

	SetConsoleTextAttribute(hConsole, colorStatus);
	std::cout << SIndex << "[" << value << "]: ";
	SetConsoleTextAttribute(hConsole, colorMessage);
	std::cout << message << std::endl;

	SetConsoleTextAttribute(hConsole, 15);
}
void PrintError(char SIndex, int value, const char* message, int colorStatus) {
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);

	SetConsoleTextAttribute(hConsole, colorStatus);
	std::cout << SIndex << "[ERROR: " << value << "]: ";
	SetConsoleTextAttribute(hConsole, 79);
	std::cout << message << std::endl;

	SetConsoleTextAttribute(hConsole, 15);
}

#undef NO_DEINES_XURI
#endif
