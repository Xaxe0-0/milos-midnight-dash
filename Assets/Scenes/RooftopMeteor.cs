using UnityEngine;

internal sealed class RooftopMeteor
{
    private const float FireWidth = 0.95f;
    private const float FireHeight = 0.35f;

    private readonly SpriteRenderer warning;
    private readonly SpriteRenderer meteor;
    private readonly SpriteRenderer fire;
    private readonly float roofTop;
    private float x;
    private float time;
    private float warningTime;

    public bool Active { get; private set; }

    public RooftopMeteor(Transform owner, SpriteRenderer meteorArt, float roofTop)
    {
        this.roofTop = roofTop;
        warning = CreateRenderer(owner, "Meteor Warning", 1);
        meteor = CreateRenderer(owner, "Falling Meteor", 2);
        fire = CreateRenderer(owner, "Meteor Fire", 1);

        warning.sprite = MakeSprite(new[]
        {
            "....YY....",
            "...YYYY...",
            "..Y.YY.Y..",
            ".Y......Y.",
            "YYYYYYYYYY"
        });
        warning.transform.localScale = new Vector3(1.1f, 0.32f, 1f);

        fire.sprite = MakeSprite(new[]
        {
            "....R.....R.....",
            "....RY...RR.....",
            "..R.RYY.RYR..R..",
            "..RRYOYRYYRRRY..",
            ".RYOYOOYOOYOYRY.",
            "RYYYYYYYYYYYYYYR"
        });
        fire.transform.localScale = new Vector3(
            FireWidth / fire.sprite.bounds.size.x,
            FireHeight / fire.sprite.bounds.size.y, 1f);
        meteor.sprite = meteorArt != null ? meteorArt.sprite : fire.sprite;
        float meteorWidth = meteor.sprite.bounds.size.x;
        meteor.transform.localScale = Vector3.one * (1.2f / meteorWidth);
        Reset();
    }

    public void Begin(float targetX, float secondsBeforeImpact)
    {
        x = targetX;
        time = 0f;
        warningTime = secondsBeforeImpact;
        Active = true;
        UpdateVisuals();
    }

    public bool Advance(float step, float deltaTime, float playerX, Bounds hitbox)
    {
        x -= step;
        time += deltaTime;
        if (x < playerX - 2.5f)
        {
            Reset();
            return false;
        }

        UpdateVisuals();
        return time >= warningTime && hitbox.Intersects(new Bounds(
            new Vector3(x, roofTop + FireHeight / 2f, 0f),
            new Vector3(FireWidth - 0.15f, FireHeight, 0.1f)));
    }

    public void Reset()
    {
        Active = false;
        warning.enabled = meteor.enabled = fire.enabled = false;
    }

    public void Dispose()
    {
        DestroyGeneratedSprite(warning.sprite);
        DestroyGeneratedSprite(fire.sprite);
        Object.Destroy(warning.gameObject);
        Object.Destroy(meteor.gameObject);
        Object.Destroy(fire.gameObject);
    }

    private void UpdateVisuals()
    {
        bool landed = time >= warningTime;
        warning.enabled = !landed;
        meteor.enabled = !landed;
        fire.enabled = landed;

        if (landed)
        {
            fire.transform.position = new Vector3(x, roofTop + FireHeight / 2f, 0f);
            return;
        }

        warning.color = new Color(1f, 1f, 1f, 0.65f + 0.35f * Mathf.Sin(time * 28f));
        warning.transform.position = new Vector3(x, roofTop + 0.16f, 0f);
        float remaining = 1f - time / warningTime;
        meteor.transform.position = new Vector3(
            x + 2f * remaining, roofTop + 0.43f + 7f * remaining, 0f);
    }

    private static SpriteRenderer CreateRenderer(Transform owner, string name, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(owner.parent, false);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingLayerID = owner.GetComponent<SpriteRenderer>().sortingLayerID;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite MakeSprite(string[] rows)
    {
        int width = rows[0].Length;
        int height = rows.Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = rows[y][x] switch
                {
                    'Y' => new Color32(255, 222, 102, 255),
                    'O' => new Color32(255, 154, 58, 255),
                    'R' => new Color32(231, 69, 53, 255),
                    _ => Color.clear
                };
                texture.SetPixel(x, height - 1 - y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), width, 0, SpriteMeshType.FullRect);
    }

    private static void DestroyGeneratedSprite(Sprite sprite)
    {
        Object.Destroy(sprite.texture);
        Object.Destroy(sprite);
    }
}
