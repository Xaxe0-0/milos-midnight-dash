using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MiloRunner : MonoBehaviour
{
    [SerializeField] private Transform[] houses = new Transform[2];
    private const float HouseSpacing = 19f;
    [SerializeField] private SpriteRenderer chimney;
    [SerializeField] private Sprite[] obstacleSprites = new Sprite[3];
    private int nextObstacleIndex;

    [UnityEngine.Serialization.FormerlySerializedAs("portalPipe")]
    [SerializeField] private Sprite riftPortal;
    [SerializeField] private Sprite riftSky;
    private const float RiftDuration = 8f;
    private const int RiftReward = 25;
    private Sprite midnightSky;
    private Sprite originalObstacleSprite;
    private Vector3 originalObstacleScale;
    private bool portalVisible;
    private const int PortalTapsRequired = 6;
    private const float PortalChargeSeconds = 3.5f;
    private const float PortalSlowMotionScale = 0.15f;
    private PortalFocusEffect portalFocus;
    private bool portalAttemptUsed;
    private bool portalCharging;
    private int portalTaps;
    private float portalChargeTime;
    private float previousTimeScale;
    private RiftPortalEffects portalEffects;
    private float nextPortalDistance;
    private float riftTimeRemaining;
    private int riftBonus;
    [SerializeField] private Sprite[] walkFrames = new Sprite[4];
    private Sprite standingSprite;
    private Quaternion uprightRotation;
    [SerializeField] private SpriteRenderer background;

    private SpriteRenderer backgroundCopy;
    private Vector3 backgroundStart;
    private float backgroundWidth;
    private float backgroundOffset;

    private const float RoofTop = -2f;
    private const float Gravity = 25f;
    private const float JumpSpeed = 10f;

    private SpriteRenderer milo;
    private Vector3 chimneyStart;
    private float standingY;
    private float verticalSpeed;
    private float distance;
    private bool grounded;
    private bool crashed;
    private RooftopMeteor meteorStrike;
    private float nextMeteorDistance;

    [SerializeField] private Transform Alien;
    private SkyFlyby alienFlyby;
    private Vector3 alienPreviousPosition;
    private bool alienFlybyWasEnabled;

    [SerializeField] private Transform manananggal;

    private const float ChaseWarningDuration = 1.5f;
    private const float ChaseDuration = 26f;
    private const float ChaseSpeedBonus = 2f;

    private SkyFlyby manananggalFlyby;
    private Vector3 manananggalPreviousPosition;
    private bool manananggalFlybyWasEnabled;
    private float chaseTimeRemaining;
    private static readonly int[] ChaseScores = { 20, 60, 100 };
    private int nextChaseIndex;
    private bool chasingManananggal;
    private bool chasingAlien;

    private bool ChaseInProgress => chaseTimeRemaining > 0f;

    private bool ChaseSpeedActive =>
        ChaseInProgress && chaseTimeRemaining <= ChaseDuration;

    private void Start()
    {
        midnightSky = background.sprite;
        originalObstacleSprite = chimney.sprite;
        originalObstacleScale = chimney.transform.localScale;
        backgroundStart = background.transform.position;
        backgroundWidth = background.bounds.size.x;

        backgroundCopy = Instantiate(background, background.transform.parent);
        backgroundCopy.flipX = !background.flipX;
        milo = GetComponent<SpriteRenderer>();
        standingSprite = milo.sprite;
        uprightRotation = transform.localRotation;
        SpriteRenderer meteorArt = GameObject.Find("Meteor")?.GetComponent<SpriteRenderer>();
        meteorStrike = new RooftopMeteor(transform, meteorArt, RoofTop);
        chimneyStart = chimney.transform.position;

        // Align the bottom of Milo's sprite with the top of the roof.
        standingY = transform.position.y + RoofTop - milo.bounds.min.y;
        ResetRun();
    }

    private void Update()
    {
        transform.localRotation = uprightRotation;
        milo.sprite = standingSprite;

        if (grounded)
        {
            Vector3 position = transform.position;
            position.y = standingY;
            transform.position = position;
        }
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

        UpdatePortalCharge();
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

        UpdateChase(dt);
        UpdateRift(dt);

        float speed = Mathf.Min(6f + distance * 0.04f, 14f);
        if (ChaseSpeedActive)
            speed += ChaseSpeedBonus;
        float step = speed * dt;
        distance += step;
        BeginDueChase();
        PositionHouses();
        if (!portalCharging)
            chimney.transform.position += Vector3.left * step;
        backgroundOffset = Mathf.Repeat(
    backgroundOffset + step * 0.2f, backgroundWidth * 2f);
        PositionBackground();

        // A smaller hitbox gives Milo's ears and tail some leeway.
        Bounds hitbox = milo.bounds;
        hitbox.Expand(new Vector3(-0.25f, -0.18f, 0f));

        bool hitMeteor = meteorStrike.Active
            && meteorStrike.Advance(step, dt, transform.position.x, hitbox);
        if ((!portalVisible && hitbox.Intersects(chimney.bounds)) || hitMeteor)
        {
            crashed = true;
            EndPortalCharge();
            return;
        }

        if (chimney.transform.position.x < -10f && !meteorStrike.Active)
        {
            if (riftPortal != null && riftSky != null && riftTimeRemaining <= 0f
                && !portalVisible && distance >= nextPortalDistance)
            {
                ShowPortal();
            }
            else if (!ChaseInProgress && distance >= nextMeteorDistance
                && (nextChaseIndex == ChaseScores.Length
                    || distance + 14f < ChaseScores[nextChaseIndex]))
            {
                const float targetX = 7.5f;
                float arrivalTime = (targetX - transform.position.x) / speed;
                float warningTime = Mathf.Clamp(arrivalTime - 0.35f, 0.35f, 1f);
                meteorStrike.Begin(targetX, warningTime);
                nextMeteorDistance = distance + 20f;
            }
            else
            {
                PlaceObstacle(Random.Range(0f, 4f));
            }
        }
    }

    private void ResetRun()
    {
        Vector3 position = transform.position;
        position.y = standingY;
        transform.position = position;
        nextObstacleIndex = 0;
        nextPortalDistance = 18f;
        riftTimeRemaining = 0f;
        riftBonus = 0;
        SetSky(midnightSky);
        PlaceObstacle(0f);
        backgroundOffset = 0f;
        PositionBackground();
        distance = 0f;
        nextMeteorDistance = 20f;
        meteorStrike.Reset();
        StopChase();
        nextChaseIndex = 0;
        PositionHouses();
        verticalSpeed = 0f;
        grounded = true;
        crashed = false;
    }
    private void PlaceObstacle(float xOffset)
    {
        EndPortalCharge();
        portalVisible = false;
        portalEffects?.Hide();
        PlaceRoofSprite(originalObstacleSprite, originalObstacleScale, xOffset);

        for (int remaining = obstacleSprites.Length; remaining > 0; remaining--)
        {
            Sprite sprite = obstacleSprites[nextObstacleIndex];
            nextObstacleIndex = (nextObstacleIndex + 1) % obstacleSprites.Length;
            if (sprite == null)
                continue;

            PlaceRoofSprite(sprite, Vector3.one, xOffset);
            return;
        }
    }

    private void PlaceRoofSprite(Sprite sprite, Vector3 scale, float xOffset)
    {
        chimney.sprite = sprite;
        chimney.transform.localScale = scale;
        chimney.transform.position = chimneyStart + Vector3.right * xOffset;

        Vector3 position = chimney.transform.position;
        position.y += RoofTop - chimney.bounds.min.y;
        chimney.transform.position = position;
    }

    private void ShowPortal()
    {
        portalVisible = true;
        portalAttemptUsed = false;
        nextPortalDistance = distance + 75f;
        PlaceRoofSprite(riftPortal, Vector3.one, 0f);
        portalEffects ??= new RiftPortalEffects(chimney);
        portalEffects.Show();
    }

    private void UpdatePortalCharge()
    {
        if (!portalCharging)
        {
            float offset = chimney.transform.position.x - transform.position.x;
            if (!portalVisible || portalAttemptUsed || !grounded || Mathf.Abs(offset) > 1.35f)
                return;

            portalAttemptUsed = true;
            portalCharging = true;
            portalTaps = 0;
            portalChargeTime = PortalChargeSeconds;
            previousTimeScale = Time.timeScale;
            Time.timeScale = previousTimeScale * PortalSlowMotionScale;

            portalFocus ??= new PortalFocusEffect();
            portalFocus.Begin();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            portalTaps++;
            if (portalTaps >= PortalTapsRequired)
            {
                EndPortalCharge();
                EnterRift();
                return;
            }
        }

        portalChargeTime -= Time.unscaledDeltaTime;
        if (portalChargeTime <= 0f)
        {
            EndPortalCharge();
            return;
        }

        portalFocus.Pulse(PortalChargeSeconds - portalChargeTime);
    }

    private void EndPortalCharge()
    {
        if (!portalCharging)
            return;

        portalCharging = false;
        Time.timeScale = previousTimeScale;
        portalFocus?.End();
    }

    private void EnterRift()
    {
        riftTimeRemaining = RiftDuration;
        SetSky(riftSky);
        PlaceObstacle(0f);
    }

    private void UpdateRift(float deltaTime)
    {
        if (riftTimeRemaining <= 0f)
            return;

        riftTimeRemaining = Mathf.Max(0f, riftTimeRemaining - deltaTime);
        if (riftTimeRemaining == 0f)
        {
            riftBonus += RiftReward;
            SetSky(midnightSky);
        }
    }

    private void SetSky(Sprite sprite)
    {
        background.sprite = sprite;
        backgroundCopy.sprite = sprite;
        backgroundWidth = background.bounds.size.x;
        backgroundOffset = Mathf.Repeat(backgroundOffset, backgroundWidth * 2f);
        PositionBackground();
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
        if (portalVisible && !crashed)
            portalEffects.Animate(portalCharging ? Time.unscaledDeltaTime : Time.deltaTime);

        if (!grounded)
        {
            // Use the raised-paw walk frame to keep Milo at his walking size.
            if (walkFrames.Length > 3 && walkFrames[3] != null)
                milo.sprite = walkFrames[3];

            // Upward speed is +JumpSpeed at takeoff and about -JumpSpeed at landing.
            float progress = Mathf.Clamp01((1f - verticalSpeed / JumpSpeed) * 0.5f);
            transform.localRotation = uprightRotation
                * Quaternion.Euler(0f, 0f, -360f * progress);
            return;
        }

        if (crashed || walkFrames.Length == 0)
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
    private void OnDisable()
    {
        EndPortalCharge();
    }

    private void OnDestroy()
    {
        StopChase();
        meteorStrike?.Dispose();
        portalEffects?.Dispose();
        portalFocus?.Dispose();
    }


    // manananggal

    private void BeginDueChase()
    {
        int milestone = nextChaseIndex;
        while (nextChaseIndex < ChaseScores.Length
            && distance >= ChaseScores[nextChaseIndex])
        {
            nextChaseIndex++;
        }

        if (nextChaseIndex == milestone)
            return;

        int score = ChaseScores[nextChaseIndex - 1];
        StartChase(score != 100, score != 60);
    }

    private void StartChase(bool useManananggal, bool useAlien)
    {
        StopChase();
        chasingManananggal = useManananggal && manananggal != null;
        chasingAlien = useAlien && Alien != null;
        if (!chasingManananggal && !chasingAlien)
            return;

        if (chasingManananggal)
            StartFlyer(manananggal, out manananggalPreviousPosition,
                out manananggalFlyby, out manananggalFlybyWasEnabled);

        if (chasingAlien)
            StartFlyer(Alien, out alienPreviousPosition,
                out alienFlyby, out alienFlybyWasEnabled);

        chaseTimeRemaining = ChaseWarningDuration + ChaseDuration;
    }

    private static void StartFlyer(Transform flyer, out Vector3 previousPosition,
        out SkyFlyby flyby, out bool flybyWasEnabled)
    {
        previousPosition = flyer.position;
        flyby = flyer.GetComponent<SkyFlyby>();
        flybyWasEnabled = flyby != null && flyby.enabled;
        if (flybyWasEnabled)
            flyby.enabled = false;

        flyer.position = new Vector3(-11f, RoofTop + 1.4f, previousPosition.z);
    }

    private void UpdateChase(float deltaTime)
    {
        if (!ChaseInProgress)
            return;

        if (chaseTimeRemaining <= deltaTime)
        {
            StopChase();
            return;
        }

        chaseTimeRemaining -= deltaTime;
        float elapsed = ChaseWarningDuration + ChaseDuration - chaseTimeRemaining;
        float enter = Mathf.Clamp01(elapsed / ChaseWarningDuration);
        float exit = 1f - Mathf.Clamp01(chaseTimeRemaining / 0.75f);
        float trailingX = transform.position.x - 2.3f;
        float x = Mathf.SmoothStep(-11f, trailingX, enter);
        x = Mathf.SmoothStep(x, -11f, exit);
        float y = RoofTop + 1.4f + Mathf.Sin(elapsed * 8f) * 0.18f;

        if (chasingManananggal && manananggal != null)
            manananggal.position = new Vector3(x, y, manananggalPreviousPosition.z);

        if (chasingAlien && Alien != null)
        {
            float offset = chasingManananggal ? 1.4f * enter : 0f;
            float height = chasingManananggal ? 0.8f * enter : 0f;
            Alien.position = new Vector3(x - offset, y + height, alienPreviousPosition.z);
        }
    }

    private void StopChase()
    {
        chaseTimeRemaining = 0f;

        if (chasingManananggal)
            RestoreFlyer(manananggal, manananggalPreviousPosition,
                manananggalFlyby, manananggalFlybyWasEnabled);

        if (chasingAlien)
            RestoreFlyer(Alien, alienPreviousPosition, alienFlyby, alienFlybyWasEnabled);

        chasingManananggal = false;
        chasingAlien = false;
    }

    private static void RestoreFlyer(Transform flyer, Vector3 previousPosition,
        SkyFlyby flyby, bool flybyWasEnabled)
    {
        if (flyer != null)
            flyer.position = previousPosition;

        if (flybyWasEnabled && flyby != null)
            flyby.enabled = true;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(12, 12, 160, 30), $"Score  {(int)distance}");
        if (riftBonus > 0)
            GUI.Box(new Rect(12, 46, 160, 30), $"Rift bonus  +{riftBonus}");

        if (!crashed && riftTimeRemaining > 0f)
        {
            GUI.Box(new Rect(Screen.width - 252f, 12, 240, 30),
                $"Violet Rift  {Mathf.CeilToInt(riftTimeRemaining)}s");
        }
        else if (portalCharging)
        {
            DrawPortalCharge();
        }
        else if (!crashed && portalVisible && !portalAttemptUsed)
        {
            GUI.Box(new Rect(Screen.width - 252f, 12, 240, 30), "Portal ahead");
        }

        if (crashed)
        {
            GUI.Box(
                new Rect(Screen.width / 2f - 135f, 55f, 270f, 45f),
                "Game over! Press Space to retry.");
        }
        if (ChaseInProgress && !crashed)
        {
            string flyerName = chasingManananggal && chasingAlien
                ? "Manananggal and alien"
                : chasingManananggal ? "Manananggal" : "Alien";
            string message = ChaseSpeedActive
                ? $"{flyerName} chase!"
                : $"{flyerName} incoming!";

            GUI.Box(new Rect(Screen.width / 2f - 120f, 12f, 240f, 30f), message);
        }
    }
    private void DrawPortalCharge()
    {
        float width = Mathf.Min(280f, Screen.width - 32f);
        float left = (Screen.width - width) / 2f;
        float top = Screen.height * 0.26f;
        GUI.Box(new Rect(left, top, width, 92f), "Tap E repeatedly to open the portal");
        GUI.Label(new Rect(left + 12f, top + 64f, width - 24f, 22f),
            $"{portalTaps} / {PortalTapsRequired} taps     {portalChargeTime:0.0}s left");

        float progress = portalTaps / (float)PortalTapsRequired;
        Rect fill = new Rect(left + 12f, top + 35f, width - 24f, 20f);
        Color previousColor = GUI.color;
        GUI.color = new Color(0.15f, 0.10f, 0.22f, 0.94f);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        if (progress > 0f)
        {
            GUI.color = new Color(0.42f, 0.95f, 1f);
            fill.width *= progress;
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
        }

        GUI.color = previousColor;
    }

}

internal sealed class RiftPortalEffects
{
    private const int SparkCount = 16;
    private readonly SpriteRenderer portal;
    private readonly GameObject sparkRoot;
    private readonly SpriteRenderer[] sparks = new SpriteRenderer[SparkCount];
    private readonly Sprite sparkSprite;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Color originalColor;
    private float elapsed;

    public RiftPortalEffects(SpriteRenderer portal)
    {
        this.portal = portal;
        sparkRoot = new GameObject("Violet Rift Sparks");
        sparkSprite = CreateSparkSprite();

        for (int i = 0; i < sparks.Length; i++)
        {
            var spark = new GameObject($"Spark {i + 1}");
            spark.transform.SetParent(sparkRoot.transform, false);
            var renderer = spark.AddComponent<SpriteRenderer>();
            renderer.sprite = sparkSprite;
            renderer.sortingLayerID = portal.sortingLayerID;
            renderer.sortingOrder = portal.sortingOrder + 1;
            sparks[i] = renderer;
        }

        sparkRoot.SetActive(false);
    }

    public void Show()
    {
        originalScale = portal.transform.localScale;
        originalRotation = portal.transform.localRotation;
        originalColor = portal.color;
        elapsed = 0f;
        sparkRoot.SetActive(true);
        Animate(0f);
    }

    public void Animate(float deltaTime)
    {
        elapsed += deltaTime;
        float pulse = Mathf.Sin(elapsed * 5f);
        portal.transform.localScale = Vector3.Scale(originalScale,
            new Vector3(1f + 0.09f * pulse, 1f - 0.04f * pulse, 1f));
        portal.transform.localRotation = originalRotation *
            Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 2.8f) * 5f);
        portal.color = Color.Lerp(originalColor * new Color(0.74f, 0.8f, 1f),
            originalColor, (pulse + 1f) * 0.5f);

        Bounds bounds = portal.bounds;
        sparkRoot.transform.position = bounds.center;

        for (int i = 0; i < sparks.Length; i++)
        {
            float life = Mathf.Repeat(elapsed * 0.72f + i / (float)SparkCount, 1f);
            float angle = elapsed * 2.5f + i * 2.39996f;
            float radius = 0.78f + life * 0.6f;
            SpriteRenderer spark = sparks[i];

            spark.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * bounds.extents.x * radius,
                Mathf.Sin(angle) * bounds.extents.y * radius + life * 0.18f,
                0f);
            float brightness = Mathf.Sin(life * Mathf.PI);
            spark.transform.localScale = Vector3.one * (0.65f + brightness * 0.55f);
            spark.color = i % 2 == 0
                ? new Color(0.25f, 1f, 0.95f, brightness)
                : new Color(0.9f, 0.5f, 1f, brightness);
        }
    }

    public void Hide()
    {
        if (!sparkRoot.activeSelf)
            return;

        sparkRoot.SetActive(false);
        if (portal != null)
        {
            portal.transform.localScale = originalScale;
            portal.transform.localRotation = originalRotation;
            portal.color = originalColor;
        }
    }

    public void Dispose()
    {
        Hide();
        Object.Destroy(sparkRoot);
        Object.Destroy(sparkSprite.texture);
        Object.Destroy(sparkSprite);
    }

    private static Sprite CreateSparkSprite()
    {
        var texture = new Texture2D(5, 5, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[25];
        pixels[12] = new Color32(255, 255, 255, 255);
        pixels[7] = pixels[11] = pixels[13] = pixels[17] =
            new Color32(255, 255, 255, 190);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f),
            48f, 0, SpriteMeshType.FullRect);
    }
}

internal sealed class PortalFocusEffect
{
    private readonly Volume volume;
    private readonly VolumeProfile profile;
    private readonly ColorAdjustments color;
    private UniversalAdditionalCameraData cameraData;
    private bool previousPostProcessing;

    public PortalFocusEffect()
    {
        var volumeObject = new GameObject("Portal Slow Motion Volume");
        volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        color = profile.Add<ColorAdjustments>(false);
        color.saturation.Override(-100f);
        color.postExposure.Override(0f);
    }

    public void Begin()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            cameraData = camera.GetUniversalAdditionalCameraData();
            previousPostProcessing = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
            // Put the runtime volume on a layer the game camera evaluates.
            int mask = cameraData.volumeLayerMask.value;
            for (int layer = 0; layer < 32; layer++)
            {
                if ((mask & (1 << layer)) == 0)
                    continue;

                volume.gameObject.layer = layer;
                break;
            }
        }

        volume.weight = 1f;
        Pulse(0f);
    }

    public void Pulse(float seconds)
    {
        color.postExposure.value = -0.12f + Mathf.Sin(seconds * 5f) * 0.16f;
    }

    public void End()
    {
        volume.weight = 0f;
        if (cameraData != null)
            cameraData.renderPostProcessing = previousPostProcessing;

        cameraData = null;
    }

    public void Dispose()
    {
        End();
        Object.Destroy(volume.gameObject);
        Object.Destroy(color);
        Object.Destroy(profile);
    }
}
