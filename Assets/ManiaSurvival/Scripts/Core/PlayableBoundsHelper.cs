using UnityEngine;

/// <summary>
/// Shared arena bounds checks for movement, jump, launch pads, and safe prop spawning.
/// </summary>
public static class PlayableBoundsHelper
{
    private static readonly string[] ForbiddenSpawnTokens =
    {
        "water", "heaven", "hell", "lava", "pit", "portal", "recovery", "safezone",
        "manaregen", "hazard", "deathzone", "hellfire", "spawn"
    };
    private static bool loggedMissingBounds;
    private static bool cachedFallbackBounds;
    private static float fallbackMinX = -17f;
    private static float fallbackMaxX = 17f;
    private static float fallbackMinZ = -17f;
    private static float fallbackMaxZ = 17f;

    public static bool IsPositionInsidePlayableBounds(Vector3 position)
    {
        if (ArenaBounds.Instance != null)
        {
            return ArenaBounds.Instance.IsInside(position);
        }

        EnsureFallbackBounds();
        return position.x >= fallbackMinX && position.x <= fallbackMaxX
            && position.z >= fallbackMinZ && position.z <= fallbackMaxZ;
    }

    public static Vector3 ClampToPlayableBounds(Vector3 position)
    {
        if (ArenaBounds.Instance != null)
        {
            return ArenaBounds.Instance.ClampPosition(position);
        }

        EnsureFallbackBounds();
        return new Vector3(
            Mathf.Clamp(position.x, fallbackMinX, fallbackMaxX),
            position.y,
            Mathf.Clamp(position.z, fallbackMinZ, fallbackMaxZ));
    }

    public static Vector3 ConstrainHorizontalMotion(Vector3 start, Vector3 motion)
    {
        Vector3 horizontal = new Vector3(motion.x, 0f, motion.z);
        if (horizontal.sqrMagnitude <= 0.000001f)
        {
            return motion;
        }

        Vector3 predicted = start + motion;
        if (IsPositionInsidePlayableBounds(predicted))
        {
            return motion;
        }

        Vector3 clamped = ClampToPlayableBounds(predicted);
        Vector3 adjustedHorizontal = new Vector3(clamped.x - start.x, 0f, clamped.z - start.z);

        if (!IsPositionInsidePlayableBounds(start + adjustedHorizontal))
        {
            adjustedHorizontal = Vector3.zero;
        }

        return new Vector3(adjustedHorizontal.x, motion.y, adjustedHorizontal.z);
    }

    public static bool TryGetForbiddenSpawnPositionReason(Vector3 position, float radius, out string reason)
    {
        reason = string.Empty;
        Collider[] overlaps = Physics.OverlapSphere(
            position + Vector3.up * 0.5f,
            Mathf.Max(0.1f, radius),
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider col = overlaps[i];
            if (col == null)
            {
                continue;
            }

            if (TryGetForbiddenSpawnReason(col.gameObject, out reason))
            {
                return true;
            }
        }

        if (IsNearNamedMapZone(position, "Water", 2f))
        {
            reason = "water";
            return true;
        }

        if (IsNearNamedMapZone(position, "Heaven", 4f))
        {
            reason = "heaven";
            return true;
        }

        if (IsNearNamedMapZone(position, "Portal", 5f))
        {
            reason = "portal";
            return true;
        }

        if (IsNearNamedMapZone(position, "Hell", 4f))
        {
            reason = "hell";
            return true;
        }

        if (IsNearNamedMapZone(position, "Lava", 4f))
        {
            reason = "lava";
            return true;
        }

        if (IsNearNamedMapZone(position, "Spawn", 4f))
        {
            reason = "spawn";
            return true;
        }

        return false;
    }

    public static bool IsForbiddenSpawnPosition(Vector3 position, float radius = 1f)
    {
        return TryGetForbiddenSpawnPositionReason(position, radius, out _);
    }

    public static void ClampUnitIfOutside(Transform unitTransform, CharacterController controller, string reason)
    {
        if (unitTransform == null)
        {
            return;
        }

        if (IsPositionInsidePlayableBounds(unitTransform.position))
        {
            return;
        }

        if (ArenaBounds.Instance != null)
        {
            ArenaBounds.Instance.ClampUnitTransform(unitTransform, reason);
            return;
        }

        EnsureFallbackBounds();
        Vector3 clamped = ClampToPlayableBounds(unitTransform.position);
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null && controllerWasEnabled)
        {
            controller.enabled = false;
        }

        unitTransform.position = clamped;

        if (controller != null && controllerWasEnabled && controller.gameObject.activeInHierarchy)
        {
            controller.enabled = true;
        }
    }

    private static bool TryGetForbiddenSpawnReason(GameObject target, out string reason)
    {
        reason = string.Empty;
        if (target == null)
        {
            return false;
        }

        string combined = (target.name + " " + target.tag + " " + LayerMask.LayerToName(target.layer)).ToLowerInvariant();
        for (int i = 0; i < ForbiddenSpawnTokens.Length; i++)
        {
            string token = ForbiddenSpawnTokens[i];
            if (combined.Contains(token))
            {
                reason = token;
                return true;
            }
        }

        if (target.GetComponent<HeavenPortal>() != null
            || target.GetComponent<HeavenRecoveryZone>() != null
            || target.GetComponent<HellfirePitDamageZone>() != null
            || target.GetComponent<NoSpawnZone>() != null)
        {
            reason = target.name;
            return true;
        }

        return false;
    }

    private static bool IsNearNamedMapZone(Vector3 position, string token, float minDistance)
    {
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot == null)
        {
            return false;
        }

        string tokenLower = token.ToLowerInvariant();
        Transform[] transforms = mapRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || !candidate.name.ToLowerInvariant().Contains(tokenLower))
            {
                continue;
            }

            if (candidate.GetComponent<Renderer>() == null && candidate.GetComponent<Collider>() == null)
            {
                continue;
            }

            Vector3 objPos = candidate.position;
            float distance = Vector3.Distance(
                new Vector3(position.x, 0f, position.z),
                new Vector3(objPos.x, 0f, objPos.z));

            if (distance <= minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureFallbackBounds()
    {
        if (cachedFallbackBounds)
        {
            if (!loggedMissingBounds)
            {
                loggedMissingBounds = true;
                Debug.LogWarning("[Movement] ArenaBounds missing, using fallback bounds.");
            }

            return;
        }

        cachedFallbackBounds = true;
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            Bounds bounds = new Bounds(mapRoot.transform.position, Vector3.zero);
            Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                string lower = renderer.gameObject.name.ToLowerInvariant();
                if (!lower.Contains("floor") && !lower.Contains("safe") && !lower.Contains("ground"))
                {
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (bounds.size.sqrMagnitude > 0.01f)
            {
                fallbackMinX = bounds.min.x;
                fallbackMaxX = bounds.max.x;
                fallbackMinZ = bounds.min.z;
                fallbackMaxZ = bounds.max.z;
            }
        }

        if (!loggedMissingBounds)
        {
            loggedMissingBounds = true;
            Debug.LogWarning("[Movement] ArenaBounds missing, using fallback bounds.");
        }
    }
}
