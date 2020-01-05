#include "defines.h"

//#define TEXTURE_MY

#include <ctime>
#include <iomanip>
#include <iostream>
#include <GL/glew.h>
#include <SDL2/SDL.h>
#include <vector>
#include <chrono>
#include <thread>
#include <fstream>
#include <cmath>

#ifdef TEXTURE_MY
#define STB_IMAGE_IMPLEMENTATION
#include "libs/stb_image.h"
#endif

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
#include "IndexBuffer.h"
#include "Camera.h"
#include "fpsCamera.h"

#include "Framework.h"
#include "mesh.h"

/*
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


Uint64 indices [] = {
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
*/
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

std::string DataPos        = "data/";
const char* vertexShader   = "basic-vertex-shader.glsl.vert";
const char* fragmentShader = "basic-fragment-shader.glsl.frag";

int main(int argc, char** argv) {
	std::string modelFile = (DataPos + "tree.bmf");
	if(argc >=2) {
		modelFile = argv[1]; 
	}
	MainClass main_class {};

	const int init_err = main_class.Init();
	if (init_err != 0) {
		return init_err;
	}

	std::cout << "OpenGl version: " << glGetString(GL_VERSION) << std::endl;

	std::cout << "Shaders: " << DataPos << std::endl
		<< "	[" << vertexShader << ", " << fragmentShader << "]" << std::endl;

	Shader shader((DataPos + vertexShader).c_str(), (DataPos + fragmentShader).c_str());
	shader.bind();

	std::cout << "shader initialized PointerID: " << shader.GetShaderID() << std::endl;

	//const int colorUniformLocation = GLCALL(glGetUniformLocation (shader.GetShaderID(), "u_color"));
	//
	//if (colorUniformLocation != -1) {
	//	glUniform4f(colorUniformLocation, 1, 1, 1, 1.0F);
	//}

#ifdef TEXTURE_MY
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
#endif

	Model renderModle;
	renderModle.Init(modelFile.c_str(), &shader);

	auto model_rotate = glm::mat4(1.0F);
	model_rotate      = scale(model_rotate, glm::vec3(.1F));

	FPSCamera camera(90.0F, 1000, 800);
	camera.Translate(glm::vec3(0, 0, 5.0f));
	camera.update();

	auto modelViewProj = camera.GetViewProj() * model_rotate;

	int modelViewProjMatrixLocation = glGetUniformLocation(shader.GetShaderID(), "u_modelViewProj");
	int modelViewLocation           = glGetUniformLocation(shader.GetShaderID(), "u_modelView");
	int invModelViewLocation        = glGetUniformLocation(shader.GetShaderID(), "u_invModelView");

	bool b_W = false;
	bool b_S = false;
	bool b_A = false;
	bool b_D = false;
	bool b_Q = false;
	bool b_E = false;

	//glPolygonMode (GL_FRONT_AND_BACK,GL_LINE);

	float32 camaraSpeed = 6.0F;

	GLCALL(glEnable(GL_DEPTH_TEST));
	do {
		glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
		GLCALL(glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT));
		GLCALL(glClear(GL_DEPTH_BUFFER_BIT));

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
					case SDLK_q: b_Q = state;
						break;
					case SDLK_e: b_E = state;
						break;
					case SDLK_F1: if (state) camera.SetFreeCam(!camera.GetFreeCam());
						break;
					case SDLK_F2: if (state) SDL_SetRelativeMouseMode(SDL_FALSE);
						break;
					default: ;
				}
				if (event.key.keysym.mod & KMOD_LSHIFT) {
					camaraSpeed = 14.0;
				}
				else if (event.key.keysym.mod & KMOD_LCTRL) {
					camaraSpeed = 2.0;
				}
				else {
					camaraSpeed = 6.0;
				}
				
				//if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) { }
			}
			else if (event.type == SDL_MOUSEMOTION) {
				if (SDL_GetRelativeMouseMode()) camera.onMouseMoved(event.motion.xrel, event.motion.yrel);
			}
			else if (event.type == SDL_MOUSEBUTTONDOWN) {
				if (event.button.button == SDL_BUTTON_LEFT) {
					SDL_SetRelativeMouseMode(SDL_TRUE);
				}
			}
		}

		if (b_W) {
			camera.moveFront(main_class.delta * camaraSpeed);
		}
		if (b_S) {
			camera.moveFront(main_class.delta * -camaraSpeed);
		}
		if (b_A) {
			camera.moveSideways(main_class.delta * -camaraSpeed);
		}
		if (b_D) {
			camera.moveSideways(main_class.delta * camaraSpeed);
		}
		if (b_Q) {
			camera.moveUp(main_class.delta * camaraSpeed);
		}
		if (b_E) {
			camera.moveUp(-main_class.delta * camaraSpeed);
		}

		camera.update();
		model_rotate           = glm::rotate(model_rotate, 1.0f * main_class.delta, glm::vec3(0, 1, 0));
		modelViewProj          = camera.GetViewProj() * model_rotate;
		glm::mat4 modelView    = camera.GetView() * model_rotate;
		glm::mat4 invModelView = glm::transpose(glm::inverse(modelView));

		GLCALL(glUniformMatrix4fv(modelViewProjMatrixLocation, 1, GL_FALSE, &modelViewProj[0][0]));
		GLCALL(glUniformMatrix4fv(modelViewLocation, 1, GL_FALSE, &modelView[0][0]));
		GLCALL(glUniformMatrix4fv(invModelViewLocation, 1, GL_FALSE, &invModelView[0][0]));
		renderModle.Render();
	}
	while (main_class.MainLoop());
#ifdef TEXTURE_MY
	glDeleteTextures(1, &textureId);
#endif
	//std::cin.get();
	return 0;
}
