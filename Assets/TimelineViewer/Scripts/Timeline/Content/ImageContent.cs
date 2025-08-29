using System.IO;
using UnityEngine;

public class ImageContent : Content
{
    private string _filePath;
    private Texture2D texture;
    public Texture2D GetTexture() => texture;
    public string getPath() => _filePath;

    public void SetLength(float newLength)
    {
        contentLength = Mathf.Max(0.1f, newLength);
    }

    public ImageContent(string path, float startingPoint)
    {
        startTime = startingPoint;
        contentLength = 20f;
        _filePath = path;

        byte[] data = File.ReadAllBytes(path);
        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(data);
    }
}