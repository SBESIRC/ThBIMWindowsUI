#version 460
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec3 aColor;
layout(location = 3) in int aComp;

layout(std140, binding = 0)uniform TransformsUBO{
	mat4 view_model;				// 0 ~ 64
	mat4 proj_view_model;			// 64 ~ 128
	mat3 transpose_inv_model;		// 128 ~ 176
	vec4 uUserClipPlane;			// 176 ~ 192
	mat4 model;						// 192 ~ 256
	vec4 uUserClipBox[6];			// 256 ~ 352
} ubo;

layout(location = 0) out vec3 vGoColor;
layout(location = 1) out vec3 vNormal;
layout(location = 2) out vec3 vFragPos;
layout(location = 3) flat out int vComp;
layout(location = 4) out float vDistance;
layout(location = 5) out float vDistanceM[6];
void main()
{

	vGoColor = aColor;
	vec4 p = vec4(aPos, 1.0);
	vec4 eyePos = ubo.model * p;
	vFragPos = eyePos.xyz;
	vDistance = dot(vFragPos, ubo.uUserClipPlane.xyz) - ubo.uUserClipPlane.w;
	for(int i=0;i<6;i++){
		vDistanceM[i]=dot(eyePos.xyz, ubo.uUserClipBox[i].xyz) - ubo.uUserClipBox[i].w;
		//if(dot(eyePos.xyz, ubo.uUserClipBox[i].xyz) - ubo.uUserClipBox[i].w<0.0){
		//	vDistanceM= -1.0;
		//	break;
		//}
	}
	vComp = aComp;
	vNormal = ubo.transpose_inv_model * aNormal;
	gl_Position = ubo.proj_view_model * vec4(aPos, 1.0);
}