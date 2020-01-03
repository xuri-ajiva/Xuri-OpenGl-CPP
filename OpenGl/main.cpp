#include "defines.h"

#include <ctime>
#include <iomanip>
#include <iostream>
#include <GL/glew.h>
#include <SDL2/SDL.h>
#include <chrono>
#include <thread>
#include <cmath>

#define STB_IMAGE_IMPLEMENTATION
#include "libs/stb_image.h"

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
	Vertex {
		-0.9f, -0.9f, 0.0f,
		0.0F, 0.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
	Vertex {
		-0.9f, 0.9f, 0.0f,
		0.0F, 1.0F,
		0.0f, 1.0f, 0.0f, 1.0f
	},
	Vertex {
		0.9f, -0.9f, 0.0f,
		1.0F, 0.0F,
		0.0f, 0.0f, 1.0f, 1.0f
	},
	Vertex {
		0.9f, 0.9f, 0.0f,
		1.0F, 1.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
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

std::string DataPos        = "Shaders/";
const char* vertexShader   = "basic-vertex-shader.glsl.vert";
const char* fragmentShader = "basic-fragment-shader.glsl.frag";

int main(int argc, char** argv) {
	MainClass main_class {};

	const int init_err = main_class.Init();
	if (init_err != 0) {
		return init_err;
	}

	std::cout << "OpenGl version: " << glGetString (GL_VERSION) << std::endl;

	std::cout << "Shaders: " << DataPos << std::endl
		<< "	[" << vertexShader << ", " << fragmentShader << "]" << std::endl;

	const Shader shader ((DataPos + vertexShader).c_str(), (DataPos + fragmentShader).c_str());
	shader.bind();

	const int colorUniformLocation = GLCALL (glGetUniformLocation (shader.GetShaderID(), "u_color"));


	Int32 textureWidth  = 0;
	Int32 textureHeight = 0;
	Int32 bitsPerPixel  = 0;
	stbi_set_flip_vertically_on_load (true);
	auto textureBuffer = stbi_load ((DataPos + "index.png").c_str(), &textureWidth, &textureHeight, &bitsPerPixel, 4);

	GLuint textureId;
	GLCALL (glGenTextures(1, &textureId));
	GLCALL (glBindTexture(GL_TEXTURE_2D, textureId));

	GLCALL (glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR));
	GLCALL (glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR));
	GLCALL (glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE));
	GLCALL (glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE));

	GLCALL (
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE,
			textureBuffer));
	GLCALL (glBindTexture(GL_TEXTURE_2D, 0));

	if (textureBuffer) {
		stbi_image_free (textureBuffer);
	}


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

		if (colorUniformLocation != -1) {
			float32 r = (sinf (main_class.time) + 1.0F) / 2.0F;
			float32 g = (cosf (main_class.time) + 1.0F) / 2.0F;;
			float32 b = (-sinf (main_class.time) + 1.0F) / 2.0F;;

			glUniform4f (colorUniformLocation, r, g, b, 0.0F);
		}

		vertex_buffer.BIND();
		index_buffer.BIND();
		GLCALL (glActiveTexture(GL_TEXTURE0));
		GLCALL (glBindTexture(GL_TEXTURE_2D, textureId));
		GLCALL (glDrawElements(GL_TRIANGLES, numIndices, GL_UNSIGNED_INT, 0));
		index_buffer.UNBIND();
		vertex_buffer.UNBIND();
	}
	while (main_class.MainLoop());

	glDeleteTextures (1, &textureId);

	//std::cin.get();
	return 0;
}
