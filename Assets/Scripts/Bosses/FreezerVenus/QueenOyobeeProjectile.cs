using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QueenOyobeeProjectile : MonoBehaviour
{
    private QueenOyobeeController owner;
    private SpriteRenderer visual;
    private Rigidbody2D rb;
    private CircleCollider2D hitbox;
    private MovementController target;
    private Vector2 position;
    private Vector2 direction;
    private Vector2 start;
    private Vector2 end;
    private float elapsed;
    private float orbitAngle;
    private bool crown;
    private bool spent;
    private readonly HashSet<int> hitPlayers = new();
    public const float OrbitSeconds = 1.1f;
    public const float FlightSeconds = 1.3f;

    public static QueenOyobeeProjectile Create(QueenOyobeeController owner, bool crown, int index)
    {
        var go = new GameObject(crown ? "Queen crown" : "Queen snowflake");
        go.layer = LayerMask.NameToLayer("Projectile");
        var p = go.AddComponent<QueenOyobeeProjectile>();
        p.owner = owner;
        p.crown = crown;
        p.orbitAngle = index * 45f;
        p.position = crown ? (Vector2)owner.CrownOrigin : (Vector2)(owner.Boss.transform.position + Vector3.up * 0.5f);
        p.start = p.position;
        var players = new List<MovementController>();
        foreach (var player in Object.FindObjectsByType<MovementController>())
            if (player != null && player.CompareTag("Player") && !player.isDead && !player.IsEndingStage && player.isActiveAndEnabled)
                players.Add(player);
        players.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
        if (players.Count > 0) p.target = players[index % players.Count];
        if (p.target == null) p.target = owner.Boss.FindTarget(p.position);
        Vector2 aim = p.target != null ? (Vector2)p.target.transform.position : p.start + Vector2.down * 5f;
        p.end = aim;
        if (crown) p.target = owner.Boss.FindTarget(p.position);
        if (crown && p.target != null) p.end = p.target.transform.position;
        p.visual = go.AddComponent<SpriteRenderer>();
        p.visual.sortingLayerID = owner.Boss.body.sortingLayerID;
        p.visual.sortingOrder = owner.Boss.body.sortingOrder + 2;
        p.visual.sprite = crown ? owner.crownFrames[0] : owner.snowFrames[index % owner.snowFrames.Length];
        p.rb = go.AddComponent<Rigidbody2D>();
        p.rb.bodyType = RigidbodyType2D.Kinematic;
        p.rb.gravityScale = 0f;
        p.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        p.hitbox = go.AddComponent<CircleCollider2D>();
        p.hitbox.isTrigger = true;
        p.hitbox.radius = crown ? 0.45f : 0.22f;
        p.hitbox.enabled = crown;
        go.transform.position = p.position;
        return p;
    }

    private void FixedUpdate()
    {
        if (spent || GamePauseController.IsPaused) return;
        if (owner == null || !owner.Boss.CombatActive) { Destroy(gameObject); return; }
        float dt = Time.fixedDeltaTime;
        elapsed += dt;
        if (crown)
        {
            float t = Mathf.Clamp01(elapsed / (FlightSeconds * 2f));
            position = CirclePoint(start, end, t);
            if (t >= 1f) { Destroy(gameObject); return; }
            visual.sprite = owner.crownFrames[Mathf.FloorToInt(elapsed * 12f) % owner.crownFrames.Length];
        }
        else if (elapsed < OrbitSeconds)
        {
            float angle = (orbitAngle + elapsed * 200f) * Mathf.Deg2Rad;
            position = (Vector2)(owner.Boss.transform.position + Vector3.up * 0.5f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.4f;
        }
        else
        {
            if (!hitbox.enabled)
            {
                direction = target != null ? ((Vector2)target.transform.position - position).normalized : Vector2.down;
                hitbox.enabled = true;
            }
            float flight = elapsed - OrbitSeconds;
            if (flight < 1.2f && target != null && !target.isDead && !target.IsEndingStage)
            {
                Vector2 desired = ((Vector2)target.transform.position - position).normalized;
                direction = Quaternion.Euler(0f, 0f, Mathf.Clamp(Vector2.SignedAngle(direction, desired), -35f * dt, 35f * dt)) * direction;
            }
            position += direction * 7.6f * dt;
            visual.sprite = owner.snowFrames[Mathf.FloorToInt(elapsed * 8f) % owner.snowFrames.Length];
            if (flight > 4f || !owner.Boss.Arena.IsFloor(owner.Boss.Arena.Cell(position)))
            { Destroy(gameObject); return; }
        }
        rb.position = FreezerVenusArena.Snap(position);
        transform.position = rb.position;
    }

    public static Vector2 CirclePoint(Vector2 head, Vector2 targetPosition, float progress)
    {
        Vector2 center = (head + targetPosition) * 0.5f;
        Vector2 radius = head - center;
        Vector2 perpendicular = new(-radius.y, radius.x);
        float angle = Mathf.Clamp01(progress) * Mathf.PI * 2f;
        return center + radius * Mathf.Cos(angle) + perpendicular * Mathf.Sin(angle);
    }

    private void OnTriggerEnter2D(Collider2D other) => Contact(other);
    private void OnTriggerStay2D(Collider2D other) => Contact(other);
    private void Contact(Collider2D other)
    {
        if (spent || !hitbox.enabled || GamePauseController.IsPaused) return;
        if (crown)
        {
            var hitPlayer = other.GetComponentInParent<MovementController>();
            if (hitPlayer == null) return;
            int id = hitPlayer.GetEntityId().GetHashCode();
            if (!hitPlayers.Contains(id) && FreezerVenusBoss.TryHitPlayer(other)) hitPlayers.Add(id);
            return;
        }
        var player = other.GetComponentInParent<MovementController>();
        if (player == null || !player.CompareTag("Player") || player.isDead || player.IsEndingStage || player.InputLocked) return;
        var health = player.GetComponent<CharacterHealth>();
        if (player.explosionInvulnerable || (health != null && health.IsInvulnerable)) return;
        var freeze = player.GetComponent<QueenOyobeeFreeze>();
        if (freeze == null) freeze = player.gameObject.AddComponent<QueenOyobeeFreeze>();
        if (freeze.TryFreeze())
        {
            spent = true;
            hitbox.enabled = false;
            Destroy(gameObject);
        }
    }
}
