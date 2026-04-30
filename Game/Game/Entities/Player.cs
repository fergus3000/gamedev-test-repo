using Godot;

/// <summary>
/// Player controller: handles movement, facing, punch attacks, and taking damage.
/// Uses CharacterBody2D for physics-based movement.
/// </summary>
public partial class Player : CharacterBody2D
{
	[Export] public float MaxHP { get; set; } = 100.0f;
	[Export] public float MoveSpeed { get; set; } = 200.0f;
	[Export] public float PunchDuration { get; set; } = 0.15f;
	[Export] public float PunchDamage { get; set; } = 10.0f;
	[Export] public float PunchKnockbackStrength { get; set; } = 300.0f;
	[Export] public float HitstunDuration { get; set; } = 0.4f;
	// Velocity multiplier per 60 fps frame — normalised to delta in code so framerate doesn't matter
	[Export] public float KnockbackDecay { get; set; } = 0.85f;
	[Export] public float DepthTolerancePx { get; set; } = 24.0f;

	public enum PlayerState { Normal, Hitstun, Dead }

	private float _currentHP;
	private PlayerState _state = PlayerState.Normal;
	private float _hitstunTimer = 0.0f;
	private Area2D _punchHitbox;
	private Hitbox _hitboxScript;
	private float _punchTimer = 0.0f;
	private bool _isPunching = false;
	private CanvasItem _visual;

	public float CurrentHP => _currentHP;
	public PlayerState State => _state;

	public override void _Ready()
	{
		_currentHP = MaxHP;
		AddToGroup("player");

		_punchHitbox = GetNode<Area2D>("PunchHitbox");
		_hitboxScript = _punchHitbox as Hitbox;
		if (_hitboxScript != null)
			_hitboxScript.HitDetected += OnPunchHit;

		_visual = GetNodeOrNull<CanvasItem>("Visual");
		if (_visual == null)
		{
			var colorRect = GetNodeOrNull<ColorRect>("ColorRect");
			if (colorRect != null)
				_visual = colorRect;
		}

		_punchHitbox.Monitoring = false;
		_punchHitbox.Monitorable = false;
	}

	public override void _Process(double delta)
	{
		if (_isPunching)
		{
			_punchTimer -= (float)delta;
			if (_punchTimer <= 0.0f)
				EndPunch();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_state == PlayerState.Hitstun)
		{
			Velocity *= (float)Mathf.Pow(KnockbackDecay, (float)delta * 60.0);
			MoveAndSlide();
			_hitstunTimer -= (float)delta;
			if (_hitstunTimer <= 0.0f)
			{
				_state = PlayerState.Normal;
				Velocity = Vector2.Zero;
			}
			return;
		}

		if (_state == PlayerState.Dead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// Normal state — handle input
		Vector2 inputDirection = Vector2.Zero;
		if (Input.IsActionPressed("move_left"))  inputDirection.X -= 1.0f;
		if (Input.IsActionPressed("move_right")) inputDirection.X += 1.0f;
		if (Input.IsActionPressed("move_up"))    inputDirection.Y -= 1.0f;
		if (Input.IsActionPressed("move_down"))  inputDirection.Y += 1.0f;

		if (inputDirection.Length() > 1.0f)
			inputDirection = inputDirection.Normalized();

		if (Input.IsActionJustPressed("attack") && !_isPunching)
			StartPunch();

		if (inputDirection.X != 0.0f)
			UpdateFacing(inputDirection.X > 0.0f);

		Velocity = inputDirection * MoveSpeed;
		MoveAndSlide();
	}

	public void TakeDamage(float damage, Vector2 knockbackVelocity)
	{
		if (_state == PlayerState.Dead)
			return;

		if (_isPunching)
			EndPunch();

		_currentHP -= damage;

		if (_currentHP <= 0.0f)
		{
			_currentHP = 0.0f;
			_state = PlayerState.Dead;
			Velocity = Vector2.Zero;
			if (_visual != null)
				_visual.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
		else
		{
			_state = PlayerState.Hitstun;
			_hitstunTimer = HitstunDuration;
			Velocity = knockbackVelocity;
		}
	}

	public string GetDebugText()
	{
		string stateText = _state switch
		{
			PlayerState.Normal  => _isPunching ? "Attacking" : "Normal",
			PlayerState.Hitstun => "Hitstun",
			PlayerState.Dead    => "Dead",
			_                   => "Unknown"
		};
		return $"Player  HP: {_currentHP:F1}/{MaxHP:F1}  State: {stateText}";
	}

	private void UpdateFacing(bool facingRight)
	{
		Scale = new Vector2(facingRight ? 1.0f : -1.0f, 1.0f);
		if (_punchHitbox != null)
			_punchHitbox.Position = new Vector2(40.0f, 0.0f);
	}

	private void StartPunch()
	{
		_isPunching = true;
		_punchTimer = PunchDuration;
		_punchHitbox.Monitorable = false;
		if (_hitboxScript != null)
			_hitboxScript.Activate();
		else
			_punchHitbox.Monitoring = true;
		UpdateFacing(Scale.X > 0.0f);
	}

	private void EndPunch()
	{
		_isPunching = false;
		_punchTimer = 0.0f;
		if (_hitboxScript != null)
			_hitboxScript.Deactivate();
		else
			_punchHitbox.Monitoring = false;
	}

	private void OnPunchHit(Area2D hurtbox)
	{
		Node parent = hurtbox.GetParent();
		if (parent is Enemy enemy)
		{
			if (Mathf.Abs(GlobalPosition.Y - enemy.GlobalPosition.Y) > DepthTolerancePx)
				return;

			Vector2 knockbackDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
			if (knockbackDir.Length() < 0.1f)
				knockbackDir = Scale.X > 0.0f ? Vector2.Right : Vector2.Left;

			enemy.TakeDamage(PunchDamage, knockbackDir * PunchKnockbackStrength);
		}
	}
}
