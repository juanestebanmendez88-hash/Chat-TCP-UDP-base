using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogUI : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Color mineColor = new Color(0.86f, 0.97f, 0.78f);
    [SerializeField] private Color otherColor = Color.white;

    public void AddMessage(string text, bool mine)
    {
        GameObject row = Instantiate(bubblePrefab, content);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.childAlignment = mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft;

        Transform bubble = row.transform.GetChild(0);

        Image background = bubble.GetComponent<Image>();
        if (background != null)
            background.color = mine ? mineColor : otherColor;

        bubble.GetComponentInChildren<TMP_Text>().text = text;

        StartCoroutine(RefreshLayout());
    }

    private System.Collections.IEnumerator RefreshLayout()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
