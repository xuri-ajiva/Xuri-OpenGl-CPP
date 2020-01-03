#include "defines.h"

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

#include "VertexBuffer.h"
#include "Shader.h"
#include  "IndexBuffer.h"

#include "Framework.h"

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
	MainClass main_class {};

	const int init_err = main_class.Init();
	if (init_err != 0) {
		return init_err;
	}
	
	std::cout << "OpenGl version: " << glGetString (GL_VERSION) << std::endl;

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

	do {
		glClearColor (0.1f, 0.1f, 0.1f, 1.0f);
		glClear (GL_COLOR_BUFFER_BIT);

		vertex_buffer.BIND();
		index_buffer.BIND();
		GLCALL (glDrawElements (GL_TRIANGLES, numIndices,GL_UNSIGNED_INT, nullptr));
		index_buffer.UNBIND();
		vertex_buffer.UNBIND();
	}
	while (main_class.MainLoop());

	//std::cin.get();
	return 0;
}
