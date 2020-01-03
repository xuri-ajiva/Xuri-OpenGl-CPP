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
#include  "IndexBuffer.h"

Vertex vertices[] = {
	Vertex {-0.9f, -0.9f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f},
	Vertex {-0.9f, 0.9f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f},
	Vertex {0.9f, -0.9f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f},
	Vertex {0.9f, 0.9f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f},
};
Uint32 numVertices = (sizeof(vertices) / sizeof(*vertices));

Uint32 indices [] = {
	0, 1, 2,
	1, 2, 3
};
Uint32 numIndices = (sizeof(indices) / sizeof(*indices));

#if _DEBUG
void GLAPIENTRY openGLDebugCallback(GLenum        source, GLenum type, GLuint id, GLenum severity, GLsizei length,
                                    const GLchar* message,
                                    const void*   userParam) {
	//if(serverity == GL_DEBUG_SEVERITY_HIGH)

	std::cout << "[Open Info: {severity: " << severity << ", source: " << source <<
		", type: " << type << ", id: " << id << "}]:" << std::endl << "	" << message << std::endl;
};

void              _GLGetError(const char* file, int line, const char* call) {
	while (GLenum error = glGetError()) {
		std::cout << "[Open info: {file: " << file << ", line: " << line << ", call: " << call <<
			", error: " << error << "}]: " << glewGetErrorString (error);
	}
}

#define GLCALL(call) call; _GLGetError(__FILE__,__LINE__,#call)
#else
#define GLCALL(call) call
#endif


std::string ShaderPos      = "Shaders/";
const char* vertexShader   = "basic-vertex-shader.glsl.vert";
const char* fragmentShader = "basic-fragment-shader.glsl.frag";

int main(int argc, char** argv) {
	std::cout << "Starting App..." << std::endl;

	SDL_Window* window;
	SDL_Init (SDL_INIT_EVERYTHING);

	SDL_GL_SetAttribute (SDL_GL_RED_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_GREEN_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_BLUE_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_ALPHA_SIZE, 8);
	SDL_GL_SetAttribute (SDL_GL_BUFFER_SIZE, 32);

	SDL_GL_SetAttribute (SDL_GL_DOUBLEBUFFER, 1);

#if _DEBUG
	SDL_GL_SetAttribute (SDL_GL_CONTEXT_FLAGS, SDL_GL_CONTEXT_DEBUG_FLAG);
#endif

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

#if _DEBUG
	glEnable (GL_DEBUG_OUTPUT);
	glEnable (GL_DEBUG_OUTPUT_SYNCHRONOUS);
	glDebugMessageCallback (openGLDebugCallback, nullptr);
#endif

	std::cout << "Shaders: " << ShaderPos << std::endl
		<< "	[" << vertexShader << ", " << fragmentShader << "]" << std::endl;

	const Shader shader ((ShaderPos + vertexShader).c_str(), (ShaderPos + fragmentShader).c_str());
	shader.bind();

	std::cout << "shader initialized PointerID: " << shader.GetShaderID() << std::endl;

	VertexBuffer vertex_buffer (vertices, numVertices);
	std::cout << "vertexBuffer initialized PointerID: " << vertex_buffer.GET_BUFFER_ID() << std::endl;
	vertex_buffer.UNBIND();

	IndexBuffer index_buffer (indices, numIndices, sizeof(indices[0]));
	std::cout << "indexBuffer initialized PointerID: " << vertex_buffer.GET_BUFFER_ID() << std::endl;
	index_buffer.UNBIND();

	Uint64  perfCounterFrequency = SDL_GetPerformanceFrequency();
	Uint64  lastCounter          = SDL_GetPerformanceCounter();
	float32 delta;
	Uint64  Frames = 0;
	//WireFrame
	//glPolygonMode (GL_FRONT_AND_BACK,GL_LINE);

	bool close = false;
	while (!close) {
		glClearColor (0.1f, 0.1f, 0.1f, 1.0f);
		glClear (GL_COLOR_BUFFER_BIT);

		vertex_buffer.BIND();
		index_buffer.BIND();
		GLCALL (glDrawElements (GL_TRIANGLES, numIndices,GL_UNSIGNED_INT, nullptr));
		index_buffer.UNBIND();
		vertex_buffer.UNBIND();
		//vertex_buffer.BIND();
		//glDrawArrays (GL_POLYGON, 0, 4);
		//vertex_buffer.UNBIND();

		SDL_GL_SwapWindow (window);

		SDL_Event event;
		while (SDL_PollEvent (&event)) {
			if (event.type == SDL_QUIT) {
				close = true;
			}
		}

		Uint64 endCounter     = SDL_GetPerformanceCounter();
		UINT64 counterElapsed = endCounter - lastCounter;
		delta                 = float32 (counterElapsed) / float32 (perfCounterFrequency);
		Uint32 FPS            = Uint32 (float32 (perfCounterFrequency) / float32 (counterElapsed));

		if (Frames % 10 == 0) SDL_SetWindowTitle (window, ("FPS: " + std::to_string (FPS)).c_str());
		lastCounter = endCounter;
		Frames++;
	}

	//std::cin.get();
	return 0;
}
