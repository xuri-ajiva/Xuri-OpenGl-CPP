#define GLEW_STATIC
#define SDL_MAIN_HANDLED


#include <iostream>
#include <GL/glew.h>
#include <SDL2/SDL.h>

#ifdef _WIN32
#pragma comment(lib, "SDL2.lib")
#pragma comment(lib, "glew32s.lib")
#pragma comment(lib, "opengl32.lib")
#endif

#include "defines.h"
#include  "VertexBuffer.h"

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

	window = SDL_CreateWindow ("Xuri´s OpenGL C++", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, 800, 600,SDL_WINDOW_OPENGL);
	SDL_GLContext glContext = SDL_GL_CreateContext (window); //SDL_SetWindowResizable(window, SDL_TRUE);

	GLenum err = glewInit();
	if (err != GLEW_OK) {
		std::cout << glewGetErrorString (err) << std::endl;
		std::cin.get();
		return -1;
	}

	std::cout << "OpenGl version: " << glGetString (GL_VERSION) << std::endl;

	Vertex vertices[] = {
		Vertex {-0.5f, -0.5f, 0.0f},
		Vertex {0.0f, 0.5f, 0.0f},
		Vertex {0.5f, -0.5f, 0.0f}
	};
	Uint32 numVertices = 3;

	VertexBuffer vertex_buffer (vertices,numVertices);
	vertex_buffer.UNBIND();
	
	std::cout << "vertexBuffer initialized PointerID: " << vertex_buffer.GET_BUFFER_ID() << std::endl;

	bool close = false;
	while (!close) {
		glClearColor (0.0f, 0.0f, 0.0f, 1.0f);
		glClear (GL_COLOR_BUFFER_BIT);
		
		vertex_buffer.BIND();
		glDrawArrays (GL_TRIANGLES,0, numVertices);
		vertex_buffer.UNBIND();

		SDL_GL_SwapWindow (window);

		SDL_Event event;
		while (SDL_PollEvent (&event)) {
			if (event.type == SDL_QUIT) {
				return 0;
				close = true;
			}
		}
	}

	std::cin.get();
	return 0;
}
