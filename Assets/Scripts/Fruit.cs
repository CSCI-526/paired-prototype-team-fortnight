using UnityEngine;

public class Fruit : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("Fruit Info")]
    public string fruitName = "Apple"; // ★ identify fruit type

    [Header("Slice Effect")]
    [SerializeField] private Color slicedColor = Color.red;
    [SerializeField] private float shrinkDuration = 0.2f;

    private bool isSliced = false;


    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isSliced) return;
        if (!other.CompareTag("Blade")) return;

        Slice();
    }

    private void Slice()
    {
        isSliced = true;

        // Notify GameManager ★
        GameManager.Instance.OnFruitSliced(this);

        if (sr != null)
        {
            sr.color = slicedColor;
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, shrinkDuration);
    }
}