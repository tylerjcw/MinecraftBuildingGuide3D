using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace MinecraftBuildingGuide3D.Rendering
{
    /// <summary>
    /// Manages OpenGL shader compilation and uniforms
    /// </summary>
    public class ShaderProgram : IDisposable
    {
        public int Handle { get; private set; }
        private bool _disposed = false;

        public ShaderProgram(string vertexSource, string fragmentSource)
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                throw new Exception($"Shader program linking failed: {infoLog}");
            }

            // Clean up individual shaders (they're linked into the program now)
            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"{type} compilation failed: {infoLog}");
            }

            return shader;
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        #region Uniform Setters

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(location, value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform3(location, value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform4(location, value);
        }

        public void SetColor4(string name, Color4 value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform4(location, value.R, value.G, value.B, value.A);
        }

        public void SetMatrix4(string name, Matrix4 value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            GL.UniformMatrix4(location, false, ref value);
        }

        #endregion

        #region Default Shaders

        public static readonly string DefaultVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aInstancePosition;
layout (location = 3) in vec4 aInstanceColor;

out vec3 FragPos;
out vec3 Normal;
out vec4 BlockColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec3 worldPos = aPosition + aInstancePosition;
    FragPos = worldPos;
    Normal = aNormal;
    BlockColor = aInstanceColor;
    gl_Position = projection * view * model * vec4(worldPos, 1.0);
}
";

        public static readonly string DefaultFragmentShader = @"
#version 330 core
in vec3 FragPos;
in vec3 Normal;
in vec4 BlockColor;

out vec4 FragColor;

uniform vec3 lightDir;
uniform vec3 viewPos;
uniform float ambientStrength;
uniform int enableLighting;
uniform float globalAlpha;

void main()
{
    vec4 baseColor = BlockColor;
    baseColor.a *= globalAlpha;
    
    if (enableLighting == 1)
    {
        // Ambient
        vec3 ambient = ambientStrength * baseColor.rgb;
        
        // Diffuse
        vec3 norm = normalize(Normal);
        float diff = max(dot(norm, -lightDir), 0.0);
        vec3 diffuse = diff * baseColor.rgb;
        
        // Simple specular
        vec3 viewDir = normalize(viewPos - FragPos);
        vec3 reflectDir = reflect(lightDir, norm);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), 16.0);
        vec3 specular = 0.2 * spec * vec3(1.0);
        
        vec3 result = ambient + diffuse * 0.7 + specular;
        FragColor = vec4(result, baseColor.a);
    }
    else
    {
        FragColor = baseColor;
    }
}
";

        public static readonly string GridVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPosition;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * vec4(aPosition, 1.0);
}
";

        public static readonly string GridFragmentShader = @"
#version 330 core
out vec4 FragColor;

uniform vec4 gridColor;

void main()
{
    FragColor = gridColor;
}
";

        public static readonly string OutlineVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aInstancePosition;
layout (location = 3) in vec4 aInstanceColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec3 worldPos = aPosition + aInstancePosition;
    gl_Position = projection * view * model * vec4(worldPos, 1.0);
}
";

        public static readonly string OutlineFragmentShader = @"
#version 330 core
out vec4 FragColor;

uniform vec4 outlineColor;

void main()
{
    FragColor = outlineColor;
}
";

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                GL.DeleteProgram(Handle);
                _disposed = true;
            }
        }

        ~ShaderProgram()
        {
            // Do not call Dispose(false) here - OpenGL context may be gone
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}