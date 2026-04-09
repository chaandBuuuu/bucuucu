using System;

/// <summary>
/// Fast lookup reference cho tất cả gameplay constants
/// </summary>
public static class GameplayConstants
{
    // Character Stats
    public const float HUNT1_HEALTH = 100f;
    public const float HUNT1_SPEED = 4.5f;
    public const float HUNT2_HEALTH = 100f;
    public const float HUNT2_SPEED = 5f;

    public const float SURVIVAL1_HEALTH = 80f;
    public const float SURVIVAL1_SPEED = 5.5f;
    public const float SURVIVAL2_HEALTH = 85f;
    public const float SURVIVAL2_SPEED = 5.2f;
    public const float SURVIVAL3_HEALTH = 90f;
    public const float SURVIVAL3_SPEED = 5f;
    public const float SURVIVAL4_HEALTH = 75f;
    public const float SURVIVAL4_SPEED = 5f;

    // Status Effect Durations
    public const float SLOWNESS_DURATION = 5f;
    public const float SLOWNESS_MAGNITUDE = 0.3f;
    public const float STUN_DURATION = 1.5f;
    public const float SWIFTNESS_DURATION = 3f;
    public const float SWIFTNESS_MAGNITUDE = 0.3f;
    public const float TRUE_SIGHT_DURATION = 5f;
    public const float BLINDNESS_DURATION = 3f;
    public const float CAPTAIN_BLACK_DURATION = 8f;
    public const float CAPTAIN_BLACK_REDUCTION = 0.3f;

    // Hunt #1
    public const float HUNT1_VINE_PULL_RANGE = 5f;
    public const float HUNT1_VINE_PULL_FORCE = 10f;
    public const float HUNT1_BLOOM_RADIUS = 3f;
    public const float HUNT1_DASH_DISTANCE = 5f;
    public const float HUNT1_DASH_DAMAGE = 20f;
    public const float HUNT1_ROOT_SPEED_BOOST = 0.4f;
    public const float HUNT1_ROOT_SLOWNESS = 0.3f;

    // Hunt #2
    public const float HUNT2_CONE_ANGLE = 45f;
    public const float HUNT2_CONE_DISTANCE = 8f;
    public const float HUNT2_EXPANDED_FOV = 80f;
    public const float HUNT2_BEAM_DISTANCE = 10f;
    public const float HUNT2_ORB_RADIUS = 5f;
    public const float HUNT2_MAX_ORBS = 3;

    // Survival #1
    public const int SURVIVAL1_START_MARKS = 6;
    public const int SURVIVAL1_MARK_TO_TIGER = 3;
    public const float SURVIVAL1_SWING_DAMAGE = 25f;
    public const float SURVIVAL1_SWING_RANGE = 2f;

    // Survival #2
    public const float SURVIVAL2_BOOMBOX_RANGE = 4f;
    public const float SURVIVAL2_CLAP_RANGE = 3f;
    public const float SURVIVAL2_CLAP_CONE = 90f;

    // Survival #3
    public const float SURVIVAL3_DETECT_RANGE = 8f;
    public const float SURVIVAL3_THROW_RANGE = 7f;

    // Survival #4
    public const float SURVIVAL4_TALENT_RANGE = 4f;

    // Gameplay
    public const int TOTAL_BONFIRES = 4;
    public const int WOOD_PER_BONFIRE = 5;
    public const int TOTAL_WOOD = 20;

    // Cooldowns
    public const float HUNT1_E_COOLDOWN = 8f;
    public const float HUNT1_R_COOLDOWN = 10f;
    public const float HUNT1_F_COOLDOWN = 12f;
    public const float HUNT2_E_COOLDOWN = 7f;
    public const float HUNT2_R_COOLDOWN = 15f;
    public const float HUNT2_F_COOLDOWN = 6f;
    public const float SURVIVAL_E_COOLDOWN = 2f;
    public const float SURVIVAL_R_COOLDOWN = 3f;
}

/// <summary>
/// Utility functions cho gameplay
/// </summary>
public static class GameplayUtils
{
    /// <summary>
    /// Lấy stats của character dựa trên ID
    /// </summary>
    public static (float health, float speed) GetCharacterStats(CharacterID id)
    {
        return id switch
        {
            CharacterID.Hunt1 => (GameplayConstants.HUNT1_HEALTH, GameplayConstants.HUNT1_SPEED),
            CharacterID.Hunt2 => (GameplayConstants.HUNT2_HEALTH, GameplayConstants.HUNT2_SPEED),
            CharacterID.Survival1 => (GameplayConstants.SURVIVAL1_HEALTH, GameplayConstants.SURVIVAL1_SPEED),
            CharacterID.Survival2 => (GameplayConstants.SURVIVAL2_HEALTH, GameplayConstants.SURVIVAL2_SPEED),
            CharacterID.Survival3 => (GameplayConstants.SURVIVAL3_HEALTH, GameplayConstants.SURVIVAL3_SPEED),
            CharacterID.Survival4 => (GameplayConstants.SURVIVAL4_HEALTH, GameplayConstants.SURVIVAL4_SPEED),
            _ => (100f, 5f)
        };
    }

    /// <summary>
    /// Format time thành string
    /// </summary>
    public static string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{mins:D2}:{secs:D2}";
    }

    /// <summary>
    /// Tính toán distance giữa 2 điểm
    /// </summary>
    public static float GetDistance(UnityEngine.Vector2 a, UnityEngine.Vector2 b)
    {
        return UnityEngine.Vector2.Distance(a, b);
    }

    /// <summary>
    /// Check nếu điểm nằm trong cone
    /// </summary>
    public static bool IsInCone(UnityEngine.Vector2 origin, UnityEngine.Vector2 direction, UnityEngine.Vector2 point, float coneAngle, float distance)
    {
        float dist = GetDistance(origin, point);
        if (dist > distance) return false;

        UnityEngine.Vector2 dirToPoint = (point - origin).normalized;
        float angle = UnityEngine.Vector2.Angle(direction, dirToPoint);

        return angle <= coneAngle / 2f;
    }
}
