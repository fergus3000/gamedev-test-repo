using Godot;

/// <summary>
/// Player controller: handles movement, facing, and punch attacks.
/// Uses CharacterBody2D for physics-based movement.
/// </summary>
public partial class Player : CharacterBody2D
{
	[Export] public float MoveSpeed { get; set; } = 200.0f;
	[Export] public float PunchDuration { get; set; } = 0.15f;
	[Export] public float PunchDamage { get; set; } = 10.0f;
	[Export] public float PunchKnockbackStrength { get; set; } = 300.0f;

	private Area2D _punchHitbox;
	private Hitbox _hitboxScript;
	private float _punchTimer = 0.0f;
	private bool _isPunching = false;
	private CanvasItem _visual;

	public override void _Ready()
	{
		// Find the punch hitbox child
		_punchHitbox = GetNode<Area2D>("PunchHitbox");
		_hitboxScript = _punchHitbox as Hitbox;

		// Connect to hitbox signal
		if (_hitboxScript != null)
		{
			_hitboxScript.HitDetected += OnPunchHit;
		}

		// Find visual child (ColorRect) for flipping
		_visual = GetNodeOrNull<CanvasItem>("Visual");
		if (_visual == null)
		{
			// Try ColorRect directly
			var colorRect = GetNodeOrNull<ColorRect>("ColorRect");
			if (colorRect != null)
			{
				_visual = colorRect;
			}
		}

		// Start with hitbox disabled
		_punchHitbox.Monitoring = false;
		_punchHitbox.Monitorable = false;
	}

	public override void _Process(double delta)
	{
		// Update punch timer
		if (_isPunching)
		{
			_punchTimer -= (float)delta;
			if (_punchTimer <= 0.0f)
			{
				EndPunch();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Handle input
		Vector2 inputDirection = Vector2.Zero;

		if (Input.IsActionPressed("move_left"))
		{
			inputDirection.X -= 1.0f;
		}
		if (Input.IsActionPressed("move_right"))
		{
			inputDirection.X += 1.0f;
		}
		if (Input.IsActionPressed("move_up"))
		{
			inputDirection.Y -= 1.0f;
		}
		if (Input.IsActionPressed("move_down"))
		{
			inputDirection.Y += 1.0f;
		}

		// Normalize diagonal movement
		if (inputDirection.Length() > 1.0f)
		{
			inputDirection = inputDirection.Normalized();
		}

		// Handle attack input
		if (Input.IsActionJustPressed("attack") && !_isPunching)
		{
			StartPunch();
		}

		// Update facing based on movement or attack direction
		if (inputDirection.X != 0.0f)
		{
			UpdateFacing(inputDirection.X > 0.0f);
		}
		else if (_isPunching && _visual != null)
		{
			// Keep facing direction during punch
			// (facing already set when punch started)
		}

		// Apply movement
		Velocity = inputDirection * MoveSpeed;
		MoveAndSlide();
	}

	private void UpdateFacing(bool facingRight)
	{
		// Flip player (and visual children) horizontally via Node2D.Scale
		Scale = new Vector2(facingRight ? 1.0f : -1.0f, 1.0f);

		// Hitbox stays at fixed local offset; parent Scale flips it with the player
		if (_punchHitbox != null)
		{
			_punchHitbox.Position = new Vector2(40.0f, 0.0f);
		}
	}

	private void StartPunch()
	{
		_isPunching = true;
		_punchTimer = PunchDuration;

		// Enable hitbox
		_punchHitbox.Monitoring = true;
		_punchHitbox.Monitorable = false; // Hitboxes don't need to be detected by others

		// Update hitbox position based on current facing
		bool facingRight = Scale.X > 0.0f;
		UpdateFacing(facingRight);
	}

	private void EndPunch()
	{
		_isPunching = false;
		_punchTimer = 0.0f;

		// Disable hitbox
		_punchHitbox.Monitoring = false;
	}

	private void OnPunchHit(Area2D hurtbox)
	{
		// Get the enemy parent of the hurtbox
		Node parent = hurtbox.GetParent();
		if (parent is Enemy enemy)
		{
			// Calculate knockback direction (away from player)
			Vector2 knockbackDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
			if (knockbackDir.Length() < 0.1f)
			{
				// Fallback if positions are too close
				knockbackDir = Scale.X > 0.0f ? Vector2.Right : Vector2.Left;
			}

			enemy.TakeDamage(PunchDamage, knockbackDir * PunchKnockbackStrength);
		}
	}
}
