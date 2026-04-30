using Godot;

public static class CombatConstants
{
    // Y-axis distance within which attacks can connect (depth lane tolerance)
    public const float DepthTolerancePx = 24.0f;

    // Local X offset of a hitbox from its owner's origin — applies to both player and enemy.
    // Always positive because the parent node's Scale.X flip handles facing direction,
    // so the child position mirrors automatically.
    public const float HitboxOffsetX = 40.0f;

    // Baseline knockback decay per 60 fps frame, normalised with Mathf.Pow(decay, delta*60)
    public const float KnockbackDecayBase = 0.85f;
}
