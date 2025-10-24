using UnityEngine;

public class CenterDotOnGUI : MonoBehaviour
{
    public Color color = Color.red;
    public int size = 5;

    void OnGUI()
    {
        var prev = GUI.color;
        GUI.color = color;
        float x = (Screen.width  - size) * 0.5f;
        float y = (Screen.height - size) * 0.5f;
        GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
        GUI.color = prev;
    }
}