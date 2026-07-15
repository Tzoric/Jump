using UnityEngine;

public sealed class MinerOutfitVisual : MonoBehaviour
{
    public enum Perspective
    {
        Side,
        TowardCamera,
        AwayFromCamera
    }

    [SerializeField] private SpriteRenderer directionSource;
    [SerializeField] private SpriteRenderer minerBody;
    [SerializeField] private Transform handPickaxe;
    [SerializeField] private CharacterOutfitDefinition outfit;
    [SerializeField, Min(1f)] private float walkFramesPerSecond = 8f;
    [SerializeField, Min(1f)] private float runFramesPerSecond = 12f;

    private Rigidbody2D physicsBody;
    private HeroMovement movement;
    private SpriteRenderer pickRenderer;
    private Vector3 bodyRestPosition;
    private Vector3 pickRestPosition;
    private readonly Sprite[,] frames = new Sprite[5, 6];
    private bool framesBuilt;
    private bool perspectiveOverride;
    private Perspective forcedPerspective;
    private bool forcedWalking;
    private float animationClock;
    private float landingUntil;
    private bool wasAirborne;
    private int lastAnimationState = -1;

    public SpriteRenderer VisualRenderer => minerBody;
    public Transform HandPickaxe => handPickaxe;
    public CharacterOutfitDefinition Outfit => outfit;
    public Perspective CurrentPerspective => perspectiveOverride ? forcedPerspective : Perspective.Side;
    public int CurrentAnimationRow { get; private set; } = -1;
    public int CurrentAnimationFrame { get; private set; } = -1;

    public void Configure(SpriteRenderer facingSource, SpriteRenderer bodyRenderer, Transform pickaxeTransform,
        CharacterOutfitDefinition outfitDefinition)
    {
        directionSource = facingSource;
        minerBody = bodyRenderer;
        handPickaxe = pickaxeTransform;
        ApplyOutfit(outfitDefinition);
        CacheRestPose();
    }

    public void ApplyOutfit(CharacterOutfitDefinition definition)
    {
        outfit = definition;
        ClearRuntimeFrames();
        CachePickRenderer();
        if (pickRenderer != null && outfit != null) pickRenderer.sprite = outfit.HandTool;
        if (Application.isPlaying) BuildFrames();
    }

    public void PlayWalkAway(bool walking = true)
    {
        perspectiveOverride = true;
        forcedPerspective = Perspective.AwayFromCamera;
        forcedWalking = walking;
        animationClock = 0f;
    }

    public void PlayWalkTowardCamera(bool walking = true)
    {
        perspectiveOverride = true;
        forcedPerspective = Perspective.TowardCamera;
        forcedWalking = walking;
        animationClock = 0f;
    }

    public void ClearPerspectiveOverride()
    {
        perspectiveOverride = false;
        forcedWalking = false;
        animationClock = 0f;
    }

    private void Awake()
    {
        physicsBody = GetComponent<Rigidbody2D>();
        movement = GetComponent<HeroMovement>();
        CacheRestPose();
        CachePickRenderer();
        BuildFrames();
    }

    private void CacheRestPose()
    {
        if (minerBody != null) bodyRestPosition = minerBody.transform.localPosition;
        if (handPickaxe != null) pickRestPosition = handPickaxe.localPosition;
    }

    private void CachePickRenderer()
    {
        pickRenderer = handPickaxe == null ? null : handPickaxe.GetComponentInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        if (directionSource == null || minerBody == null) return;

        if (!framesBuilt) BuildFrames();
        float direction = directionSource.flipX ? -1f : 1f;
        float horizontalSpeed = physicsBody == null ? 0f : Mathf.Abs(physicsBody.linearVelocityX);
        float walkCycle = Time.time * Mathf.Lerp(4f, 10f, Mathf.InverseLerp(0f, 7.5f, horizontalSpeed));
        float bob = !framesBuilt && horizontalSpeed > .15f ? Mathf.Abs(Mathf.Sin(walkCycle)) * .025f : 0f;

        Perspective perspective = perspectiveOverride ? forcedPerspective : Perspective.Side;
        minerBody.flipX = perspective == Perspective.Side && direction < 0f;
        minerBody.transform.localPosition = bodyRestPosition + Vector3.up * bob;

        UpdateAnimation(perspective, horizontalSpeed);

        if (handPickaxe != null)
        {
            bool showSideTool = perspective == Perspective.Side;
            if (pickRenderer != null) pickRenderer.enabled = showSideTool;
            if (!showSideTool) return;

            bool preparingJump = movement != null && movement.IsPreparingJump;
            bool airbornePose = movement != null &&
                (!movement.IsGrounded || (physicsBody != null && physicsBody.linearVelocityY > .5f));
            float swing = preparingJump ? -24f : airbornePose ? -4f :
                horizontalSpeed > .15f ? Mathf.Sin(walkCycle) * 12f : -8f;
            float pickVerticalOffset = preparingJump ? -.045f : airbornePose ? .015f : bob;
            handPickaxe.localPosition = new Vector3(Mathf.Abs(pickRestPosition.x) * direction,
                pickRestPosition.y + pickVerticalOffset, pickRestPosition.z);
            handPickaxe.localRotation = Quaternion.Euler(0f, 0f, swing * direction);
            handPickaxe.localScale = new Vector3(direction, 1f, 1f);
        }
    }

    private void UpdateAnimation(Perspective perspective, float horizontalSpeed)
    {
        if (!framesBuilt) return;

        int row;
        int frame;
        float fps;
        int state;

        if (perspective == Perspective.TowardCamera || perspective == Perspective.AwayFromCamera)
        {
            row = perspective == Perspective.TowardCamera ? 3 : 4;
            fps = walkFramesPerSecond;
            frame = forcedWalking ? AnimatedFrame(fps) : 0;
            state = 20 + row + (forcedWalking ? 10 : 0);
        }
        else
        {
            bool grounded = movement != null && movement.IsGrounded;
            bool preparingJump = movement != null && movement.IsPreparingJump;
            float velocityY = physicsBody == null ? 0f : physicsBody.linearVelocityY;
            bool launching = velocityY > .5f && !preparingJump;
            bool airborne = !grounded || launching;
            if (wasAirborne && grounded && !launching) landingUntil = Time.time + .12f;
            wasAirborne = airborne;
            bool landing = !airborne && Time.time < landingUntil;

            if (preparingJump)
            {
                row = 2;
                frame = 1;
                fps = walkFramesPerSecond;
                state = 90;
            }
            else if (airborne || landing)
            {
                row = 2;
                frame = landing ? 5 : velocityY > 1f ? 2 : velocityY > -.75f ? 3 : 4;
                fps = walkFramesPerSecond;
                state = 100 + frame;
            }
            else if (horizontalSpeed > 6.25f)
            {
                row = 1;
                fps = runFramesPerSecond;
                frame = AnimatedFrame(fps);
                state = 2;
            }
            else if (horizontalSpeed > .15f)
            {
                row = 0;
                fps = walkFramesPerSecond;
                frame = AnimatedFrame(fps);
                state = 1;
            }
            else
            {
                row = 2;
                frame = 0;
                fps = walkFramesPerSecond;
                state = 0;
            }
        }

        CurrentAnimationRow = row;
        CurrentAnimationFrame = frame;

        if (state != lastAnimationState)
        {
            animationClock = 0f;
            lastAnimationState = state;
            if (row < 2 || row > 2) frame = forcedWalking || horizontalSpeed > .15f ? 0 : frame;
        }

        Sprite next = frames[row, Mathf.Clamp(frame, 0, 5)];
        if (next != null) minerBody.sprite = next;
    }

    private int AnimatedFrame(float fps)
    {
        animationClock += Time.deltaTime * fps;
        return Mathf.FloorToInt(animationClock) % 6;
    }

    private void BuildFrames()
    {
        Sprite sheet = outfit == null ? null : outfit.AnimationSheet;
        if (sheet == null || !sheet.texture.isReadable) return;

        Texture2D texture = sheet.texture;
        for (int row = 0; row < 5; row++)
        {
            int top = Mathf.RoundToInt(row * texture.height / 5f);
            int bottom = Mathf.RoundToInt((row + 1) * texture.height / 5f);
            int y = texture.height - bottom;
            int height = bottom - top;
            for (int column = 0; column < 6; column++)
            {
                int x0 = Mathf.RoundToInt(column * texture.width / 6f);
                int x1 = Mathf.RoundToInt((column + 1) * texture.width / 6f);
                Sprite frame = Sprite.Create(texture, new Rect(x0, y, x1 - x0, height),
                    new Vector2(.5f, .5f), sheet.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                frame.name = $"{outfit.OutfitId}_{row}_{column}";
                frames[row, column] = frame;
            }
        }
        framesBuilt = true;
    }

    private void ClearRuntimeFrames()
    {
        for (int row = 0; row < 5; row++)
        for (int column = 0; column < 6; column++)
        {
            if (frames[row, column] != null && Application.isPlaying) Destroy(frames[row, column]);
            frames[row, column] = null;
        }
        framesBuilt = false;
    }

    private void OnDestroy()
    {
        ClearRuntimeFrames();
    }
}
