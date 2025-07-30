using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GraphMaker : MonoBehaviour
{
    [Header("Sprite")]
    public Sprite circleSprite; 

    [Header("Target UI")]
    public RawImage graphImage;

    //internal variables
    List<GameObject> allGraphElements = new();

    private void Start()
    {
        
    }

    GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject circle = new GameObject("Circle", typeof(Image));
        circle.transform.SetParent(graphImage.rectTransform, false);
        circle.GetComponent<Image>().sprite = circleSprite;

        RectTransform circleRectTransform = circle.GetComponent<RectTransform>();
        circleRectTransform.anchoredPosition = anchoredPosition;
        circleRectTransform.sizeDelta = new Vector2(10, 10);
        circleRectTransform.anchorMin = new Vector2(0f, 0f);
        circleRectTransform.anchorMax = new Vector2(0f, 0f);

        allGraphElements.Add(circle);

        return circle;
    }

    public void ShowGraph(List<float> values)
    {
        ClearGraph();

        float padLeft = 35f;
        float padRight = 10f;
        float padBottom = 30f;
        float padTop = 10f;

        // Get drawable area from the RectTransform actually used for plotting
        RectTransform rt = graphImage.rectTransform;
        float contentWidth = rt.rect.width - padLeft - padRight;
        float contentHeight = rt.rect.height - padTop - padBottom;

        // Data
        int n = values.Count; // prefer .Count over .Count()
        if (n == 0) return; // or return;

        // Y range with 20% headroom
        double minYd = values.Min();
        double maxYd = values.Max();
        double range = Mathf.Max(1e-6f, (float)maxYd - (float)minYd); // prevent div by zero
        double margin = 0.2 * range;

        float yMin = (float)(minYd - margin);
        float yMax = (float)(maxYd + margin);
        float yRange = yMax - yMin;

        // Clear previous points/lines if needed

        GameObject lastDot = null;

        // If only 1 point, place it in the middle on X for aesthetics
        float denom = Mathf.Max(1, n - 1);

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0.5f : (float)i / denom; // 0..1 across the width
            float xPos = padLeft + t * contentWidth;

            float v = (float)values[i];
            float yNorm = Mathf.InverseLerp(yMin, yMax, v); // (v - yMin)/yRange
            float yPos = padBottom + yNorm * contentHeight;

            GameObject dot = CreateCircle(new Vector2(xPos, yPos));

            if (lastDot != null)
            {
                CreateLine(
                    lastDot.GetComponent<RectTransform>().anchoredPosition,
                    dot.GetComponent<RectTransform>().anchoredPosition
                );
            }
            lastDot = dot;
        }
    }

    public void ShowGraph(List<double> values)
    {
        ClearGraph();

        float padLeft = 35f;
        float padRight = 10f;
        float padBottom = 30f;
        float padTop = 10f;

        // Get drawable area from the RectTransform actually used for plotting
        RectTransform rt = graphImage.rectTransform;
        float contentWidth = rt.rect.width - padLeft - padRight;
        float contentHeight = rt.rect.height - padTop - padBottom;

        // Data
        int n = values.Count; // prefer .Count over .Count()
        if (n == 0) return; // or return;

        // Y range with 20% headroom
        double minYd = values.Min();
        double maxYd = values.Max();
        double range = Mathf.Max(1e-6f, (float)maxYd - (float)minYd); // prevent div by zero
        double margin = 0.2 * range;

        float yMin = (float)(minYd - margin);
        float yMax = (float)(maxYd + margin);
        float yRange = yMax - yMin;

        // Clear previous points/lines if needed

        GameObject lastDot = null;

        // If only 1 point, place it in the middle on X for aesthetics
        float denom = Mathf.Max(1, n - 1);

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0.5f : (float)i / denom; // 0..1 across the width
            float xPos = padLeft + t * contentWidth;

            float v = (float)values[i];
            float yNorm = Mathf.InverseLerp(yMin, yMax, v); // (v - yMin)/yRange
            float yPos = padBottom + yNorm * contentHeight;

            GameObject dot = CreateCircle(new Vector2(xPos, yPos));

            if (lastDot != null)
            {
                CreateLine(
                    lastDot.GetComponent<RectTransform>().anchoredPosition,
                    dot.GetComponent<RectTransform>().anchoredPosition
                );
            }
            lastDot = dot;
        }
    }

    public void ShowGraph(List<int> values)
    {
        ClearGraph();

        float padLeft = 35f;
        float padRight = 10f;
        float padBottom = 30f;
        float padTop = 10f;

        // Get drawable area from the RectTransform actually used for plotting
        RectTransform rt = graphImage.rectTransform;
        float contentWidth = rt.rect.width - padLeft - padRight;
        float contentHeight = rt.rect.height - padTop - padBottom;

        // Data
        int n = values.Count; // prefer .Count over .Count()
        if (n == 0) return; // or return;

        // Y range with 20% headroom
        double minYd = values.Min();
        double maxYd = values.Max();
        double range = Mathf.Max(1e-6f, (float)maxYd - (float)minYd); // prevent div by zero
        double margin = 0.2 * range;

        float yMin = (float)(minYd - margin);
        float yMax = (float)(maxYd + margin);
        float yRange = yMax - yMin;

        // Clear previous points/lines if needed

        GameObject lastDot = null;

        // If only 1 point, place it in the middle on X for aesthetics
        float denom = Mathf.Max(1, n - 1);

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0.5f : (float)i / denom; // 0..1 across the width
            float xPos = padLeft + t * contentWidth;

            float v = (float)values[i];
            float yNorm = Mathf.InverseLerp(yMin, yMax, v); // (v - yMin)/yRange
            float yPos = padBottom + yNorm * contentHeight;

            GameObject dot = CreateCircle(new Vector2(xPos, yPos));

            if (lastDot != null)
            {
                CreateLine(
                    lastDot.GetComponent<RectTransform>().anchoredPosition,
                    dot.GetComponent<RectTransform>().anchoredPosition
                );
            }
            lastDot = dot;
        }
    }

    void CreateLine(Vector2 dotA, Vector2 dotB)
    {
        GameObject line = new GameObject("line", typeof(Image));
        line.transform.SetParent(graphImage.rectTransform, false);
        line.GetComponent<Image>().color = new Color(0f, 0f, 1f, 0.5f);

        Vector2 dir = dotB - dotA;
        dir.Normalize();
        float distance = Vector2.Distance(dotA, dotB);

        RectTransform lineRectTransform = line.GetComponent<RectTransform>();
        lineRectTransform.anchorMin = new Vector2(0f, 0f);
        lineRectTransform.anchorMax = new Vector2(0f, 0f);
        lineRectTransform.sizeDelta = new Vector2(distance, 3f);
        lineRectTransform.anchoredPosition = dotA + dir * distance * 0.5f;

        lineRectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        allGraphElements.Add(line);
    }

    public void ClearGraph()
    {
        foreach (GameObject go in allGraphElements)
        {
            Destroy(go);
        }

        allGraphElements.Clear();
    }

}
