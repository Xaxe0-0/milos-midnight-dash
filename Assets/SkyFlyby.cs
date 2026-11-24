using UnityEngine;

public class SkyFlyby : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float drop = 0f;

    private Vector3 startPosition;
    private float flightTime;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        flightTime += Time.deltaTime;

        const float leftEdge = -11f;
        const float flightWidth = 22f;
        float offset = Mathf.Repeat(
            startPosition.x - leftEdge - flightTime * speed, flightWidth);

        float progress = 1f - offset / flightWidth;
        float y = startPosition.y - progress * drop
            + Mathf.Sin(flightTime * 3f) * bobHeight;

        transform.position = new Vector3(leftEdge + offset, y, startPosition.z);
    }
}