using Godot;

public static class AIConstants
{
    // Slot offsets relative to player, player facing right (positive X = forward)
    public static readonly Vector2 OffsetFrontAttack       = new( 55f,    0f);
    public static readonly Vector2 OffsetRearAttack        = new(-55f,    0f);
    public static readonly Vector2 OffsetNearDiagonalFront = new( 55f,   55f);
    public static readonly Vector2 OffsetNearDiagonalRear  = new(-55f,   55f);
    public static readonly Vector2 OffsetFarDiagonalFront  = new( 55f,  -55f);
    public static readonly Vector2 OffsetFarDiagonalRear   = new(-55f,  -55f);
    public static readonly Vector2 OffsetWideFront         = new( 160f,   0f);
    public static readonly Vector2 OffsetWideRear          = new(-160f,   0f);

    // Slot assignment timing
    public const float SlotAssignmentInterval = 0.5f;   // seconds between assignment passes
    public const float SlotAssignmentJitter   = 0.1f;   // ± seconds of randomisation per cycle

    // Cost function weights
    public const float CostWeightDistance   = 1.0f;   // c1: travel time (distance / walkSpeed)
    public const float CostWeightPreference = 0.5f;   // c2: preference mismatch penalty
    public const float CostWeightStickiness = 0.4f;   // c3: stay-in-slot discount (subtracted)
    public const float CostWeightWide       = 0.3f;   // c4: wide-slot penalty

    // World boundary Y limits — slots outside these are clamped and penalised
    public const float BoundaryTop       = 100f;
    public const float BoundaryBottom    = 500f;
    public const float OutOfBoundsPenalty = 0.5f;
}
