using System.Runtime.InteropServices.JavaScript;

namespace Wrecs.WebGL.GameFramework;

public interface ITextureLoader
{
    Task<JSObject> LoadTexture(string url,
                               bool mipMapping = true,
                               bool nearestNeighborMagnification = false);
}
