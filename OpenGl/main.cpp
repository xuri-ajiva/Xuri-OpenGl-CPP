#define GLEW_STATIC
#define SDL_MAIN_HANDLED

#include <ctime>
#include <iomanip>
#include <iostream>
#include <GL/glew.h>
#include <SDL2/SDL.h>
#include <chrono>
#include <thread>

#ifdef _WIN32
    #include <windows.h>
#else
    #include <unistd.h>
#endif // _WIN32

#ifdef _WIN32
#pragma comment(lib, "SDL2.lib")
#pragma comment(lib, "glew32s.lib")
#pragma comment(lib, "opengl32.lib")
#endif

#include "defines.h"
#include "VertexBuffer.h"
#include "Shader.h"

Vertex vertices[] = {
	Vertex {-0.9f, -0.9f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f},
	Vertex {0.0f, 0.9f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f},
	Vertex {0.9f, -0.9f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f}
};
Uint32 numVertices = 3;

std::string ShaderPos      = "Shaders/";
const char* vertexShader   = "basic-vertex-shader.glsl.vert";
const char* fragmentShader = "basic-fragment-shader.glsl.frag";


int main(int argc, char** argv){
	std::cout << "Starting App..." << std::endl;

	SDL_Window* window;
	SDL_Init (SDL_INIT_EVERYTHING);

	SDL_GL_SetAttribute (SDL_GL_RED_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_GREEN_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_BLUE_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_ALPHA_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_BUFFER_SIZE, 32);

	SDL_GL_SetAttribute (SDL_GL_DOUBLEBUFFER, 1);

	Uint32 flags = SDL_WINDOW_OPENGL;// | SDL_WINDOW_FULLSCREEN_DESKTOP;

	window = SDL_CreateWindow ("Xuri´s OpenGL C++",
	                           SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
	                           1000, 800,
	                           flags);

	SDL_GLContext glContext = SDL_GL_CreateContext (window); //SDL_SetWindowResizable(window, SDL_TRUE);


	SDL_GL_SetSwapInterval (1);

	GLenum err = glewInit();
	if (err != GLEW_OK) {
		std::cout << glewGetErrorString (err) << std::endl;
		std::cin.get();
		return -1;
	}

	std::cout << "OpenGl version: " << glGetString (GL_VERSION) << std::endl;

	std::cout << "Shaders: " << ShaderPos << std::endl
		<< "	[" << vertexShader << ", " << fragmentShader << "]" << std::endl;

	const Shader shader ((ShaderPos + vertexShader).c_str(), (ShaderPos + fragmentShader).c_str());
	shader.bind();

	std::cout << "shader initialized PointerID: " << shader.GetShaderID() << std::endl;

	VertexBuffer vertex_buffer (vertices, numVertices);
	vertex_buffer.UNBIND();
	std::cout << "vertexBuffer initialized PointerID: " << vertex_buffer.GET_BUFFER_ID() << std::endl;

	Uint64  perfCounterFrequency = SDL_GetPerformanceFrequency();
	Uint64  lastCounter          = SDL_GetPerformanceCounter();
	float32 delta;

	//WireFrame
	//glPolygonMode (GL_FRONT_AND_BACK,GL_LINE);

	bool close = false;
	while (!close) {
		glClearColor (0.1f, 0.1f, 0.1f, 1.0f);
		glClear (GL_COLOR_BUFFER_BIT);

		vertex_buffer.BIND();
		glDrawArrays (GL_TRIANGLES, 0, numVertices);
		vertex_buffer.UNBIND();

		SDL_GL_SwapWindow (window);

		SDL_Event event;
		while (SDL_PollEvent (&event)) {
			if (event.type == SDL_QUIT) {
				return 0;
				close = true;
			}
		}

		Uint64 endCounter     = SDL_GetPerformanceCounter();
		UINT64 counterElapsed = endCounter - lastCounter;
		delta                 = float32 (counterElapsed) / float32 (perfCounterFrequency);
		Uint32 FPS            = Uint32 (float32 (perfCounterFrequency) / float32 (counterElapsed));

		SDL_SetWindowTitle (window, ("FPS: " + std::to_string (FPS)).c_str());
		lastCounter = endCounter;
	}

	std::cin.get();
	return 0;
}
