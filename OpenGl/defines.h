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
	glm::vec3 position;
	glm::vec3 normal;
	glm::vec2 textureCord;
};