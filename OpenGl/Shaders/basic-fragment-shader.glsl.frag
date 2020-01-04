#version 330 core

layout(location = 0) out vec4 f_color;

in vec4 v_color;
in vec2 v_texpos;
uniform sampler2D u_texture;

uniform vec4 u_color;

void main() {
	vec4 textColor = texture(u_texture, v_texpos);
	f_color = textColor * (u_color +v_color);
	//f_color = vec4(u_color.x + v_color.x,u_color.y + v_color.y,u_color.z + v_color.z,u_color.w + v_color.w);
}