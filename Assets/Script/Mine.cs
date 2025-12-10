using UnityEngine;

public class Mine : MonoBehaviour
{
    [Header("Explosion")]
    public float explosionRadius = 4f;
    public float explosionForce = 400f;
    public LayerMask affectLayers = ~0;
    public GameObject explosionPrefab;
    public float explosionVfxLifetime = 3f;
    public AudioClip explosionSfx;
    [Range(0f, 2f)] public float explosionSfxVolume = 1f;

    [Header("Detection")]
    public string playerTag = "Player";
    public string enemyTag = "Enemy";
    public bool explodeOnAnyCollider = false; // if false, only player/enemy trigger it

    bool exploded;

    void Reset()
    {
        // Ensure we have a trigger collider
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (exploded) return;

        if (explodeOnAnyCollider || other.CompareTag(playerTag) || other.CompareTag(enemyTag))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        // Damage and physics push (same as Grenade)
        Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius, affectLayers);
        foreach (var c in cols)
        {
            if (c.CompareTag(playerTag))
            {
                var ph = c.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(1);
            }
            if (c.CompareTag(enemyTag))
            {
                var enemy = c.GetComponentInParent<Ennemy>();
                if (enemy == null) enemy = c.GetComponent<Ennemy>();
                if (enemy != null) enemy.TakeDamage(1);
            }
            var rb = c.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        // VFX
        if (explosionPrefab != null)
        {
            Vector3 pos = transform.position;
            Quaternion rot = Quaternion.identity;
            if (Physics.Raycast(pos + Vector3.up * 0.2f, Vector3.down, out var hit, 2f))
            {
                rot = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90f, 0f, 0f);
                pos = hit.point;
            }
            var vfx = Instantiate(explosionPrefab, pos, rot);
            if (explosionVfxLifetime > 0f) Destroy(vfx, explosionVfxLifetime);
        }

        // Play explosion sound at position using a temp source
        if (explosionSfx != null)
        {
            var go = new GameObject("ExplosionAudio");
            go.transform.position = transform.position;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = Mathf.Clamp(explosionSfxVolume, 0f, 2f);
            src.clip = explosionSfx;
            src.loop = false;
            src.Play();
            Object.Destroy(go, explosionSfx.length + 0.1f);
        }

        Destroy(gameObject);
    }
}
