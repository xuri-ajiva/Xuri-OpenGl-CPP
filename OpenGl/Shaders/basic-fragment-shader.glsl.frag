#version 330 core

layout(location = 0) out vec4 f_color;

in vec4 v_color;
in vec2 v_texpos;
uniform sampler2D u_texture;

uniform vec4 u_color;

void main() {
	vec4 textColor = texture(u_texture, v_texpos);
	f_color = textColor * (u_color +v_color);
}