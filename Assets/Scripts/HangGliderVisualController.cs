using UnityEngine;

public enum HangGliderVisualState
{
    Stowed,
    Hover,
    Float,
    Dive,
    GlideLeft,
    GlideRight
}

/// <summary>
/// Presents the hang glider as a small state machine instead of rotating one
/// front-facing picture for every movement direction. The front view is used
/// only while hovering; all travelling states use directional side art.
/// </summary>
[DefaultExecutionOrder(150)]
[DisallowMultipleComponent]
public sealed class HangGliderVisualController : MonoBehaviour
{
    private const int ProceduralFlapFrameCount = 4;

    [SerializeField] private ParachuteDescentController flightController;
    [SerializeField] private HeroMovement movement;
    [SerializeField] private MinerOutfitVisual minerVisual;
    [SerializeField] private SpriteRenderer gliderRenderer;

    [Header("Directional Art")]
    [SerializeField] private Sprite hoverFrontSprite;
    [SerializeField] private Sprite floatRightSprite;
    [SerializeField] private Sprite diveRightSprite;
    [SerializeField] private Sprite bankRightSprite;

    [Header("Grip Anchors (normalized sprite rect)")]
    [Tooltip("Center of the wrapped control bar, measured from the sprite rect's bottom-left corner.")]
    [SerializeField] private Vector2 hoverGripAnchor = new(.4992f, .0556f);
    [SerializeField] private Vector2 floatGripAnchor = new(.7106f, .0552f);
    [SerializeField] private Vector2 diveGripAnchor = new(.7125f, .0477f);
    [SerializeField] private Vector2 bankGripAnchor = new(.6693f, .0648f);

    [Header("Wing Flex")]
    [SerializeField, Min(.1f)] private float hoverFlapFramesPerSecond = 5.5f;
    [SerializeField, Min(.1f)] private float floatFlapFramesPerSecond = 3.5f;
    [SerializeField, Min(.1f)] private float bankFlapFramesPerSecond = 5f;
    [SerializeField, Min(.1f)] private float diveFlapFramesPerSecond = 7f;
    [SerializeField, Range(0f, .08f)] private float hoverFlex = .035f;
    [SerializeField, Range(0f, .08f)] private float floatFlex = .024f;
    [SerializeField, Range(0f, .08f)] private float bankFlex = .03f;
    [SerializeField, Range(0f, .08f)] private float diveFlex = .012f;
    [SerializeField, Range(.05f, 1f)] private float directionDeadZone = .22f;

    private Rigidbody2D body;
    private Vector3 restPosition;
    private Vector3 restScale = Vector3.one;
    private Quaternion restRotation = Quaternion.identity;
    private Vector3 restGripPosition;
    private bool restPoseCached;
    private bool facingLeft;
    private float animationClock;

    public HangGliderVisualState CurrentState { get; private set; } = HangGliderVisualState.Stowed;
    public int CurrentFrame { get; private set; }
    public float WingFlexAmount { get; private set; }
    public bool IsFacingLeft => facingLeft;
    public SpriteRenderer GliderRenderer => gliderRenderer;
    public Sprite HoverFrontSprite => hoverFrontSprite;
    public Sprite FloatRightSprite => floatRightSprite;
    public Sprite DiveRightSprite => diveRightSprite;
    public Sprite BankRightSprite => bankRightSprite;
    public Vector2 CurrentGripPosition { get; private set; }
    public Vector2 RestGripPosition => restGripPosition;
    public float GripAnchorError { get; private set; }
    public bool HasAlignedGripAnchor => CurrentState == HangGliderVisualState.Stowed || GripAnchorError <= .002f;
    public bool HasCompleteDirectionalArt => hoverFrontSprite != null && floatRightSprite != null &&
                                              diveRightSprite != null && bankRightSprite != null;

    public void Configure(ParachuteDescentController controller, HeroMovement heroMovement,
        MinerOutfitVisual outfitVisual, SpriteRenderer renderer, Sprite hoverFront,
        Sprite floatRight, Sprite diveRight, Sprite bankRight)
    {
        flightController = controller;
        movement = heroMovement;
        minerVisual = outfitVisual;
        gliderRenderer = renderer;
        hoverFrontSprite = hoverFront;
        floatRightSprite = floatRight;
        diveRightSprite = diveRight;
        bankRightSprite = bankRight;
        body = GetComponent<Rigidbody2D>();
        CacheRestPose(true);
        ApplyStowedPose();
    }

    private void Awake()
    {
        flightController ??= GetComponent<ParachuteDescentController>();
        movement ??= GetComponent<HeroMovement>();
        minerVisual ??= GetComponent<MinerOutfitVisual>();
        body = GetComponent<Rigidbody2D>();
        CacheRestPose(false);
        ApplyStowedPose();
    }

    private void Update()
    {
        if (flightController == null || !flightController.IsDeployed || gliderRenderer == null)
        {
            ApplyStowedPose();
            return;
        }

        UpdateFacing();
        HangGliderVisualState nextState = ResolveState();
        if (nextState != CurrentState)
        {
            CurrentState = nextState;
            animationClock = 0f;
            CurrentFrame = 0;
        }

        gliderRenderer.enabled = true;
        gliderRenderer.sprite = SpriteForState(CurrentState);
        gliderRenderer.flipX = CurrentState != HangGliderVisualState.Hover && facingLeft;
        ApplyMinerFlightPose();
    }

    private void LateUpdate()
    {
        if (CurrentState == HangGliderVisualState.Stowed || gliderRenderer == null) return;

        float framesPerSecond = FramesPerSecondForState(CurrentState);
        float flex = FlexForState(CurrentState);
        animationClock += Time.deltaTime * framesPerSecond;
        CurrentFrame = Mathf.FloorToInt(animationClock) % ProceduralFlapFrameCount;
        float cycle = animationClock / ProceduralFlapFrameCount;
        float wave = Mathf.Sin(cycle * Mathf.PI * 2f);
        WingFlexAmount = wave * flex;

        float spriteScale = NormalizedSpriteScale(gliderRenderer.sprite);
        Vector3 scale = restScale * spriteScale;
        // Flex around the wrapped control bar. The bar stays locked to the
        // miner's hands while the upper wing breathes vertically.
        scale.y *= 1f + WingFlexAmount;

        float bank = CurrentState switch
        {
            HangGliderVisualState.GlideLeft => 4.5f,
            HangGliderVisualState.GlideRight => -4.5f,
            _ => 0f
        };
        float flutterRotation = CurrentState == HangGliderVisualState.Dive ? .35f : .85f;
        Quaternion rotation = restRotation * Quaternion.Euler(0f, 0f, bank + wave * flutterRotation);
        gliderRenderer.transform.localScale = scale;
        gliderRenderer.transform.localRotation = rotation;

        Vector2 grip = GripAnchorForState(CurrentState);
        Vector3 gripOffset = GripOffsetInParent(gliderRenderer.sprite, grip,
            gliderRenderer.flipX, scale, rotation);
        gliderRenderer.transform.localPosition = restGripPosition - gripOffset;
        CurrentGripPosition = gliderRenderer.transform.localPosition + gripOffset;
        GripAnchorError = Vector2.Distance(CurrentGripPosition, restGripPosition);
    }

    private HangGliderVisualState ResolveState()
    {
        float vertical = flightController.GliderVerticalInput;
        if (vertical > flightController.VerticalInputDeadZone) return HangGliderVisualState.Hover;
        if (vertical < -flightController.VerticalInputDeadZone) return HangGliderVisualState.Dive;

        float horizontal = movement == null ? 0f : movement.HorizontalInput;
        if (Mathf.Abs(horizontal) <= directionDeadZone && body != null)
            horizontal = body.linearVelocityX / Mathf.Max(.01f, flightController.MaximumHorizontalSpeed);
        if (horizontal < -directionDeadZone) return HangGliderVisualState.GlideLeft;
        if (horizontal > directionDeadZone) return HangGliderVisualState.GlideRight;
        return HangGliderVisualState.Float;
    }

    private void UpdateFacing()
    {
        float horizontal = movement == null ? 0f : movement.HorizontalInput;
        if (Mathf.Abs(horizontal) <= directionDeadZone && body != null)
            horizontal = body.linearVelocityX;
        if (horizontal < -directionDeadZone) facingLeft = true;
        else if (horizontal > directionDeadZone) facingLeft = false;
    }

    private void ApplyMinerFlightPose()
    {
        if (minerVisual == null) return;
        if (CurrentState == HangGliderVisualState.Hover)
        {
            minerVisual.SetFlightPose(MinerOutfitVisual.Perspective.TowardCamera, 0, false);
            return;
        }

        int airborneFrame = CurrentState == HangGliderVisualState.Dive ? 4 : 3;
        minerVisual.SetFlightPose(MinerOutfitVisual.Perspective.Side, airborneFrame, facingLeft);
    }

    private Sprite SpriteForState(HangGliderVisualState state)
    {
        Sprite selected = state switch
        {
            HangGliderVisualState.Hover => hoverFrontSprite,
            HangGliderVisualState.Dive => diveRightSprite,
            HangGliderVisualState.GlideLeft => bankRightSprite,
            HangGliderVisualState.GlideRight => bankRightSprite,
            HangGliderVisualState.Float => floatRightSprite,
            _ => hoverFrontSprite
        };
        return selected != null ? selected : hoverFrontSprite;
    }

    private float FramesPerSecondForState(HangGliderVisualState state)
    {
        return state switch
        {
            HangGliderVisualState.Hover => hoverFlapFramesPerSecond,
            HangGliderVisualState.Dive => diveFlapFramesPerSecond,
            HangGliderVisualState.GlideLeft => bankFlapFramesPerSecond,
            HangGliderVisualState.GlideRight => bankFlapFramesPerSecond,
            _ => floatFlapFramesPerSecond
        };
    }

    private float FlexForState(HangGliderVisualState state)
    {
        return state switch
        {
            HangGliderVisualState.Hover => hoverFlex,
            HangGliderVisualState.Dive => diveFlex,
            HangGliderVisualState.GlideLeft => bankFlex,
            HangGliderVisualState.GlideRight => bankFlex,
            _ => floatFlex
        };
    }

    private float NormalizedSpriteScale(Sprite activeSprite)
    {
        if (hoverFrontSprite == null || activeSprite == null) return 1f;
        return hoverFrontSprite.bounds.size.x /
               Mathf.Max(.001f, activeSprite.bounds.size.x);
    }

    private Vector2 GripAnchorForState(HangGliderVisualState state)
    {
        return state switch
        {
            HangGliderVisualState.Float => floatGripAnchor,
            HangGliderVisualState.Dive => diveGripAnchor,
            HangGliderVisualState.GlideLeft => bankGripAnchor,
            HangGliderVisualState.GlideRight => bankGripAnchor,
            _ => hoverGripAnchor
        };
    }

    private static Vector3 GripOffsetInParent(Sprite sprite, Vector2 normalizedGrip,
        bool flipX, Vector3 scale, Quaternion rotation)
    {
        if (sprite == null) return Vector3.zero;
        Vector2 pointPixels = Vector2.Scale(normalizedGrip, sprite.rect.size);
        Vector2 localPoint = (pointPixels - sprite.pivot) / sprite.pixelsPerUnit;
        if (flipX) localPoint.x = -localPoint.x;
        return rotation * Vector3.Scale(new Vector3(localPoint.x, localPoint.y, 0f), scale);
    }

    private void CacheRestPose(bool force)
    {
        if ((!force && restPoseCached) || gliderRenderer == null) return;
        restPosition = gliderRenderer.transform.localPosition;
        restScale = gliderRenderer.transform.localScale;
        restRotation = gliderRenderer.transform.localRotation;
        restGripPosition = restPosition + GripOffsetInParent(hoverFrontSprite,
            hoverGripAnchor, false, restScale, restRotation);
        restPoseCached = true;
    }

    private void ApplyStowedPose()
    {
        if (CurrentState != HangGliderVisualState.Stowed)
        {
            CurrentState = HangGliderVisualState.Stowed;
        }
        CurrentFrame = 0;
        WingFlexAmount = 0f;
        GripAnchorError = 0f;
        animationClock = 0f;
        if (gliderRenderer != null)
        {
            CacheRestPose(false);
            gliderRenderer.sprite = hoverFrontSprite != null ? hoverFrontSprite : gliderRenderer.sprite;
            gliderRenderer.flipX = false;
            gliderRenderer.enabled = false;
            gliderRenderer.transform.localPosition = restPosition;
            gliderRenderer.transform.localScale = restScale;
            gliderRenderer.transform.localRotation = restRotation;
            CurrentGripPosition = restGripPosition;
        }
        if (minerVisual != null) minerVisual.ClearFlightPose();
    }

    private void OnDisable()
    {
        ApplyStowedPose();
    }
}
