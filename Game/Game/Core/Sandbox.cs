using Godot;

/// <summary>
/// Root script for the combat sandbox scene.
/// Updates debug overlay with enemy information.
/// </summary>
public partial class Sandbox : Node2D
{
    private Enemy _enemy;
    private Label _debugLabel;

    public override void _Ready()
    {
        // Find enemy and debug label
        _enemy = GetNodeOrNull<Enemy>("Enemy");
        var debugOverlay = GetNodeOrNull<CanvasLayer>("DebugOverlay");
        if (debugOverlay != null)
        {
            _debugLabel = debugOverlay.GetNodeOrNull<Label>("Label");
        }

        if (_enemy == null)
        {
            GD.PrintErr("Sandbox: Enemy node not found!");
        }
        if (_debugLabel == null)
        {
            GD.PrintErr("Sandbox: Debug Label not found!");
        }
    }

    public override void _Process(double delta)
    {
        if (_enemy != null && _debugLabel != null)
        {
            _debugLabel.Text = _enemy.GetDebugText();
        }
    }
}
