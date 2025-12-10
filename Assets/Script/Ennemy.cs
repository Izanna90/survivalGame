using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Ennemy : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 12f;
    public float loseRadius = 14f; // slightly larger to reduce toggling
    public string playerTag = "Player";

    [Header("Chase")]
    public float chaseSpeed = 4f;
    public float returnSpeed = 3f;
    public float stoppingDistance = 1.5f;

    [Header("Grenades")]
    public GameObject grenadePrefab; // prefab with Rigidbody and explosion logic
    public Transform throwOrigin;    // optional hand/socket; defaults to enemy position
    public float throwCooldown = 2.5f;
    public float throwForce = 12f;   // fallback horizontal force
    public float upForce = 4f;       // fallback vertical arc
    public float minThrowRange = 5f; // don't throw too close
    public float maxThrowRange = 25f;
    public float launchAngleDeg = 35f; // ballistic launch angle

    [Header("Health")]
    public int maxHealth = 2;
    public int currentHealth;
    public Animator animator;

    // Ragdoll parts
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    private NavMeshAgent agent;
    private Transform player;
    private Vector3 origin;
    private bool isChasing;
    private float lastThrowTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        origin = transform.position;
        agent.stoppingDistance = stoppingDistance;

        // Place agent onto NavMesh at start
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origin, out hit, 2f, NavMesh.AllAreas))
        {
            origin = hit.position;
            agent.Warp(origin);
        }
        else
        {
            Debug.LogWarning("Enemy origin not on NavMesh. Move the enemy onto a baked NavMesh.", this);
        }

        // Find player by tag
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) player = playerObj.transform;

        // Ensure throw origin
        if (throwOrigin == null)
        {
            // Create a simple throw origin above a capsule enemy
            GameObject t = new GameObject("ThrowOrigin");
            t.transform.SetParent(transform);
            float h = 1.5f; // default height; override if your capsule is taller
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule) h = Mathf.Max(1f, capsule.height * 0.75f);
            t.transform.localPosition = new Vector3(0f, h, 0f);
            throwOrigin = t.transform;
        }

        currentHealth = maxHealth;
        if (animator == null) animator = GetComponent<Animator>();

        // Cache ragdoll parts (exclude root)
        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);
        SetRagdollEnabled(false);
    }

    void Update()
    {
        // Stop logic if dead
        if (currentHealth <= 0) return;

        if (player == null) return;

        // If agent is not ready or not on NavMesh, try to recover
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            // Attempt to snap back to nearest NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                return; // skip this frame until we are on the NavMesh
            }
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // State switch with hysteresis
        if (!isChasing && distToPlayer <= detectionRadius)
        {
            isChasing = true;
            agent.speed = chaseSpeed;
        }
        else if (isChasing && distToPlayer > loseRadius)
        {
            isChasing = false;
            agent.speed = returnSpeed;
        }

        if (isChasing)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            TryThrowGrenade(distToPlayer);
        }
        else
        {
            agent.speed = returnSpeed;
            agent.SetDestination(origin);
            // Optionally stop when back to origin
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.ResetPath();
            }
        }
    }

    void TryThrowGrenade(float distToPlayer)
    {
        if (grenadePrefab == null) return;
        if (Time.time - lastThrowTime < throwCooldown) return;
        if (distToPlayer < minThrowRange || distToPlayer > maxThrowRange) return;

        Vector3 originPos = throwOrigin.position;
        Vector3 target = player.position;

        // Aim throw origin toward player so forward offset is correct
        Vector3 aimDir = (target - originPos);
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude > 0.0001f)
            throwOrigin.rotation = Quaternion.LookRotation(aimDir.normalized, Vector3.up);

        // Small forward/up offset to avoid overlapping enemy or ground
        float spawnForwardOffset = 0.3f;
        float spawnUpOffset = 0.2f;
        originPos += throwOrigin.forward * spawnForwardOffset + Vector3.up * spawnUpOffset;

        // Debug line to visualize intended target
        Debug.DrawLine(originPos, target, Color.green, 1.0f);

        // Planar distance and height difference
        Vector3 toTarget = target - originPos;
        float y = toTarget.y;
        toTarget.y = 0f;
        float d = toTarget.magnitude;

        // Compute ballistic velocity for a fixed angle
        float g = Mathf.Abs(Physics.gravity.y); // use project gravity
        float angleRad = Mathf.Deg2Rad * Mathf.Clamp(launchAngleDeg, 10f, 60f);
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);

        // v^2 = g*d^2 / (2*cos^2*(d*tan(angle) - y))
        float denom = 2f * cos * cos * (d * Mathf.Tan(angleRad) - y);

        Vector3 launchVel;

        if (d > 0.1f && denom > 0.0001f)
        {
            float v2 = (g * d * d) / denom;
            if (v2 > 0f)
            {
                float v = Mathf.Sqrt(v2);
                // Direction in plane towards target
                Vector3 dir = toTarget.normalized;
                // Decompose velocity along dir and up based on angle
                launchVel = dir * (v * cos) + Vector3.up * (v * sin);
            }
            else
            {
                // Invalid solution, fallback
                launchVel = toTarget.normalized * throwForce + Vector3.up * upForce;
            }
        }
        else
        {
            // Too close or angle invalid, fallback
            launchVel = toTarget.normalized * throwForce + Vector3.up * upForce;
        }

        // Diagnostics: planar values
        Vector2 targetXZ = new Vector2(target.x, target.z);
        Vector2 playerXZ = new Vector2(player.position.x, player.position.z);
        Vector2 originXZ = new Vector2(originPos.x, originPos.z);
        Vector2 launchVelXZ = new Vector2(launchVel.x, launchVel.z);

        // Estimate landing on flat ground at originPos.y (time to hit ground using vertical motion)
        float vY = launchVel.y;
        float h0 = 0f; // start height reference
        float hTarget = y; // relative target height vs origin
        // Time to return to origin height: t = (2*vY)/g (if starting and ending at same height)
        // Use this simple estimate to get an idea of planar landing point:
        float tFlat = Mathf.Max(0f, (2f * Mathf.Max(0f, vY)) / g);
        Vector2 estimatedLandingXZ = originXZ + launchVelXZ * tFlat;

        Debug.Log(
            $"[Grenade Debug] targetXZ={targetXZ:F2}, playerXZ={playerXZ:F2}, originXZ={originXZ:F2}, " +
            $"planarDist={d:F2}, heightDelta={y:F2}, launchVelXZ={launchVelXZ:F2}, vY={vY:F2}, estLandingXZ={estimatedLandingXZ:F2}",
            this
        );

        // Spawn grenade
        GameObject grenade = Instantiate(grenadePrefab, originPos, Quaternion.identity);

        // Ignore collisions between grenade and enemy colliders
        var grenadeCol = grenade.GetComponent<Collider>();
        if (grenadeCol != null)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (col.enabled) Physics.IgnoreCollision(grenadeCol, col, true);
            }
        }

        var rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Ensure sensible physics settings
            rb.useGravity = true;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.velocity = launchVel; // set initial velocity
            rb.angularVelocity = Vector3.zero;
        }

        // Log final launch velocity for verification
        Debug.Log($"[Grenade Debug] finalLaunchVel={launchVel:F2}", this);

        lastThrowTime = Time.time;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Disable AI/animation
        if (animator) animator.enabled = false;
        if (agent)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        // Enable ragdoll physics
        SetRagdollEnabled(true);

        // Optional: add a small impulse
        foreach (var rb in ragdollBodies)
        {
            if (rb != null && rb != GetComponent<Rigidbody>())
            {
                rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
            }
        }
    }

    void SetRagdollEnabled(bool enabled)
    {
        foreach (var rb in ragdollBodies)
        {
            if (rb == null) continue;
            if (rb == GetComponent<Rigidbody>()) continue; // skip root if present
            rb.isKinematic = !enabled;
        }
        foreach (var col in ragdollColliders)
        {
            if (col == null) continue;
            if (col == GetComponent<Collider>()) continue; // skip root collider
            col.enabled = enabled;
        }
        // Ensure root collider remains for nav/selection before death; after death it's fine to keep
        var rootCol = GetComponent<Collider>();
        if (rootCol) rootCol.enabled = !enabled;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRadius);
        
        // Visualize throw origin
        if (throwOrigin != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(throwOrigin.position, 0.1f);
            Gizmos.DrawLine(throwOrigin.position, throwOrigin.position + Vector3.forward * 0.5f);
        }
    }
}
