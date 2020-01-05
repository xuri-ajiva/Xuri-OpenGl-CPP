#version 330 core

layout(location = 0) out vec4 f_color;

in vec3 v_normal;
in vec3 v_position;

uniform vec3 u_diffuse;
uniform vec3 u_specular;
uniform vec3 u_emissive;
uniform float u_shininess;

void main()
{
    // Vector from fragment to camera (camera always at 0,0,0)
    vec3 view = normalize(-v_position);
    vec3 light = normalize(vec3(2.0f, 2.0f, 2.0f));
    vec3 normal = normalize(v_normal);
    vec3 reflection = reflect(-light, normal);

    vec3 ambient = u_diffuse * 0.2;
    vec3 diffuse = max(dot(normal, light), 0.0) * u_diffuse;
    vec3 specular = pow(max(dot(reflection, view), 0.0), u_shininess) * u_specular;

    f_color = vec4(ambient + diffuse + specular + u_emissive, 1.0f);
}