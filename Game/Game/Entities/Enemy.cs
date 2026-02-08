using Godot;

/// <summary>
/// Enemy dummy: has HP, can take damage, enters hitstun and knockback when hit.
/// Uses CharacterBody2D for physics-based movement.
/// </summary>
public partial class Enemy : CharacterBody2D
{
	[Export] public float MaxHP { get; set; } = 30.0f;
	[Export] public float HitstunDuration { get; set; } = 0.3f;
	[Export] public float KnockbackDecay { get; set; } = 0.85f; // Per frame decay

	private float _currentHP;
	private EnemyState _state = EnemyState.Idle;
	private float _hitstunTimer = 0.0f;
	private CanvasItem _visual;

	public float CurrentHP => _currentHP;
	public EnemyState State => _state;

	public enum EnemyState
	{
		Idle,
		Hitstun,
		Dead
	}

	public override void _Ready()
	{
		_currentHP = MaxHP;
		_state = EnemyState.Idle;

		// Ensure hurtbox is in group so Hitbox can detect it (scene-defined groups may not apply)
		var hurtbox = GetNodeOrNull<Area2D>("Hurtbox");
		if (hurtbox != null)
			hurtbox.AddToGroup("hurtbox");

		// Find visual child for potential effects
		_visual = GetNodeOrNull<CanvasItem>("Visual");
		if (_visual == null)
		{
			var colorRect = GetNodeOrNull<ColorRect>("ColorRect");
			if (colorRect != null)
			{
				_visual = colorRect;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (_state)
		{
			case EnemyState.Idle:
				// Enemy dummy doesn't move on its own in this slice
				Velocity = Vector2.Zero;
				MoveAndSlide();
				break;

			case EnemyState.Hitstun:
				// Apply knockback velocity
				MoveAndSlide();

				// Decay knockback
				Velocity *= KnockbackDecay;

				// Update hitstun timer
				_hitstunTimer -= (float)delta;
				if (_hitstunTimer <= 0.0f)
				{
					_state = EnemyState.Idle;
					Velocity = Vector2.Zero;
				}
				break;

			case EnemyState.Dead:
				// Dead enemies don't move
				Velocity = Vector2.Zero;
				break;
		}
	}

	public void TakeDamage(float damage, Vector2 knockbackDirection)
	{
		if (_state == EnemyState.Dead)
		{
			return; // Already dead
		}

		_currentHP -= damage;

		if (_currentHP <= 0.0f)
		{
			_currentHP = 0.0f;
			_state = EnemyState.Dead;
			Velocity = Vector2.Zero;

			// Disable collision and visual (optional)
			var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (collisionShape != null)
			{
				collisionShape.Disabled = true;
			}
			if (_visual != null)
			{
				_visual.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Dim and semi-transparent
			}
		}
		else
		{
			// Enter hitstun
			_state = EnemyState.Hitstun;
			_hitstunTimer = HitstunDuration;
			Velocity = knockbackDirection;
		}
	}

	public string GetDebugText()
	{
		string stateText = _state switch
		{
			EnemyState.Idle => "Idle",
			EnemyState.Hitstun => "Hitstun",
			EnemyState.Dead => "Dead",
			_ => "Unknown"
		};

		return $"HP: {_currentHP:F1}/{MaxHP:F1}  State: {stateText}";
	}
}
