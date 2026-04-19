using UnityEngine;

/// <summary>
/// Racing game constants and configuration — source of truth duy nhất cho toàn project.
/// Tất cả script đã được cập nhật dùng các hằng số này thay vì hardcode.
/// </summary>
public static class RacingConstants
{
    // ── Car Movement ─────────────────────────────────────────────────────────
    public const float CAR_ACCELERATION              = 12f;    // ✅ INCREASED from 8 → 12 (50% faster)
    public const float CAR_MAX_SPEED                 = 22f;    // ✅ INCREASED from 15 → 22 (47% faster)
    public const float CAR_FRICTION                  = 0.95f;
    public const float CAR_DRIFT_FRICTION            = 0.92f;
    public const float CAR_ROTATION_SPEED            = 180f;
    public const float CAR_DRIFT_ROTATION_MULTIPLIER = 1.5f;

    // ── Race Configuration ───────────────────────────────────────────────────
    public const int RACE_LAPS_TO_WIN = 1;  // ✅ FIX: 1 lap = First to cross finish line wins
    public const int MAX_PLAYERS      = 4;

    // ── Powerup Settings ─────────────────────────────────────────────────────
    public const float SHIELD_DURATION          = 3f;

    public const float TRAP_SLOW_AMOUNT         = 0.6f;  // Giảm 60% tốc độ
    public const float TRAP_SLOW_DURATION       = 3f;
    public const float TRAP_LIFETIME            = 15f;

    public const float GUN_SLOW_AMOUNT          = 0.5f;  // Giảm 50% tốc độ
    public const float GUN_SLOW_DURATION        = 3f;

    public const float SPEED_BOOST_DURATION     = 5f;
    public const float SPEED_BOOST_MULTIPLIER   = 1.5f;  // Tăng 50% tốc độ

    // ── Powerup Pickup ───────────────────────────────────────────────────────
    public const float POWERUP_RESPAWN_TIME = 10f;
    public const float POWERUP_PICKUP_RANGE = 1f;

    // ── Bullet ───────────────────────────────────────────────────────────────
    public const float BULLET_SPEED    = 20f;
    public const float BULLET_LIFETIME = 5f;

    // ── UI Format Strings ────────────────────────────────────────────────────
    public const string LAP_FORMAT   = "Lap: {0}/{1}";
    public const string SPEED_FORMAT = "Speed: {0:F1}";
    public const string TIMER_FORMAT = "{0:00}:{1:00}";
}