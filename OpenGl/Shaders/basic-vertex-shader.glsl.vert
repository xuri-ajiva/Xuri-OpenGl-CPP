#version 330 core

layout(location = 0) in vec3 porstion;

void main() {
	gl_Position = vec4(porstion, 1.0f);
}