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

#include "libs/glm/glm.hpp"
#include "libs/glm/ext/matrix_transform.hpp"
#include "libs/glm/ext/matrix_relational.hpp"
#include "libs/glm/gtc/matrix_transform.hpp"

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
#include "Camera.h"
#include "fpsCamera.h"

#include "Framework.h"

Vertex vertices[] = {
	Vertex {
		-0.5f, -0.5f, 0.0f,
		0.0F, 0.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
	Vertex {
		-0.0f, 0.5f, 0.0f,
		0.0F, 1.0F,
		0.0f, 1.0f, 0.0f, 1.0f
	},
	Vertex {
		0.5f, -0.5f, 0.0f,
		1.0F, 0.0F,
		0.0f, 0.0f, 1.0f, 1.0f
	}, 
	Vertex {
		0.9f, 0.9f, 0.0f,
		1.0F, 1.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
	Vertex {
		-0.9f, -0.9f, -0.2f,
		0.0F, 0.0F,
		0.0f, 0.0f, 1.0f, 1.0f
	},
	Vertex {
		-0.0f, 0.9f, -0.2f,
		0.0F, 1.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
	Vertex {
		0.9f, -0.9f, -0.2f,
		1.0F, 0.0F,
		0.0f, 1.0f, 1.0f, 1.0f
	},
	Vertex {//7
		0.9f, 0.9f, -0.2f,
		1.0F, 1.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
	Vertex {//8
		-5.0f, 0.0f, -5.0f,
		0.0F, 0.0F,
		0.0f, 0.0f, 1.0f, 1.0f
	},
	Vertex {//9
		5.0f, 0.0f, -5.0f,
		0.0F, 1.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
	Vertex {//10
		5.0f, 0.0f, 5.0f,
		1.0F, 0.0F,
		0.0f, 1.0f, 1.0f, 1.0f
	},
	Vertex { //11
		-5.0f, 0.0f, 5.0f,
		1.0F, 1.0F,
		1.0f, 0.0f, 0.0f, 1.0f
	},
};
Uint32 numVertices = (sizeof(vertices) / sizeof(*vertices));

Uint32 indices [] = {
	0, 1, 2,
	//8,9,10,
	//8,10,11,
	//4, 1, 6,

	//0, 1, 4,
	//1, 5, 4,
	//
	//1, 2, 5,
	//2, 5, 6,
	//
	//0, 2, 4,
	//2, 4, 6,
};
Uint32 numIndices = (sizeof(indices) / sizeof(*indices));

#if _DEBUG
void              _GLGetError(const char* file, int line, const char* call) {
	while (GLenum error = glGetError()) {
		std::cout << "[Open info: {file: " << file << ", line: " << line << ", call: " << call <<
			", error: " << error << "}]: " << glewGetErrorString(error);
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

	std::cout << "OpenGl version: " << glGetString(GL_VERSION) << std::endl;

	std::cout << "Shaders: " << DataPos << std::endl
		<< "	[" << vertexShader << ", " << fragmentShader << "]" << std::endl;

	const Shader shader((DataPos + vertexShader).c_str(), (DataPos + fragmentShader).c_str());
	shader.bind();

	const int colorUniformLocation = GLCALL(glGetUniformLocation (shader.GetShaderID(), "u_color"));

	if (colorUniformLocation != -1) {
		glUniform4f(colorUniformLocation, 1, 1, 1, 1.0F);
	}


	Int32 textureWidth  = 0;
	Int32 textureHeight = 0;
	Int32 bitsPerPixel  = 0;
	stbi_set_flip_vertically_on_load(true);
	auto textureBuffer = stbi_load((DataPos + "abstract.jpg").c_str(), &textureWidth, &textureHeight, &bitsPerPixel, 4);

	GLuint textureId;
	GLCALL(glGenTextures(1, &textureId));
	GLCALL(glBindTexture(GL_TEXTURE_2D, textureId));

	GLCALL(glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR));
	GLCALL(glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR));
	GLCALL(glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE));
	GLCALL(glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE));

	GLCALL(
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE,
			textureBuffer));
	GLCALL(glBindTexture(GL_TEXTURE_2D, 0));

	if (textureBuffer) {
		stbi_image_free(textureBuffer);
	}


	std::cout << "shader initialized PointerID: " << shader.GetShaderID() << std::endl;

	VertexBuffer vertex_buffer(vertices, numVertices);
	std::cout << "vertexBuffer initialized PointerID: " << vertex_buffer.GET_BUFFER_ID() << std::endl;
	vertex_buffer.UNBIND();

	IndexBuffer index_buffer(indices, numIndices, sizeof(indices[0]));
	std::cout << "indexBuffer initialized PointerID: " << vertex_buffer.GET_BUFFER_ID() << std::endl;
	index_buffer.UNBIND();

	auto model_rotate = glm::mat4(1.0F);
	model_rotate      = scale(model_rotate, glm::vec3(3.0F));

	FPSCamera camara(90.0F, 1000, 800);
	camara.Translate(glm::vec3(0, 0, 5.0f));
	camara.update();

	auto modelViewProj = camara.GetViewProj() * model_rotate;

	const int modelViewProjMatrixLocation = GLCALL(glGetUniformLocation(shader.GetShaderID(), "u_modelViewProj"));

	bool    b_W         = false;
	bool    b_S         = false;
	bool    b_A         = false;
	bool    b_D         = false;
	float32 camaraSpeed = 6.0F;

	do {
		SDL_Event event;
		while (SDL_PollEvent(&event)) {
			if (event.type == SDL_QUIT) {
				return 0;
			}
			if (event.type == SDL_KEYDOWN || event.type == SDL_KEYUP) {
				//if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) {	//	if (orthoCam) {	//		projection = perspective;	//	}	//	else {	//		projection = ortho;	//	}	//	//	orthoCam = !orthoCam;	//}	
				const auto state = event.type == SDL_KEYDOWN;
				switch (event.key.keysym.sym) {
					case SDLK_ESCAPE: return 0;
					case SDLK_w: b_W = state;
						break;
					case SDLK_a: b_A = state;
						break;
					case SDLK_s: b_S = state;
						break;
					case SDLK_d: b_D = state;
						break;
					case SDLK_F1: if (state) {
							camara.SetFreeCam(!camara.GetFreeCam());
						}
						break;
					default: ;
				}
				if (event.key.keysym.mod & KMOD_LSHIFT) {
					camaraSpeed = 14.0;
				}
				else {
					camaraSpeed = 6.0;
				}
				//if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) { }
			}
			else if (event.type == SDL_MOUSEMOTION) {
				camara.onMouseMoved(event.motion.xrel, event.motion.yrel);
			}
		}

		glm::vec3 translat = glm::vec3(0);
		if (b_W) {
			camara.moveFront(main_class.delta * camaraSpeed);
		}
		if (b_S) {
			camara.moveFront(main_class.delta * -camaraSpeed);
		}
		if (b_A) {
			camara.moveSideways(main_class.delta * -camaraSpeed);
		}
		if (b_D) {
			camara.moveSideways(main_class.delta * camaraSpeed);
		}
		camara.update();


		model_rotate = glm::rotate(model_rotate, sinf(main_class.time) * 1 * main_class.delta, glm::vec3(1, 1, 1));

		modelViewProj = camara.GetViewProj() * model_rotate;


		glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
		glClear(GL_COLOR_BUFFER_BIT);

		float32 r = (sinf(main_class.time) + 1.0F) / 2.0F;
		float32 g = (cosf(main_class.time) + 1.0F) / 2.0F;
		float32 b = ((-sinf(main_class.time)) + 1.0F) / 2.0F;

		if (colorUniformLocation != -1) {
			glUniform4f(colorUniformLocation, r, g, b, 1.0F);
		}

		vertex_buffer.BIND();
		index_buffer.BIND();
		GLCALL(glActiveTexture(GL_TEXTURE0));
		GLCALL(glBindTexture(GL_TEXTURE_2D, textureId));

		GLCALL(glUniformMatrix4fv(modelViewProjMatrixLocation,1,GL_FALSE, &modelViewProj[0][0]));
		GLCALL(glDrawElements(GL_TRIANGLES, numIndices, GL_UNSIGNED_INT, 0));
		index_buffer.UNBIND();
		vertex_buffer.UNBIND();
	}
	while (main_class.MainLoop());
	/*
	glDeleteTextures (1, &textureId);
*/
	//std::cin.get();
	return 0;
}
