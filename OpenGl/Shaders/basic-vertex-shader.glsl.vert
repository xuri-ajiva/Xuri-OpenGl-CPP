#version 330 core

layout(location = 0) in vec3 a_porstion;
layout(location = 1) in vec2 a_texpos;
layout(location = 2) in vec4 a_color;

out vec4 v_color;
out vec2 v_texpos;

uniform mat4 u_modelViewProj;

void main() {
	gl_Position = u_modelViewProj * vec4(a_porstion, 1.0f);
	v_color =  /*vec4(normalize(a_porstion), 1.0f);*/ a_color;
	v_texpos = a_texpos;
}