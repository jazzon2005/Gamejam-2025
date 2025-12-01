using UnityEngine;
using TMPro; // Librería base

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float lifeTime = 1f;
    public Vector3 offset = new Vector3(0, 1f, 0);

    // OJO: Usamos 'TextMeshPro' (el de mundo), NO 'TextMeshProUGUI' (el de UI)
    private TextMeshPro textMesh;
    private Color textColor;
    private float timer;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(string text, Color color)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
            textColor = color;
            // Forzar orden de renderizado por código si el inspector falla
            textMesh.sortingOrder = 100;
        }
        timer = 0;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
        else
        {
            if (textMesh != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, timer / lifeTime);
                textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            }
        }
    }
}