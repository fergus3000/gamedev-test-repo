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
    public const float CostWeightTier       = 0.3f;   // c4: slot tier penalty (see GetSlotTierPenalty)

    // Tier penalties — attack slots are always preferred; outer slots penalised progressively
    public static float GetSlotTierPenalty(AIManager.SlotName slot) => slot switch
    {
        AIManager.SlotName.FrontAttack or AIManager.SlotName.RearAttack           => 0.0f,
        AIManager.SlotName.NearDiagonalFront or AIManager.SlotName.NearDiagonalRear => 0.4f,
        AIManager.SlotName.FarDiagonalFront  or AIManager.SlotName.FarDiagonalRear  => 0.7f,
        AIManager.SlotName.WideFront         or AIManager.SlotName.WideRear         => 1.0f,
        _ => 0.0f
    };

    // World boundary Y limits — slots outside these are clamped and penalised
    public const float BoundaryTop       = 100f;
    public const float BoundaryBottom    = 500f;
    public const float OutOfBoundsPenalty = 0.5f;

    // Pathfinding
    public const float ObstacleClearance       = 8.0f;   // padding around obstacles for waypoint corners
    public const float ZoneOfControlHalfWidth  = 50.0f;  // X half-extent of player ZoC rectangle
    public const float ZoneOfControlHalfHeight = 30.0f;  // Y half-extent of player ZoC rectangle
    public const float WaypointArrivalThreshold = 6.0f;  // distance at which a waypoint is considered reached
}
