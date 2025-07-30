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

        return circle;
    }

    void ShowGraph(List<double> values)
    {
        float padLeft = 35f;
        float padBottom = 30f;

        float graphHeight = graphImage.rectTransform.sizeDelta.y - padBottom;
        float xSize = 20f;
        float yMax = (float)values.Max() + (float)values.Max() * 0.2f;

        GameObject lastDot = null;
        for(int i =0; i < values.Count; i++)
        {
            float xPos = padLeft + (i * xSize);
            float yPos = (((float)values[i] / yMax) * graphHeight) + padBottom;

            GameObject createdDot = CreateCircle(new Vector2(xPos, yPos));
            if (lastDot != null)
            {
                CreateLine(lastDot.GetComponent<RectTransform>().anchoredPosition, createdDot.GetComponent<RectTransform>().anchoredPosition);
            }
            lastDot = createdDot;
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
    }

}
