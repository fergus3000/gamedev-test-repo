using Godot;

/// <summary>
/// Root script for the combat sandbox scene.
/// Updates debug overlay with player and enemy state.
/// </summary>
public partial class Sandbox : Node2D
{
    private Enemy _enemy;
    private Player _player;
    private Label _debugLabel;

    public override void _Ready()
    {
        _enemy = GetNodeOrNull<Enemy>("Enemy");
        _player = GetNodeOrNull<Player>("Player");
        var debugOverlay = GetNodeOrNull<CanvasLayer>("DebugOverlay");
        if (debugOverlay != null)
            _debugLabel = debugOverlay.GetNodeOrNull<Label>("Label");

        if (_enemy == null)  GD.PrintErr("Sandbox: Enemy node not found!");
        if (_player == null) GD.PrintErr("Sandbox: Player node not found!");
        if (_debugLabel == null) GD.PrintErr("Sandbox: Debug Label not found!");
    }

    public override void _Process(double delta)
    {
        if (_debugLabel == null) return;
        string playerLine = _player != null ? _player.GetDebugText() : "Player: not found";
        string enemyLine  = _enemy  != null ? _enemy.GetDebugText()  : "Enemy: not found";
        _debugLabel.Text = $"{playerLine}\n{enemyLine}";
    }
}
