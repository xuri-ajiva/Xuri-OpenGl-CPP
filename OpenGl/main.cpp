#define NO_DEINES_XURI
#include "defines.h"
#define _SILENCE_EXPERIMENTAL_FILESYSTEM_DEPRECATION_WARNING

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

#include "libs/glm/glm.hpp"
#include "libs/glm/ext/matrix_transform.hpp"
#include "libs/glm/ext/matrix_relational.hpp"
#include "libs/glm/gtc/matrix_transform.hpp"

#include <fstream>
#include <iostream>
#include <experimental/filesystem>
namespace fs = std::experimental::filesystem;

//#define BOOST_FILESYSTEM_VERSION 3
//#define BOOST_FILESYSTEM_NO_DEPRECATED 
//#include <boost/filesystem.hpp>

#ifdef _WIN32
    #include <windows.h>
#else
    #include <unistd.h>
#endif // _WIN32

#define STB_IMAGE_IMPLEMENTATION
#include "libs/stb_image.h"
#undef STB_IMAGE_IMPLEMENTATION

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

std::string ShaderPos      = "shaders/";
std::string ModelsPos      = "models/";
const char* vertexShader   = "basic-vertex-shader.glsl.vert";
const char* fragmentShader = "basic-fragment-shader.glsl.frag";

int main(int argc, char** argv) {
	std::vector<Model*>     models;
	std::vector<glm::mat4> mats;

	//std::string modelFile = (ModelsPos + "tree.bmf");
	//if (argc >= 2) {
	//	modelFile = argv[1];
	//}
	MainClass main_class {};

	const int init_err = main_class.Init();
	if (init_err != 0) {
		return init_err;
	}

	std::cout << "OpenGl version: " << glGetString(GL_VERSION) << std::endl;

	std::cout << "Shaders: " << ShaderPos << std::endl << "	[" << vertexShader << ", " << fragmentShader << "]" << std::endl;

	Shader shader((ShaderPos + vertexShader).c_str(), (ShaderPos + fragmentShader).c_str());
	shader.bind();
	PrintStatus('S', shader.GetShaderID(), "-> Shader", 9,MESSAGE_COLOR);

	int       directionLocation = GLCALL(glGetUniformLocation(shader.GetShaderID(), "u_directional_light.direction"));
	glm::vec3 sunColor          = glm::vec3(1);
	glm::vec3 sunDirection      = glm::vec3(-1.0F);
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_directional_light.diffuse"),1,(float*)&sunColor));
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_directional_light.specular"),1,(float*)&sunColor));
	sunColor *= 0.4F;
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_directional_light.ambient"),1,(float*)&sunColor));

	glm::vec3 pointColor = glm::vec3(0, 0, 3);
	pointColor *= 0;
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_point_light.diffuse"),1,(float*)&pointColor));
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_point_light.specular"),1,(float*)&pointColor));
	pointColor *= 0.2F;
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_point_light.ambient"),1,(float*)&pointColor));
	GLCALL(glUniform1f(glGetUniformLocation(shader.GetShaderID(), "u_point_light.linear"), 0.027f));
	GLCALL(glUniform1f(glGetUniformLocation(shader.GetShaderID(), "u_point_light.quadratic"), 0.0028f));
	glm::vec4 pointLightPosition = glm::vec4(1, 1, 10, 1);
	int       pointLightLocation = GLCALL(glGetUniformLocation(shader.GetShaderID(), "u_point_light.position"));

	glm::vec3 spotColor = glm::vec3(2);
	spotColor *= 0;
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_spot_light.diffuse"),1,(float*)&spotColor));
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_spot_light.specular"),1,(float*)&spotColor));
	spotColor *= 0.1F;
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_spot_light.ambient"),1,(float*)&spotColor));
	glm::vec3 spotLightPosition = glm::vec3(0.0F);
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_spot_light.position"), 1, (float*)&spotLightPosition));
	spotLightPosition.z = 1.0f;
	GLCALL(glUniform3fv(glGetUniformLocation(shader.GetShaderID(), "u_spot_light.direction"), 1, (float*)&spotLightPosition));
	GLCALL(glUniform1f( glGetUniformLocation(shader.GetShaderID(), "u_spot_light.innerCone"), 0.99f));
	GLCALL(glUniform1f( glGetUniformLocation(shader.GetShaderID(), "u_spot_light.outerCone"), 0.98f));

	std::string path(ModelsPos);
	std::string ext(".bmf");
	int         idx = 0;
	for (auto& p : fs::recursive_directory_iterator(path)) {
		if (p.path().extension() == ext) {
			std::cout << "Loading: " << p << '\n';
			auto t = p.path().string();
			Model*      model = new Model(t.c_str(), &shader);
			model->Init();
			models.push_back(model);
			auto mat4X = glm::mat4(1.0F);
			mat4X      = glm::translate(mat4X, glm::vec3(100 * (idx++), 0, 0));
			mat4X      = glm::scale(mat4X, glm::vec3(.2));
			mats.push_back(mat4X);
		}
	}


	auto camModelRender = glm::scale(glm::mat4(1.0F), glm::vec3(.2));


	//model_rotate      = scale(model_rotate, glm::vec3(.1F));

	FPSCamera camera(90.0F, 1000, 800);
	camera.Translate(glm::vec3(0, 0, 5.0f));
	camera.update();

	auto modelViewProj = camera.GetViewProj() * camModelRender;

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

	float32 camaraSpeed = 60.0F;

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
					case SDLK_ESCAPE:
						return 0;
					case SDLK_w:
						b_W = state;
						break;
					case SDLK_a:
						b_A = state;
						break;
					case SDLK_s:
						b_S = state;
						break;
					case SDLK_d:
						b_D = state;
						break;
					case SDLK_q:
						b_Q = state;
						break;
					case SDLK_e:
						b_E = state;
						break;
					case SDLK_F1:
						if (state) camera.SetFreeCam(!camera.GetFreeCam());
						break;
					case SDLK_F2:
						if (state) SDL_SetRelativeMouseMode(SDL_FALSE);
						break;
					default: ;
				}
				if (event.key.keysym.mod & KMOD_LSHIFT) {
					camaraSpeed = 140.0;
				} else if (event.key.keysym.mod & KMOD_LCTRL) {
					camaraSpeed = 2.0;
				} else {
					camaraSpeed = 60.0;
				}
				
				//if (event.key.keysym.sym == SDLK_p && event.key.keysym.mod & KMOD_LALT) { }
			} else if (event.type == SDL_MOUSEMOTION) {
				if (SDL_GetRelativeMouseMode()) camera.onMouseMoved(event.motion.xrel, event.motion.yrel);
			} else if (event.type == SDL_MOUSEBUTTONDOWN) {
				if (event.button.button == SDL_BUTTON_LEFT) {
					SDL_SetRelativeMouseMode(SDL_TRUE);
				}
			} else if (event.type == SDL_WINDOWEVENT) {
				int w, h;
				SDL_GetWindowSize(main_class.window, &w, &h);
				glViewport(0, 0, w, h);
			}
			//else if (event.type == SDL_WINDOWEVENT_FOCUS_LOST) {
			//	SDL_SetRelativeMouseMode(SDL_FALSE);
			//}
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
		modelViewProj          = camera.GetViewProj() * camModelRender;
		glm::mat4 modelView    = camera.GetView() * camModelRender;
		glm::mat4 invModelView = glm::transpose(glm::inverse(modelView));

		glm::vec4 transformedSunDirection = glm::transpose(glm::inverse(camera.GetView())) * glm::vec4(
			sunDirection, 1.0F);
		glUniform3fv(directionLocation, 1, (float*)&transformedSunDirection);

		glm::mat4 pointLightMatrix              = glm::rotate(glm::mat4(1), - main_class.delta, {0, 1, 0});
		pointLightPosition                      = pointLightPosition * pointLightMatrix;
		glm::vec3 transformedPointLightPosition = (glm::vec3)(camera.GetView() * (pointLightPosition));
		glUniform3fv(pointLightLocation, 1, (float*)&transformedPointLightPosition);


		GLCALL(glUniformMatrix4fv(modelViewProjMatrixLocation, 1, GL_FALSE, &modelViewProj[0][0]));
		GLCALL(glUniformMatrix4fv(modelViewLocation, 1, GL_FALSE, &modelView[0][0]));
		GLCALL(glUniformMatrix4fv(invModelViewLocation, 1, GL_FALSE, &invModelView[0][0]));

		///

		for (int i = 0; i < idx; ++i) {
			//mats[i]   =  glm::rotate(mats[i],main_class.time * , glm::vec3 (0, main_class.time,0));
			modelViewProj = camera.GetViewProj() * mats[i];
			modelView     = camera.GetView() * mats[i];
			invModelView  = glm::transpose(glm::inverse(modelView));
			
			GLCALL(glUniformMatrix4fv(modelViewProjMatrixLocation, 1, GL_FALSE, &modelViewProj[0][0]));
			GLCALL(glUniformMatrix4fv(modelViewLocation, 1, GL_FALSE, &modelView[0][0]));
			GLCALL(glUniformMatrix4fv(invModelViewLocation, 1, GL_FALSE, &invModelView[0][0]));

			models[i]->Render();
		}
	}
	while (main_class.MainLoop());
	//std::cin.get();
	return 0;
}
