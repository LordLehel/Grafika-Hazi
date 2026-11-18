#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;

// a hatterfeny szine
uniform vec3 uAmbientLightColor;

// az illuminacios egyutthatok szinkomponensenkent (r, g, b)
uniform vec3 uAmbientStrength;
uniform vec3 uDiffuseStrength;
uniform vec3 uSpecularStrength;

uniform float uShininess;

		
in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;

void main()
{
    // komponensenkenti szorzas (r*r, g*g, b*b)
    vec3 ambient = uAmbientStrength * uAmbientLightColor;

    vec3 norm = normalize(outNormal);
    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor * uDiffuseStrength;

    vec3 viewDir = normalize(uViewPos - outWorldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    // a spekularis egyutthato most szorozza a csillanast
    vec3 specular = spec * uLightColor * uSpecularStrength;

    vec3 result = (ambient + diffuse + specular) * outCol.rgb;

    FragColor = vec4(result, outCol.w);
}