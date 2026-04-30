using Godot;

public interface IDamageable
{
    void TakeDamage(float damage, Vector2 knockbackVelocity);
}
