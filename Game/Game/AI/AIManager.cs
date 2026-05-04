using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload singleton. Owns slot layout around the player and runs greedy
/// slot assignment for all active enemies on a jittered timer.
/// </summary>
public partial class AIManager : Node2D
{
    public enum SlotName
    {
        FrontAttack = 0,
        RearAttack,
        NearDiagonalFront,
        NearDiagonalRear,
        FarDiagonalFront,
        FarDiagonalRear,
        WideFront,
        WideRear
    }

    public struct SlotDefinition
    {
        public SlotName  Name;
        public Vector2   WorldPosition;
        public bool      IsOccupied;
        public Enemy     OccupantEnemy;
        public bool      IsClamped;       // true when Y was clamped to boundary
    }

    private const int SlotCount = 8;

    [Export] public bool DebugDraw { get; set; } = true;

    private SlotDefinition[]            _slots       = new SlotDefinition[SlotCount];
    private Node2D                      _player;
    private readonly List<Enemy>        _enemies     = new();
    private readonly Dictionary<Enemy, SlotName?> _enemySlots = new();
    private float                       _assignmentTimer;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        ZIndex = 100;   // draw debug overlay on top of game objects
        InitSlots();
        _assignmentTimer = AIConstants.SlotAssignmentInterval;
    }

    public override void _Process(double delta)
    {
        // Lazy player lookup — autoload initialises before the game scene
        if (_player == null)
            _player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (_player != null)
            RecalculateSlotPositions();

        _assignmentTimer -= (float)delta;
        if (_assignmentTimer <= 0f)
        {
            RunSlotAssignment();
            float jitter = (GD.Randf() * 2f - 1f) * AIConstants.SlotAssignmentJitter;
            _assignmentTimer = AIConstants.SlotAssignmentInterval + jitter;
        }

        if (DebugDraw)
            QueueRedraw();
    }

    // -------------------------------------------------------------------------
    // Enemy registration
    // -------------------------------------------------------------------------

    public void RegisterEnemy(Enemy enemy)
    {
        if (_enemies.Contains(enemy)) return;
        _enemies.Add(enemy);
        _enemySlots[enemy] = null;
    }

    public void DeregisterEnemy(Enemy enemy)
    {
        if (_enemySlots.TryGetValue(enemy, out SlotName? assigned) && assigned.HasValue)
            FreeSlot(assigned.Value);

        _enemies.Remove(enemy);
        _enemySlots.Remove(enemy);
    }

    // -------------------------------------------------------------------------
    // Slot position recalculation
    // -------------------------------------------------------------------------

    private void InitSlots()
    {
        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new SlotDefinition { Name = (SlotName)i };
    }

    private static Vector2 GetBaseOffset(SlotName slot) => slot switch
    {
        SlotName.FrontAttack       => AIConstants.OffsetFrontAttack,
        SlotName.RearAttack        => AIConstants.OffsetRearAttack,
        SlotName.NearDiagonalFront => AIConstants.OffsetNearDiagonalFront,
        SlotName.NearDiagonalRear  => AIConstants.OffsetNearDiagonalRear,
        SlotName.FarDiagonalFront  => AIConstants.OffsetFarDiagonalFront,
        SlotName.FarDiagonalRear   => AIConstants.OffsetFarDiagonalRear,
        SlotName.WideFront         => AIConstants.OffsetWideFront,
        SlotName.WideRear          => AIConstants.OffsetWideRear,
        _                          => Vector2.Zero
    };

    private void RecalculateSlotPositions()
    {
        bool facingRight = _player.Scale.X >= 0f;

        for (int i = 0; i < SlotCount; i++)
        {
            Vector2 offset = GetBaseOffset(_slots[i].Name);
            if (!facingRight)
                offset.X = -offset.X;

            Vector2 pos = _player.GlobalPosition + offset;

            bool clamped = false;
            if (pos.Y < AIConstants.BoundaryTop)        { pos.Y = AIConstants.BoundaryTop;    clamped = true; }
            else if (pos.Y > AIConstants.BoundaryBottom) { pos.Y = AIConstants.BoundaryBottom; clamped = true; }

            _slots[i].WorldPosition = pos;
            _slots[i].IsClamped     = clamped;
        }
    }

    // -------------------------------------------------------------------------
    // Greedy slot assignment
    // -------------------------------------------------------------------------

    private void RunSlotAssignment()
    {
        // 1. Free slots held by dead/hitstun enemies
        foreach (var enemy in _enemies)
        {
            if (enemy.State is Enemy.EnemyState.Hitstun or Enemy.EnemyState.Dead)
            {
                if (_enemySlots.TryGetValue(enemy, out SlotName? s) && s.HasValue)
                {
                    FreeSlot(s.Value);
                    _enemySlots[enemy] = null;
                }
            }
        }

        // 2. Collect enemies eligible for assignment
        var eligible = _enemies.FindAll(e =>
            e.State != Enemy.EnemyState.Dead &&
            e.State != Enemy.EnemyState.Hitstun);

        if (eligible.Count == 0) return;

        // 3. Snapshot previous assignments for stickiness, then release those slots
        var previousSlots = new Dictionary<Enemy, SlotName?>(eligible.Count);
        foreach (var enemy in eligible)
        {
            _enemySlots.TryGetValue(enemy, out SlotName? prev);
            previousSlots[enemy] = prev;
            if (prev.HasValue)
            {
                FreeSlot(prev.Value);
                _enemySlots[enemy] = null;
            }
        }

        // 4. Compute each enemy's best-case cost (all slots free) for sort order
        var sortKeys = new Dictionary<Enemy, float>(eligible.Count);
        foreach (var enemy in eligible)
        {
            float best = float.MaxValue;
            for (int i = 0; i < SlotCount; i++)
            {
                float c = ComputeCost(enemy, ref _slots[i], previousSlots[enemy]);
                if (c < best) best = c;
            }
            sortKeys[enemy] = best;
        }
        eligible.Sort((a, b) => sortKeys[a].CompareTo(sortKeys[b]));

        // 5. Greedy assignment — recalculate best available slot per enemy
        foreach (var enemy in eligible)
        {
            float best     = float.MaxValue;
            int   bestIdx  = -1;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].IsOccupied) continue;
                float c = ComputeCost(enemy, ref _slots[i], previousSlots[enemy]);
                if (c < best) { best = c; bestIdx = i; }
            }

            if (bestIdx < 0) continue;  // no free slot available

            _slots[bestIdx].IsOccupied    = true;
            _slots[bestIdx].OccupantEnemy = enemy;
            _enemySlots[enemy]            = _slots[bestIdx].Name;
            enemy.AssignSlot(_slots[bestIdx].WorldPosition);
        }
    }

    private float ComputeCost(Enemy enemy, ref SlotDefinition slot, SlotName? previousSlot)
    {
        // c1: travel time
        float travelTime = enemy.GlobalPosition.DistanceTo(slot.WorldPosition)
                           / Mathf.Max(enemy.WalkSpeed, 1f);
        float cost = AIConstants.CostWeightDistance * travelTime;

        // c2: preference mismatch
        if (!SlotMatchesPreference(slot.Name, enemy.Preference))
            cost += AIConstants.CostWeightPreference;

        // c3: stickiness — staying in the same slot is cheaper
        if (previousSlot == slot.Name)
            cost -= AIConstants.CostWeightStickiness;

        // c4: wide-slot penalty
        if (slot.Name is SlotName.WideFront or SlotName.WideRear)
            cost += AIConstants.CostWeightWide;

        // out-of-bounds penalty
        if (slot.IsClamped)
            cost += AIConstants.OutOfBoundsPenalty;

        return cost;
    }

    private static bool SlotMatchesPreference(SlotName slot, Enemy.SlotPreference pref) => pref switch
    {
        Enemy.SlotPreference.Front => slot is SlotName.FrontAttack
                                           or SlotName.NearDiagonalFront
                                           or SlotName.FarDiagonalFront
                                           or SlotName.WideFront,
        Enemy.SlotPreference.Rear  => slot is SlotName.RearAttack
                                           or SlotName.NearDiagonalRear
                                           or SlotName.FarDiagonalRear
                                           or SlotName.WideRear,
        _                          => true   // SlotPreference.None matches everything
    };

    private void FreeSlot(SlotName name)
    {
        int idx = (int)name;
        _slots[idx].IsOccupied    = false;
        _slots[idx].OccupantEnemy = null;
    }

    // -------------------------------------------------------------------------
    // Debug visualisation
    // -------------------------------------------------------------------------

    public override void _Draw()
    {
        if (!DebugDraw || _player == null) return;

        var font = ThemeDB.Singleton.FallbackFont;
        if (font == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            ref SlotDefinition s = ref _slots[i];
            Color color = s.IsClamped   ? new Color(0.5f, 0.5f, 0.5f)
                        : s.IsOccupied  ? new Color(0.2f, 0.9f, 0.2f)
                                        : new Color(0.9f, 0.9f, 0.2f);
            DrawCircle(s.WorldPosition, 8f, color);
            DrawString(font, s.WorldPosition + new Vector2(12f, -4f),
                       s.Name.ToString(), HorizontalAlignment.Left, -1, 11, Colors.White);
        }

        foreach (var enemy in _enemies)
        {
            if (!_enemySlots.TryGetValue(enemy, out SlotName? assigned) || !assigned.HasValue)
                continue;
            DrawLine(enemy.GlobalPosition, _slots[(int)assigned.Value].WorldPosition,
                     new Color(0.4f, 0.9f, 1f, 0.8f), 1f);
        }
    }
}
