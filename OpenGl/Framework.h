#pragma once
#include "defines.h"
#include <SDL2/SDL.h>
#include "Framework.h"
#include <ostream>
#include <iostream>
#include "Shader.h"

class MainClass {
public:
	int Init();

	Uint64  perf_counter_frequency = SDL_GetPerformanceFrequency();
	Uint64  last_counter           = SDL_GetPerformanceCounter();
	float32 delta                  = 0.0F;
	float32 time                   = 0.0F;
	Uint64  frames                 = 0;
	//WireFrame

	bool MainLoop();

	SDL_Window* window;
};
#if _DEBUG
void GLAPIENTRY openGLDebugCallback(GLenum        source, GLenum type, GLuint id, GLenum severity, GLsizei length,
                                    const GLchar* message,
                                    const void*   userParam) {
	//if(serverity == GL_DEBUG_SEVERITY_HIGH)

	std::cout << "[Open Info: {severity: " << severity << ", source: " << source <<
		", type: " << type << ", id: " << id << "}]:" << std::endl << "	" << message << std::endl;
};
#endif

inline int MainClass::Init() {
	std::cout << "Starting App..." << std::endl;

	SDL_Init(SDL_INIT_EVERYTHING);

	SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_BUFFER_SIZE, 32);

	SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);

#if _DEBUG
	SDL_GL_SetAttribute(SDL_GL_CONTEXT_FLAGS, SDL_GL_CONTEXT_DEBUG_FLAG);
#endif

	Uint32 flags = SDL_WINDOW_OPENGL;// | SDL_WINDOW_RESIZABLE;

	window = SDL_CreateWindow("Xuri´s OpenGL C++",
	                          SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
	                          1000, 800,
	                          flags);

	SDL_GLContext glContext = SDL_GL_CreateContext(window); //SDL_SetWindowResizable(window, SDL_TRUE);
	SDL_GL_SetSwapInterval(1);

	GLenum err = glewInit();
	if (err != GLEW_OK) {
		std::cout << glewGetErrorString(err) << std::endl;
		std::cin.get();
		return -1;
	}

#if _DEBUG
	glEnable(GL_DEBUG_OUTPUT);
	glEnable(GL_DEBUG_OUTPUT_SYNCHRONOUS);
	glDebugMessageCallback(openGLDebugCallback, nullptr);
#endif

	SDL_SetRelativeMouseMode(SDL_TRUE);

	//glEnable(GL_CULL_FACE);
	return 0;
}

inline bool MainClass::MainLoop() {
	SDL_GL_SwapWindow(window);

	Uint64 endCounter     = SDL_GetPerformanceCounter();
	Uint64 counterElapsed = endCounter - last_counter;
	delta                 = float32(counterElapsed) / float32(perf_counter_frequency);
	Uint32 FPS            = Uint32(float32(perf_counter_frequency) / float32(counterElapsed));

	if (frames % 10 == 0) SDL_SetWindowTitle(window, ("FPS: " + std::to_string(FPS) + "  Time: "+std::to_string(time)).c_str() );
	last_counter = endCounter;
	frames++;
	time += delta;

	return true;
}
