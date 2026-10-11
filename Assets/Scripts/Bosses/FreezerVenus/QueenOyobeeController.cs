using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FreezerVenusBoss))]
public sealed class QueenOyobeeController : MonoBehaviour
{
    public Sprite transformationSprite;
    public Sprite[] idleFrames;
    public Sprite[] hurtFrames;
    public Sprite[] crownlessFrames;
    public Sprite[] crownFrames;
    public Sprite[] snowFrames;
    public Sprite[] scepterFrames;
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    private FreezerVenusBoss boss;
    private SpriteRenderer scepter;
    private SpriteRenderer wornCrown;
    private GameObject telegraph;
    private QueenOyobeeIceWave iceWave;
    private readonly List<QueenOyobeeProjectile> projectiles = new();
    public bool Active { get; private set; }
    public FreezerVenusBoss Boss => boss;
    public Vector3 CrownOrigin => boss.transform.position + Vector3.up * 3.125f;

    private void Awake() => boss = GetComponent<FreezerVenusBoss>();

    public IEnumerator TransformRoutine()
    {
        if (FindAnyObjectByType<StageAssets.Stage35SnowfallEffect>() == null)
            new GameObject("Queen Oyobee stage snowfall").AddComponent<StageAssets.Stage35SnowfallEffect>();
        boss.body.color = Color.white;
        boss.body.sprite = transformationSprite;
        if (GameMusicController.Instance != null && music != null)
            GameMusicController.Instance.PlayMusic(music, musicVolume, loop: true);
        // Preserve native pixels while the compressed Venus pose flashes into the queen.
        float elapsed = 0f;
        telegraph = QueenOyobeeProjectile.CreateRitual(this);
        while (elapsed < 2.2f)
        {
            yield return null;
            if (GamePauseController.IsPaused) continue;
            elapsed += Time.deltaTime;
            boss.body.color = Color.Lerp(Color.white, new Color(0.5f, 0.85f, 1f), Mathf.PingPong(elapsed * 2f, 1f));
            boss.body.enabled = Mathf.FloorToInt(elapsed / Mathf.Lerp(0.18f, 0.045f, elapsed / 2.2f)) % 2 == 0;
        }
        if (telegraph != null) Destroy(telegraph);
        boss.body.enabled = true;
        boss.body.color = Color.white;
        Active = true;
        boss.SetFrame(idleFrames, 0);
        var go = new GameObject("Queen Oyobee scepter");
        go.transform.SetParent(boss.body.transform, false);
        go.transform.localPosition = new Vector3(1.375f, 0.25f, 0f);
        scepter = go.AddComponent<SpriteRenderer>();
        scepter.sortingLayerID = boss.body.sortingLayerID;
        scepter.sortingOrder = boss.body.sortingOrder + 1;
        scepter.sprite = scepterFrames[0];
        var crownObject = new GameObject("Queen Oyobee worn crown");
        crownObject.transform.SetParent(boss.body.transform, false);
        crownObject.transform.localPosition = Vector3.up * 3.125f;
        wornCrown = crownObject.AddComponent<SpriteRenderer>();
        wornCrown.sprite = crownFrames[0];
        wornCrown.sortingLayerID = boss.body.sortingLayerID;
        wornCrown.sortingOrder = boss.body.sortingOrder + 1;
        iceWave = QueenOyobeeIceWave.Create(this);
        while (iceWave != null) yield return null;
    }

    public IEnumerator AttackRoutine(int index)
    {
        bool ice = index % 3 == 0;
        Vector2[] path = null;
        float crownFlightSeconds = 0f;
        if (!ice)
        {
            var target = boss.FindTarget(CrownOrigin);
            path = QueenOyobeeProjectile.BuildCrownPath(this, target != null ? (Vector2)target.transform.position : (Vector2)boss.GroundPosition, index);
            crownFlightSeconds = QueenOyobeeProjectile.CrownFlightSeconds(path);
            path = QueenOyobeeProjectile.ExpandCrownPath(boss.Arena, path);
            telegraph = QueenOyobeeProjectile.CreateTelegraph(this, path);
        }
        // Two horizontal sweeps signal snow; two vertical lifts signal the crown.
        for (int repeat = 0; repeat < 2; repeat++)
        {
            for (int frame = 0; frame < 4; frame++)
            {
                if (scepter != null)
                {
                    scepter.sprite = scepterFrames[ice ? (frame == 1 ? 2 : 0) : (frame == 1 ? 1 : 0)];
                    scepter.transform.localRotation = Quaternion.Euler(0f, 0f,
                        ice && frame == 1 ? 15f : 0f);
                    scepter.transform.localPosition = new Vector3(1.125f,
                        0.25f + (!ice && frame == 1 ? 0.5f : 0f), 0f);
                }
                yield return FreezerVenusBoss.WaitUnpaused(ice ? 0.065f : 0.09f);
            }
        }
        if (scepter != null)
        {
            scepter.sprite = scepterFrames[0];
            scepter.transform.localRotation = Quaternion.identity;
            scepter.transform.localPosition = new Vector3(1.375f, 0.25f, 0f);
        }
        projectiles.RemoveAll(p => p == null);
        if (ice)
        {
            var destinations = QueenOyobeeProjectile.GroundSnowDestinations(boss.Arena, boss.GroundPosition);
            for (int i = 0; i < 6; i++)
            {
                if (i < destinations.Count)
                    projectiles.Add(QueenOyobeeProjectile.CreateGroundSnow(this, i, destinations[i]));
                projectiles.Add(QueenOyobeeProjectile.Create(this, false, i));
            }
            yield return FreezerVenusBoss.WaitUnpaused(0.3f);
        }
        else
        {
            if (wornCrown != null) wornCrown.enabled = false;
            boss.SetFrame(crownlessFrames, 0);
            var crown = QueenOyobeeProjectile.Create(this, true, 0, path, crownFlightSeconds);
            projectiles.Add(crown);
            while (crown != null) yield return null;
            if (telegraph != null) Destroy(telegraph);
            if (wornCrown != null) wornCrown.enabled = true;
            boss.SetFrame(idleFrames, 0);
        }
        yield return FreezerVenusBoss.WaitUnpaused(0.18f);
    }

    public void ClearProjectiles()
    {
        if (telegraph != null) Destroy(telegraph);
        if (iceWave != null) Destroy(iceWave.gameObject);
        foreach (var projectile in projectiles)
            if (projectile != null) Destroy(projectile.gameObject);
        projectiles.Clear();
        if (scepter != null) scepter.enabled = false;
        if (wornCrown != null) wornCrown.enabled = true;
    }

    public void SyncVisibility()
    {
        if (scepter != null) scepter.enabled = boss.CombatActive && boss.body.enabled;
        if (wornCrown != null) wornCrown.gameObject.SetActive(boss.body.enabled);
    }

    private void LateUpdate()
    {
        if (Active) SyncVisibility();
    }

    private void OnDisable() => ClearProjectiles();
}
