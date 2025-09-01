using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImageContent : Content
{

    private string _filePath;

    private Texture2D texture;
    public Texture2D GetTexture() => texture;

    public RectTransform BarRT { get; private set; }
    public string getPath() => _filePath;

    public ImageContent(
        GameObject barPrefab,
        string path,
        int timeToPixel,
        RectTransform parentRow,     // parent is a ROW, not the raw content RT
        float startingPoint)
    {
        if (!barPrefab) { Debug.LogError("ImageContent: barPrefab null."); return; }
        if (!parentRow) { Debug.LogError("ImageContent: parentRow null."); return; }

        panel = barPrefab;
        startTime = startingPoint;
        contentLength = 20f; // default still duration
        _filePath = path;

        byte[] data = File.ReadAllBytes(path);
        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(data);

        var barRoot = Object.Instantiate(barPrefab);
        var rootRT = barRoot.GetComponent<RectTransform>() ?? barRoot.AddComponent<RectTransform>();
        rootRT.SetParent(parentRow, false);

        RectTransform rt =
            barRoot.transform.childCount > 0
                ? barRoot.transform.GetChild(0).GetComponent<RectTransform>()
                : rootRT;

        if (!rt) { Debug.LogError("ImageContent: bar prefab has no RectTransform."); return; }
        BarRT = rt;

        rt.SetParent(parentRow, false);
        rt.localScale = Vector3.one;
        // left-anchored, vertically centered IN THE ROW
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        float width = (contentLength / 4f) * timeToPixel;
        float posX = (startTime / 4f) * timeToPixel;

        rt.anchoredPosition = new Vector2(posX, 0f);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);

        var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
        img.color = Color.blue; // image = blue
        img.raycastTarget = true;
    }
}
