using UnityEngine;

public class MiloRunner : MonoBehaviour
{
    [SerializeField] private Transform[] houses = new Transform[2];
    private const float HouseSpacing = 19f;
    [SerializeField] private SpriteRenderer chimney;
    [SerializeField] private Sprite[] walkFrames = new Sprite[4];
    private Sprite standingSprite;
    [SerializeField] private SpriteRenderer background;

    private SpriteRenderer backgroundCopy;
    private Vector3 backgroundStart;
    private float backgroundWidth;
    private float backgroundOffset;

    private const float RoofTop = -2f;
    private const float Gravity = 24f;
    private const float JumpSpeed = 9f;

    private SpriteRenderer milo;
    private Vector3 chimneyStart;
    private float standingY;
    private float verticalSpeed;
    private float distance;
    private bool grounded;
    private bool crashed;

    private void Start()
    {
        backgroundStart = background.transform.position;
        backgroundWidth = background.bounds.size.x;

        backgroundCopy = Instantiate(background, background.transform.parent);
        backgroundCopy.flipX = !background.flipX;
        milo = GetComponent<SpriteRenderer>();
        standingSprite = milo.sprite;
        chimneyStart = chimney.transform.position;

        // Align the bottom of Milo's sprite with the top of the roof.
        standingY = transform.position.y + RoofTop - milo.bounds.min.y;
        ResetRun();
    }

    private void Update()
    {
        milo.sprite = standingSprite;

        if (grounded)
        {
            Vector3 position = transform.position;
            position.y = standingY;
            transform.position = position;
        }
        milo.sprite = standingSprite;
        bool pressedSpace = Input.GetKeyDown(KeyCode.Space);

        if (crashed)
        {
            if (pressedSpace)
                ResetRun();

            return;
        }

        if (pressedSpace && grounded)
        {
            verticalSpeed = JumpSpeed;
            grounded = false;
        }

        float dt = Time.deltaTime;
        if (!grounded)
        {
            verticalSpeed -= Gravity * dt;
            Vector3 position = transform.position;
            position.y += verticalSpeed * dt;

            if (position.y <= standingY)
            {
                position.y = standingY;
                verticalSpeed = 0f;
                grounded = true;
            }

            transform.position = position;
        }

        float speed = Mathf.Min(6f + distance * 0.04f, 14f);
        float step = speed * dt;
        PositionHouses();
        distance += step;
        chimney.transform.position += Vector3.left * step;
        backgroundOffset = Mathf.Repeat(
    backgroundOffset + step * 0.2f, backgroundWidth * 2f);
        PositionBackground();

        // A smaller hitbox gives Milo's ears and tail some leeway.
        Bounds hitbox = milo.bounds;
        hitbox.Expand(new Vector3(-0.25f, -0.18f, 0f));

        if (hitbox.Intersects(chimney.bounds))
        {
            crashed = true;
            return;
        }

        if (chimney.transform.position.x < -10f)
        {
            chimney.transform.position =
                chimneyStart + Vector3.right * Random.Range(0f, 4f);
        }
    }

    private void ResetRun()
    {
        Vector3 position = transform.position;
        position.y = standingY;
        transform.position = position;
        chimney.transform.position = chimneyStart;
        backgroundOffset = 0f;
        PositionBackground();
        distance = 0f;
        PositionHouses();
        verticalSpeed = 0f;
        grounded = true;
        crashed = false;
    }
    private void PositionBackground()
    {
        float firstX = backgroundOffset < backgroundWidth
            ? -backgroundOffset
            : backgroundWidth * 2f - backgroundOffset;

        background.transform.position = backgroundStart + Vector3.right * firstX;
        backgroundCopy.transform.position =
            backgroundStart + Vector3.right * (backgroundWidth - backgroundOffset);
    }
    private void LateUpdate()
    {
        if (!grounded || crashed || walkFrames.Length == 0)
            return;

        int frame = Mathf.FloorToInt(distance / 0.7f) % walkFrames.Length;
        Sprite walkingSprite = walkFrames[frame];

        if (walkingSprite == null)
            return;

        milo.sprite = walkingSprite;

        Vector3 position = transform.position;
        position.y += RoofTop - milo.bounds.min.y - 0.13f;
        transform.position = position;
    }
    private void PositionHouses()
    {
        float loopWidth = houses.Length * HouseSpacing;

        for (int i = 0; i < houses.Length; i++)
        {
            if (houses[i] == null)
                continue;

            Vector3 position = houses[i].position;
            float offset = distance + HouseSpacing - i * HouseSpacing;
            position.x = HouseSpacing - Mathf.Repeat(offset, loopWidth);
            houses[i].position = position;
        }
    }
    private void OnGUI()
    {
        GUI.Box(new Rect(12, 12, 160, 30), $"Score  {(int)distance}");

        if (crashed)
        {
            GUI.Box(
                new Rect(Screen.width / 2f - 135f, 55f, 270f, 45f),
                "Game over! Press Space to retry.");
        }
    }
}