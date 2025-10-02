using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FruitGuideAnimator : MonoBehaviour
{
    [Header("Rows to animate (assign FruitEntry_* objects)")]
    public RectTransform[] fruitRows;

    [Header("Parent layout objects")]
    public VerticalLayoutGroup listLayout;           // drag FruitList here
    public ContentSizeFitter sizeFitter;             // optional: drag if you have one

    [Header("Animation")]
    public float fallDistance = 150f;
    public float duration = 0.5f;
    public float stagger = 0.1f;                     // 0 = all together

    public void ResetRows()
    {
        // Re-enable layout so rows snap back into place
        if (listLayout) listLayout.enabled = true;
        if (sizeFitter) sizeFitter.enabled = true;

        foreach (var row in fruitRows)
        {
            if (!row) continue;

            // Stop ignoring layout so VerticalLayoutGroup can position rows
            var le = row.GetComponent<LayoutElement>();
            if (le) le.ignoreLayout = false;

            // Reset position & alpha
            row.anchoredPosition = Vector2.zero;

            if (row.TryGetComponent<CanvasGroup>(out var cg))
                cg.alpha = 1f;
        }

        // Force layout rebuild so everything is visible in the right place
        if (listLayout)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listLayout.GetComponent<RectTransform>());
    }
    public IEnumerator AnimateRows()
    {
        if (listLayout) listLayout.enabled = false;
        if (sizeFitter) sizeFitter.enabled = false;

        // make rows free to move and ensure CanvasGroup exists
        foreach (var row in fruitRows)
        {
            if (!row) continue;

            // ensure CanvasGroup
            if (!row.TryGetComponent<CanvasGroup>(out var cg))
                cg = row.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            // ignore layout so VerticalLayoutGroup doesn't snap them back
            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        // start animations (staggered)
        for (int i = 0; i < fruitRows.Length; i++)
        {
            StartCoroutine(AnimateSingle(fruitRows[i]));
            if (stagger > 0f) yield return new WaitForSeconds(stagger);
        }

        // wait until the last one finishes
        float total = duration + Mathf.Max(0, (fruitRows.Length - 1)) * Mathf.Max(0, stagger);
        yield return new WaitForSeconds(total);
    }

    private IEnumerator AnimateSingle(RectTransform row)
    {
        if (!row) yield break;

        if (!row.TryGetComponent<CanvasGroup>(out var cg))
            cg = row.gameObject.AddComponent<CanvasGroup>();

        Vector2 start = row.anchoredPosition;
        Vector2 end   = start - new Vector2(0f, fallDistance);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            row.anchoredPosition = Vector2.Lerp(start, end, p);
            cg.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        row.anchoredPosition = end;
        cg.alpha = 0f;
    }
}