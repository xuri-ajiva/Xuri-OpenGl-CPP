#include "Shader.h"
#include <fstream>
#include <iostream>
#include <windows.h>

Shader::Shader(const char* vertexShaderFilename, const char* fragmentShaderFilename){
	shaderId = createShader (vertexShaderFilename, fragmentShaderFilename);
}

Shader::~Shader(){
	glDeleteProgram (shaderId);
}

void Shader::bind() const{
	glUseProgram (shaderId);
}

void Shader::unbind(){
	glUseProgram (0);
}

GLuint Shader::compile(std::string shaderSource, GLenum type){
	GLuint      id  = glCreateShader (type);
	const char* src = shaderSource.c_str();
	glShaderSource (id, 1, &src, 0);
	glCompileShader (id);

	int result;
	glGetShaderiv (id, GL_COMPILE_STATUS, &result);
	if (result != GL_TRUE) {
		int length = 0;
		glGetShaderiv (id, GL_INFO_LOG_LENGTH, &length);
		char* message = new char[length];
		glGetShaderInfoLog (id, length, &length, message);
		std::cout << "Shader compilation error: " << message << std::endl;
		delete[] message;
		return 0;
	}
	return id;
}

std::string Shader::parse(const char* filename){
	FILE* file;
	
#ifdef _WIN32
	if (fopen_s (&file, filename, "rb") != 0) {
		std::cout << "File " << filename << " not found" << std::endl;
		return "";
	}
	if (file == nullptr) {
		std::cout << "File " << filename << " not found" << std::endl;
		return "";
	}
#else
	file = fopen(filename, "rb");
	if (file == nullptr) {
		std::cout << "File " << filename << " not found" << std::endl;
		return "";
	}
#endif


	std::string contents;
	fseek (file, 0, SEEK_END);
	const size_t fileSize = ftell (file);
	rewind (file);
	contents.resize (fileSize);

	fread (&contents[0], 1, fileSize, file);
	fclose (file);

	return contents;
}

GLuint Shader::createShader(const char* vertexShaderFilename, const char* fragmentShaderFilename){
	const std::string vertexShaderSource   = parse (vertexShaderFilename);
	const std::string fragmentShaderSource = parse (fragmentShaderFilename);

	const GLuint program = glCreateProgram();
	const GLuint vs      = compile (vertexShaderSource, GL_VERTEX_SHADER);
	const GLuint fs      = compile (fragmentShaderSource, GL_FRAGMENT_SHADER);

	glAttachShader (program, vs);
	glAttachShader (program, fs);
	glLinkProgram (program);

    
	/*// del shader s // after link can all shader`s be deleted but do not del program
    glDetachShader(program, vs);
    glDetachShader(program, fs);

    glDeleteShader(vs);
    glDeleteShader(fs);
	//*/

	return program;
}
