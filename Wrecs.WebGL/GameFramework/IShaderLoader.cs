using System.Runtime.InteropServices.JavaScript;

namespace Wrecs.WebGL.GameFramework;

public interface IShaderLoader
{
    JSObject LoadShaderProgram(string vertexShaderName, string fragmentShaderName);
}
