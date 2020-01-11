#pragma once
#include <vector>
#include <fstream>

#include "defines.h"

#include "libs/glm/glm.hpp"
#include "Shader.h"
#include "VertexBuffer.h"
#include "IndexBuffer.h"
#include "libs/stb_image.h"
#include <variant>
#include <iostream>

struct BMFMaterial {
	glm::vec3 diffuse   = {};
	glm::vec3 specular  = {};
	glm::vec3 emissive  = {};
	float     shininess = 0.0F;
};

struct Material {
	BMFMaterial material   = {};
	GLuint      diffuseMap = {};
	GLuint      normalMap  = {};
};

class Mesh {
public:
	Mesh(void*    data, Uint64      numVertices, std::vector<Uint32> indices, Uint64 numIndices,
	     Material material, Shader* shader, VertexMode               mode) {

		this->vertexMode = mode;
		this->material   = material;
		this->shader     = shader;
		this->numIndices = numIndices;


		vertexBuffer = new VertexBuffer(data, numVertices, mode);
		PrintStatus('V', vertexBuffer->GET_BUFFER_ID(), "-> vertexBuffer", 3, MESSAGE_COLOR);
		indexBuffer = new IndexBuffer(indices.data(), numIndices, sizeof(indices[0]));
		PrintStatus('I', indexBuffer->GET_BUFFER_ID(), "-> indexBuffer", 5, MESSAGE_COLOR);

		diffuseLocation      = glGetUniformLocation(shader->GetShaderID(), "u_material.diffuse");
		specularLocation     = glGetUniformLocation(shader->GetShaderID(), "u_material.specular");
		emissiveLocation     = glGetUniformLocation(shader->GetShaderID(), "u_material.emissive");
		shininessLocation    = glGetUniformLocation(shader->GetShaderID(), "u_material.shininess");
		diffuseMapLocation   = glGetUniformLocation(shader->GetShaderID(), "u_diffuse_map");
		normalMapLocation    = glGetUniformLocation(shader->GetShaderID(), "u_normal_map");
		useNormalMapLocation = glGetUniformLocation(shader->GetShaderID(), "u_use_normal_map");
		useTexturesLocation  = glGetUniformLocation(shader->GetShaderID(), "u_use_textures");
	}

	~Mesh() {
		delete vertexBuffer;
		delete indexBuffer;
	}

	void Render() {
		vertexBuffer->BIND();
		indexBuffer->BIND();
		glUniform3fv(diffuseLocation, 1, (float*)&material.material.diffuse.x);
		glUniform3fv(specularLocation, 1, (float*)&material.material.specular.x);
		glUniform3fv(emissiveLocation, 1, (float*)&material.material.emissive.x);
		glUniform1f(shininessLocation, material.material.shininess);
		if (this->vertexMode == TextureOnly || this->vertexMode == TextureAndNormal) {
			glBindTexture(GL_TEXTURE_2D, material.diffuseMap);
			glUniform1i(diffuseMapLocation, 0);
			glUniform1i(useTexturesLocation, 1); //tell shader to use textures
		} else {
			glUniform1i(useTexturesLocation, 0);
		}
		if (this->vertexMode == TextureAndNormal) {
			glActiveTexture(GL_TEXTURE1);
			glBindTexture(GL_TEXTURE_2D, material.normalMap);
			glActiveTexture(GL_TEXTURE0);
			glUniform1i(normalMapLocation, 1);
			glUniform1i(useNormalMapLocation, 1); //tell shader to use normalMap
		} else {
			glUniform1i(useNormalMapLocation, 0);
		}
		glDrawElements(GL_TRIANGLES, numIndices, GL_UNSIGNED_INT, 0);
		//vertexBuffer->UNBIND();
		//indexBuffer->UNBIND();
	}

private:
	VertexMode    vertexMode;
	VertexBuffer* vertexBuffer;
	IndexBuffer*  indexBuffer;
	Shader*       shader;
	Material      material;
	Uint64        numIndices = 0;
	int           diffuseLocation;
	int           specularLocation;
	int           emissiveLocation;
	int           shininessLocation;
	int           diffuseMapLocation;
	int           normalMapLocation;
	int           useTexturesLocation;
	int           useNormalMapLocation;
};

class Model {
public:
	Model(const char* filename, Shader* shader) {
		this->FileName = filename;
		this->Shader   = shader;
	}

	void Init() {
		std::ifstream input        = std::ifstream(this->FileName, std::ios::in | std::ios::binary);
		UINT64        numMeshes    = 0;
		UINT64        numMaterials = 0;
		UINT64        sizeOfVertex = 0;
		VertexMode    vertexMode   = {};
		if (!input.is_open()) {
			PrintError('#', -1, "File Not Found!",MINUS_COLOR);
			return;
		}
		input.read((char*)&sizeOfVertex, sizeof(Uint64));
		switch (sizeOfVertex) {
			case sizeof(VertexTextureAndNormal):
				vertexMode = TextureAndNormal;
				break;
			case sizeof(VertexTextureOnly):
				vertexMode = TextureOnly;
				break;
			case sizeof(VertexMaterialOnly):
				vertexMode = MaterialOnly;
				break;
			default:
				PrintError('-', -1, "unknown Vertex Struct!",MINUS_COLOR);
				return;
		}

		input.read((char*)&numMaterials, sizeof(Uint64));
		for (Uint64 i = 0; i < numMaterials; i++) {
			Material material = {};
			input.read((char*)&material, sizeof(BMFMaterial));

			Uint64 diffuseMapNameLength = 0;
			input.read((char*)&diffuseMapNameLength, sizeof(Uint64));
			std::string diffuseMapName(diffuseMapNameLength, '\0');
			input.read((char*)&diffuseMapName[0], diffuseMapNameLength);
			PrintStatus('+', i, ("diffuseMap: " + diffuseMapName).c_str(),diffuseMapNameLength > 0 ? PLUS_COLOR : MINUS_COLOR,MESSAGE_COLOR);

			Uint64 normalMapNameLength = 0;
			input.read((char*)&normalMapNameLength, sizeof(Uint64));
			std::string normalMapName(normalMapNameLength, '\0');
			input.read((char*)&normalMapName[0], normalMapNameLength);
			PrintStatus('+', i, ("normalMap: " + normalMapName).c_str(),normalMapNameLength > 0 ? PLUS_COLOR : MINUS_COLOR,MESSAGE_COLOR);

			//assert(diffuseMapNameLength > 0);
			//assert(normalMapNameLength > 0);

			Int32 textureWidth  = 0;
			Int32 textureHeight = 0;
			Int32 bitsPerPixel  = 0;
			glGenTextures(2, &material.diffuseMap);
			stbi_set_flip_vertically_on_load(true);
			if (vertexMode == TextureOnly || vertexMode == TextureAndNormal) {
				auto textureBuffer = stbi_load(diffuseMapName.c_str(), &textureWidth, &textureHeight, &bitsPerPixel, 4);

				//assert(textureBuffer);
				//assert(material.diffuseMap);

				if (textureBuffer && material.diffuseMap) {
					glBindTexture(GL_TEXTURE_2D, material.diffuseMap);

					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

					glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, textureBuffer);

					if (textureBuffer) {
						stbi_image_free(textureBuffer);
					}
				} else {
					PrintError('-', i, "textureBuffer or material.diffuseMap missing",MINUS_COLOR);
				}
			}
			if (vertexMode == TextureAndNormal) {
				auto textureBuffer = stbi_load(normalMapName.c_str(), &textureWidth, &textureHeight, &bitsPerPixel, 4);

				//assert(textureBuffer);
				//assert(material.normalMap);

				if (textureBuffer && material.diffuseMap) {
					glBindTexture(GL_TEXTURE_2D, material.normalMap);

					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
					glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

					glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, textureWidth, textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, textureBuffer);

					if (textureBuffer) {
						stbi_image_free(textureBuffer);
					}
				} else {
					PrintError('-', i, "textureBuffer or material.diffuseMap missing",MINUS_COLOR);
				}
			}

			glBindTexture(GL_TEXTURE_2D, 0);
			PrintStatus('C', i, "-> Material", 11,MESSAGE_COLOR);
			materials.push_back(material);
		}
		// mesh
		input.read((char*)&numMeshes, sizeof(Uint64));

		meshes.reserve(numMeshes);
		for (Uint64 i = 0; i < numMeshes; i++) {
			std::vector<Uint32> indices;
			Uint64              numVertices   = 0;
			Uint64              numIndices    = 0;
			Uint64              materialIndex = 0;

			input.read((char*)&materialIndex, sizeof(Uint64));
			input.read((char*)&numVertices, sizeof(Uint64));
			input.read((char*)&numIndices, sizeof(Uint64));

			std::vector<VertexTextureAndNormal> verticesN;
			std::vector<VertexTextureOnly>      verticesT;
			std::vector<VertexMaterialOnly>     verticesM;

			void* data;

			switch (vertexMode) {
				case TextureAndNormal:
					for (Uint64 k = 0; k < numVertices; k++) {
						VertexTextureAndNormal vertex {};
						//input.read((char*)&vertex.position.x, sizeof(float));
						//input.read((char*)&vertex.position.y, sizeof(float));
						//input.read((char*)&vertex.position.z, sizeof(float));
						//input.read((char*)&vertex.normal.x, sizeof(float));
						//input.read((char*)&vertex.normal.y, sizeof(float));
						//input.read((char*)&vertex.normal.z, sizeof(float));
						//input.read((char*)&vertex.tangent.x, sizeof(float));
						//input.read((char*)&vertex.tangent.y, sizeof(float));
						//input.read((char*)&vertex.tangent.z, sizeof(float));
						//input.read((char*)&vertex.textureCord.x, sizeof(float));
						//input.read((char*)&vertex.textureCord.y, sizeof(float));
						input.read((char*)&vertex.position.x, sizeof(VertexTextureAndNormal));
						verticesN.push_back(vertex);
					}
					data = verticesN.data();
					break;
				case TextureOnly:
					for (Uint64 l = 0; l < numVertices; l++) {
						VertexTextureOnly vertex {};
						input.read((char*)&vertex.position.x, sizeof(float));
						input.read((char*)&vertex.position.y, sizeof(float));
						input.read((char*)&vertex.position.z, sizeof(float));
						input.read((char*)&vertex.normal.x, sizeof(float));
						input.read((char*)&vertex.normal.y, sizeof(float));
						input.read((char*)&vertex.normal.z, sizeof(float));
						input.read((char*)&vertex.textureCord.x, sizeof(float));
						input.read((char*)&vertex.textureCord.y, sizeof(float));
						//put.read((char*)&vertex.position.x, sizeof(VertexTextureOnly));
						verticesT.push_back(vertex);
					}
					data = verticesT.data();
					break;
				case MaterialOnly:
					for (Uint64 r = 0; r < numVertices; r++) {
						VertexMaterialOnly vertex {};
						//input.read((char*)&vertex.position.x, sizeof(float));
						//input.read((char*)&vertex.position.y, sizeof(float));
						//input.read((char*)&vertex.position.z, sizeof(float));
						//input.read((char*)&vertex.normal.x, sizeof(float));
						//input.read((char*)&vertex.normal.y, sizeof(float));
						//input.read((char*)&vertex.normal.z, sizeof(float));
						input.read((char*)&vertex.position.x, sizeof(VertexMaterialOnly));
						verticesM.push_back(vertex);
					}
					data = verticesM.data();
					break;
				default:
					PrintError('-', -1, "unknown Vertex Struct!",MINUS_COLOR);
					return;
			}

			for (Uint64 g = 0; g < numIndices; g++) {
				Uint32 index;
				input.read((char*)&index, sizeof(Uint32));
				indices.push_back(index);
			}

			Mesh* mesh = new Mesh(data, numVertices, indices, numIndices, materials[materialIndex], this->Shader, vertexMode);
			PrintStatus('M', i, "-> Mesh*", 13,MESSAGE_COLOR);
			meshes.push_back(mesh);
		}
	}

	void Render() {

		for (Mesh* mesh : meshes) {
			mesh->Render();
		}
	}

	~Model() {
		for (Mesh* mesh : meshes) {
			delete mesh;
		}
	}

private:
	Shader*               Shader   = {};
	const char*           FileName = {};
	std::vector<Mesh*>    meshes;
	std::vector<Material> materials;
};
