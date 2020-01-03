#pragma once

#include  <GL/glew.h>
#include  "defines.h"
#include <string>

class Shader {
public:
	Shader(const char* vertexShaderFilename, const char* fragmentShaderFilename);

	virtual ~Shader();

	void bind() const;

	static void unbind();

private:

	static GLuint compile(std::string shaderSource, GLenum type);

	static std::string parse(const char* fileName);

	static GLuint createShader(const char* vertexShaderFilename, const char* fragmentShaderFilename);

	GLuint shaderId;
};
