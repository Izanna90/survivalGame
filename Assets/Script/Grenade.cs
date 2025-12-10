using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float fuseTime = 2.5f;
    public bool explodeOnImpact = true;     // explode when touching something
    public bool useFuseFallback = false;    // optional: still explode after fuseTime if never hits
    public float explosionRadius = 4f;
    public float explosionForce = 400f;
    public LayerMask affectLayers = ~0; // all layers by default
    public GameObject explosionPrefab;
    public float explosionVfxLifetime = 3f;
    public AudioClip explosionSfx;
    [Range(0f, 2f)] public float explosionSfxVolume = 1f;

    bool exploded;

    void OnEnable()
    {
        if (useFuseFallback)
        {
            Invoke(nameof(Explode), fuseTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnImpact || exploded) return;

        // Optional: filter by layer (ground/enemy/player) or just explode on any hit
        Explode();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!explodeOnImpact || exploded) return;

        // If your grenade uses triggers (e.g., for enemies), explode here too
        Explode();
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;
        CancelInvoke(); // cancel fallback fuse if any

        // Simple physics push
        Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius, affectLayers);
        foreach (var c in cols)
        {
            // Apply damage to player if present
            if (c.CompareTag("Player"))
            {
                var ph = c.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(1);
            }

            // Apply damage to enemy if present
            if (c.CompareTag("Enemy"))
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

        // Spawn explosion VFX at impact position
        if (explosionPrefab != null)
        {
            // Optional: align to ground normal if close to ground
            Vector3 pos = transform.position;
            Quaternion rot = Quaternion.identity;
            if (Physics.Raycast(pos + Vector3.up * 0.2f, Vector3.down, out var hit, 2f))
            {
                rot = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90f, 0f, 0f); // face up using normal
                pos = hit.point;
            }

            var vfx = Instantiate(explosionPrefab, pos, rot);
            if (explosionVfxLifetime > 0f) Destroy(vfx, explosionVfxLifetime);
        }

        // Play explosion sound at position using a temp source for reliable volume
        if (explosionSfx != null)
        {
            var go = new GameObject("ExplosionAudio");
            go.transform.position = transform.position;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D; set to 1f for 3D positional
            src.volume = Mathf.Clamp(explosionSfxVolume, 0f, 2f);
            src.clip = explosionSfx;
            src.loop = false;
            src.pitch = 1f;
            src.outputAudioMixerGroup = null;
            src.Play();
            Object.Destroy(go, explosionSfx.length + 0.1f);
        }

        Destroy(gameObject);
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}
