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

struct Vertex {
	// Position
	float32 x;
	float32 y;
	float32 z;

	// Texture
	float32 u;
	float32 v;

	// Color
	float32 r;
	float32 g;
	float32 b;
	float32 a;
};