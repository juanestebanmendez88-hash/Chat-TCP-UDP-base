using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Crea una burbuja de chat por cada mensaje dentro de un Scroll View, estilo WhatsApp:
// mis mensajes se alinean a la derecha, los del otro lado a la izquierda.
public class ChatLogUI : MonoBehaviour
{
    [SerializeField] private RectTransform content;    // Scroll View > Viewport > Content
    [SerializeField] private GameObject bubblePrefab;  // Prefab de la fila (fila > burbuja con Image + TMP_Text)
    [SerializeField] private ScrollRect scrollRect;    // Opcional: para bajar el scroll al ultimo mensaje
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

        // TMP y los Content Size Fitter no calculan su tamano en el mismo frame en
        // que se instancia la burbuja: por eso el primer mensaje sale "cortado" y
        // solo se acomoda cuando llega el siguiente. Forzamos ese reajuste aqui.
        StartCoroutine(RefreshLayout());
    }

    private System.Collections.IEnumerator RefreshLayout()
    {
        yield return null; // esperar un frame a que TMP genere su malla de texto
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
