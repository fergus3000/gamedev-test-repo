using Godot;

public partial class Enemy : CharacterBody2D, IDamageable
{
	[Export] public float MaxHP { get; set; } = 30.0f;
	[Export] public float WalkSpeed { get; set; } = 80.0f;
	[Export] public float AttackRange { get; set; } = 55.0f;
	[Export] public float AttackCooldown { get; set; } = 1.5f;
	[Export] public float AttackDuration { get; set; } = 0.3f;
	[Export] public float AttackDamage { get; set; } = 8.0f;
	[Export] public float AttackKnockback { get; set; } = 200.0f;
	[Export] public float HitstunDuration { get; set; } = 0.3f;
	[Export] public float KnockbackDecay { get; set; } = CombatConstants.KnockbackDecayBase;

	public enum EnemyState { Idle, Walking, Attacking, Hitstun, Dead }

	private float _currentHP;
	private EnemyState _state = EnemyState.Idle;
	private float _hitstunTimer = 0.0f;
	private float _attackTimer = 0.0f;
	private float _attackCooldownTimer = 0.0f;
	private Node2D _player;
	private Area2D _attackHitbox;
	private Hitbox _hitboxScript;
	private CanvasItem _visual;

	public float CurrentHP => _currentHP;
	public EnemyState State => _state;

	public override void _Ready()
	{
		_currentHP = MaxHP;

		// Find player — Player._Ready() runs first (scene order), so the group is already populated
		_player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (_player != null)
			_state = EnemyState.Walking;

		_attackHitbox = GetNodeOrNull<Area2D>("AttackHitbox");
		_hitboxScript = _attackHitbox as Hitbox;
		if (_hitboxScript != null)
			_hitboxScript.HitDetected += OnAttackHit;

		if (_attackHitbox != null)
		{
			_attackHitbox.Monitoring = false;
			_attackHitbox.Monitorable = false;
		}

		var hurtbox = GetNodeOrNull<Area2D>("Hurtbox");
		if (hurtbox != null)
			hurtbox.AddToGroup("hurtbox");

		_visual = GetNodeOrNull<CanvasItem>("Visual");
		if (_visual == null)
		{
			var colorRect = GetNodeOrNull<ColorRect>("ColorRect");
			if (colorRect != null)
				_visual = colorRect;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (_state)
		{
			case EnemyState.Idle:
				Velocity = Vector2.Zero;
				MoveAndSlide();
				break;

			case EnemyState.Walking:
				UpdateWalking((float)delta);
				break;

			case EnemyState.Attacking:
				UpdateAttacking((float)delta);
				break;

			case EnemyState.Hitstun:
				UpdateHitstun((float)delta);
				break;

			case EnemyState.Dead:
				Velocity = Vector2.Zero;
				break;
		}
	}

	private void UpdateWalking(float delta)
	{
		if (_player == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (_attackCooldownTimer > 0f)
			_attackCooldownTimer -= delta;

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		bool inDepth = Mathf.Abs(toPlayer.Y) <= CombatConstants.DepthTolerancePx;

		if (toPlayer.Length() <= AttackRange && inDepth && _attackCooldownTimer <= 0f)
		{
			StartAttack();
		}
		else
		{
			UpdateFacing(toPlayer.X > 0f);
			Velocity = toPlayer.Normalized() * WalkSpeed;
			MoveAndSlide();
		}
	}

	private void UpdateAttacking(float delta)
	{
		Velocity = Vector2.Zero;
		MoveAndSlide();
		_attackTimer -= delta;
		if (_attackTimer <= 0f)
			EndAttack();
	}

	private void UpdateHitstun(float delta)
	{
		Velocity *= (float)Mathf.Pow(KnockbackDecay, delta * 60.0);
		MoveAndSlide();
		_hitstunTimer -= delta;
		if (_hitstunTimer <= 0f)
		{
			_state = EnemyState.Walking;
			Velocity = Vector2.Zero;
		}
	}

	private void StartAttack()
	{
		_state = EnemyState.Attacking;
		_attackTimer = AttackDuration;
		if (_attackHitbox != null)
		{
			if (_player != null)
				UpdateFacing(_player.GlobalPosition.X > GlobalPosition.X);
			if (_hitboxScript != null)
				_hitboxScript.Activate();
			else
				_attackHitbox.Monitoring = true;
		}
	}

	private void EndAttack()
	{
		_state = EnemyState.Walking;
		_attackCooldownTimer = AttackCooldown;
		if (_attackHitbox != null)
		{
			if (_hitboxScript != null)
				_hitboxScript.Deactivate();
			else
				_attackHitbox.Monitoring = false;
		}
	}

	private void UpdateFacing(bool facingRight)
	{
		Scale = new Vector2(facingRight ? 1.0f : -1.0f, 1.0f);
		if (_attackHitbox != null)
			// Hitbox offset is always +X in local space; Scale.X flip on the parent mirrors it automatically
			_attackHitbox.Position = new Vector2(CombatConstants.HitboxOffsetX, 0.0f);
	}

	private void OnAttackHit(Area2D hurtbox)
	{
		Node parent = hurtbox.GetParent();
		if (parent is IDamageable target && parent is Node2D targetNode)
		{
			// Secondary depth check at hit resolution — independent of movement/approach logic
			if (Mathf.Abs(GlobalPosition.Y - targetNode.GlobalPosition.Y) > CombatConstants.DepthTolerancePx)
				return;

			Vector2 knockbackDir = (targetNode.GlobalPosition - GlobalPosition).Normalized();
			if (knockbackDir.Length() < 0.1f)
				knockbackDir = Scale.X > 0f ? Vector2.Right : Vector2.Left;

			target.TakeDamage(AttackDamage, knockbackDir * AttackKnockback);
		}
	}

	public void TakeDamage(float damage, Vector2 knockbackVelocity)
	{
		if (_state == EnemyState.Dead)
			return;

		// Interrupt attack if mid-swing
		if (_state == EnemyState.Attacking && _attackHitbox != null)
			_attackHitbox.SetDeferred("monitoring", false);

		_currentHP -= damage;

		if (_currentHP <= 0.0f)
		{
			_currentHP = 0.0f;
			_state = EnemyState.Dead;
			Velocity = Vector2.Zero;

			var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (collisionShape != null)
				collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			if (_visual != null)
				_visual.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
		else
		{
			_state = EnemyState.Hitstun;
			_hitstunTimer = HitstunDuration;
			Velocity = knockbackVelocity;
		}
	}

	public string GetDebugText()
	{
		string stateText = _state switch
		{
			EnemyState.Idle      => "Idle",
			EnemyState.Walking   => "Walking",
			EnemyState.Attacking => "Attacking",
			EnemyState.Hitstun   => "Hitstun",
			EnemyState.Dead      => "Dead",
			_                    => "Unknown"
		};
		return $"Enemy   HP: {_currentHP:F1}/{MaxHP:F1}  State: {stateText}";
	}
}
