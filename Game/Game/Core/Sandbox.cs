using Godot;
using System.Text;

public partial class Sandbox : Node2D
{
    private Player _player;
    private Label  _debugLabel;

    public override void _Ready()
    {
        _player = GetNodeOrNull<Player>("Player");
        var debugOverlay = GetNodeOrNull<CanvasLayer>("DebugOverlay");
        if (debugOverlay != null)
            _debugLabel = debugOverlay.GetNodeOrNull<Label>("Label");

        if (_player == null)     GD.PrintErr("Sandbox: Player node not found!");
        if (_debugLabel == null) GD.PrintErr("Sandbox: Debug Label not found!");
    }

    public override void _Process(double delta)
    {
        if (_debugLabel == null) return;

        var sb = new StringBuilder();
        if (_player != null)
            sb.AppendLine(_player.GetDebugText());

        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy enemy)
                sb.AppendLine(enemy.GetDebugText());
        }

        _debugLabel.Text = sb.ToString().TrimEnd();
    }
}
