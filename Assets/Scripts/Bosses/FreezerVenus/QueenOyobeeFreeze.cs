using UnityEngine;

// Uses the existing stun and health visual owners without adding or replacing player sprites.
[DisallowMultipleComponent]
public sealed class QueenOyobeeFreeze : MonoBehaviour
{
    private StunReceiver stun;
    private MovementController movement;
    private CharacterHealth health;
    private float remaining;
    private float protection;
    private bool tinted;

    private void Awake()
    {
        stun = GetComponent<StunReceiver>();
        movement = GetComponent<MovementController>();
        health = GetComponent<CharacterHealth>();
    }

    public bool TryFreeze()
    {
        if (stun == null || !stun.CanReceiveStun || stun.IsStunned || protection > 0f) return false;
        stun.Stun(2f, preserveCurrentSprites: true);
        remaining = 2f;
        protection = 3.5f;
        if (health != null)
        {
            health.SetPersistentTint(new Color(0.55f, 0.85f, 1f, 1f));
            tinted = true;
        }
        return true;
    }

    private void Update()
    {
        if (GamePauseController.IsPaused) return;
        protection = Mathf.Max(0f, protection - Time.deltaTime);
        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        if (remaining <= 0f || movement == null || movement.isDead || movement.IsEndingStage ||
            stun == null || !stun.IsStunned) ClearTint();
    }

    private void ClearTint()
    {
        if (tinted && health != null) health.ClearPersistentTint();
        tinted = false;
    }

    private void OnDisable()
    {
        remaining = 0f;
        protection = 0f;
        ClearTint();
    }
}
