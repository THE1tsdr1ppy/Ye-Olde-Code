using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform planet;       // The object to orbit around
    public float orbitSpeed = 10f; // Degrees per second
    public float orbitRadius = 5f; // Distance from the planet

    private float angle;

    void Start()
    {
        if (planet == null)
        {
            Debug.LogError("Planet not assigned.");
            enabled = false;
            return;
        }

        // Initialize position on orbit circle
        angle = 0f;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
        transform.position = planet.position + offset;
    }

    void Update()
    {
        angle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

        // Keep angle between 0 and 2*PI
        if (angle > Mathf.PI * 2f) angle -= Mathf.PI * 2f;

        // Compute new position
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
        transform.position = planet.position + offset;

        // Optional: Look at the planet
        transform.LookAt(planet);
    }
}
