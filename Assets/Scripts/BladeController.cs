 using UnityEngine;

public class BladeController : MonoBehaviour
{
    private Camera cam;
    private TrailRenderer trail;
    private Rigidbody2D rb;

    void Awake()
    {
        cam = Camera.main;
        trail = GetComponent<TrailRenderer>();
        rb = GetComponent<Rigidbody2D>();
        trail.emitting = false; // start off
    }

     void Update()
{
    if (Input.GetMouseButton(0))
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        rb.position = new Vector2(mousePos.x, mousePos.y);
        Debug.Log("Blade moving to " + rb.position);  // check console
        trail.emitting = true;
    }
    else
    {
        trail.emitting = false;
    }
}
}