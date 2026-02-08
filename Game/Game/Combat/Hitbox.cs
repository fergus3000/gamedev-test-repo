using Godot;

/// <summary>
/// Attached to an Area2D that represents a hitbox (e.g., punch attack).
/// Detects when this hitbox overlaps with a hurtbox and notifies the owner.
/// </summary>
public partial class Hitbox : Area2D
{
    [Signal]
    public delegate void HitDetectedEventHandler(Area2D hurtbox);

    public override void _Ready()
    {
        // Connect to area_entered to detect overlaps with hurtboxes
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        // Check if the overlapping area is a hurtbox (has the "hurtbox" group)
        if (area.IsInGroup("hurtbox"))
        {
            EmitSignal(SignalName.HitDetected, area);
        }
    }
}
