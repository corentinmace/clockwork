#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 VertexColor;

out vec4 FragColor;

// Material properties
uniform vec4 uDiffuseColor;
uniform vec4 uAmbientColor;
uniform vec4 uSpecularColor;
uniform vec4 uEmissionColor;
uniform bool uHasTexture;
uniform sampler2D uTexture;

// Lighting
uniform bool uLightingEnabled;
uniform vec3 uLightPosition;
uniform vec4 uLightColor;

void main()
{
    vec4 texColor = vec4(1.0);
    if (uHasTexture) {
        texColor = texture(uTexture, TexCoord);
        if (texColor.a < 0.1) {
            discard;
        }
    }

    if (uLightingEnabled) {
        vec3 norm = normalize(Normal);
        vec3 lightDir = normalize(uLightPosition - FragPos);

        // Ambient
        vec4 ambient = uAmbientColor;

        // Diffuse
        float diff = max(dot(norm, lightDir), 0.0);
        vec4 diffuse = diff * uDiffuseColor;

        // Specular (simple Blinn-Phong)
        vec3 viewDir = normalize(-FragPos);
        vec3 halfDir = normalize(lightDir + viewDir);
        float spec = pow(max(dot(norm, halfDir), 0.0), 32.0);
        vec4 specular = spec * uSpecularColor;

        vec4 result = (ambient + diffuse + specular + uEmissionColor) * texColor * VertexColor;
        FragColor = vec4(result.rgb, texColor.a * VertexColor.a);
    } else {
        FragColor = uDiffuseColor * texColor * VertexColor;
    }
}
