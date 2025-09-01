using UnityEngine;

public abstract class Content
{
    protected GameObject panel;
    protected float contentLength;
    protected float startTime;
    protected int layer; // smaller = more front (on top)

    //protected int layer;

    public int GetLayer() { return layer; }
    public void SetLayer(int newLayer) { layer = newLayer; }

    public float getLength()
    {
        return contentLength;
    }

    public float getStart()
    {
        return startTime;
    }

    public float getEnd()
    {
        return startTime + contentLength;
    }

    public virtual void Show() { }
    public virtual void Hide() { }
    public virtual bool IsModel() { return false; }

    //public int GetLayer() => layer;
    //public void SetLayer(int l) => layer = l;

    public void SetStart(float s) => startTime = Mathf.Max(0f, s);
}
