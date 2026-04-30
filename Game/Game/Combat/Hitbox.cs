using Godot;
using System.Collections.Generic;

public partial class Hitbox : Area2D
{
    [Signal]
    public delegate void HitDetectedEventHandler(Area2D hurtbox);

    private readonly HashSet<Area2D> _hitThisSwing = new();

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    // Enable monitoring and handle the case where targets are already overlapping.
    // AreaEntered only fires on enter, so we defer a check for pre-existing overlaps.
    public void Activate()
    {
        _hitThisSwing.Clear();
        Monitoring = true;
        CallDeferred(MethodName.CheckInitialOverlaps);
    }

    public void Deactivate()
    {
        Monitoring = false;
        _hitThisSwing.Clear();
    }

    private void CheckInitialOverlaps()
    {
        foreach (var area in GetOverlappingAreas())
            OnAreaEntered(area);
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area.IsInGroup("hurtbox") && _hitThisSwing.Add(area))
            EmitSignal(SignalName.HitDetected, area);
    }
}
