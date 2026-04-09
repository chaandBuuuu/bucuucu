using UnityEngine;

/// <summary>
/// Racing game constants and configuration
/// </summary>
public static class RacingConstants
{
    // Car Movement
    public const float CAR_ACCELERATION = 8f;
    public const float CAR_MAX_SPEED = 15f;
    public const float CAR_FRICTION = 0.95f;
    public const float CAR_DRIFT_FRICTION = 0.92f;
    public const float CAR_ROTATION_SPEED = 180f;
    public const float CAR_DRIFT_ROTATION_MULTIPLIER = 1.5f;

    // Race Configuration
    public const int RACE_LAPS_TO_WIN = 4;
    public const int MAX_PLAYERS = 4;

    // Powerup Settings
    public const float SHIELD_DURATION = 3f;
    public const float TRAP_SLOW_AMOUNT = 0.6f;
    public const float TRAP_SLOW_DURATION = 3f;
    public const float TRAP_RESPAWN_TIME = 10f;
    public const float TRAP_LIFETIME = 15f;
    public const float GUN_SLOW_AMOUNT = 0.5f;
    public const float GUN_SLOW_DURATION = 3f;
    public const float SPEED_BOOST_DURATION = 5f;
    public const float SPEED_BOOST_MULTIPLIER = 1.5f;

    // Powerup Pickup
    public const float POWERUP_RESPAWN_TIME = 10f;
    public const float POWERUP_PICKUP_RANGE = 1f;

    // Bullet
    public const float BULLET_SPEED = 20f;
    public const float BULLET_LIFETIME = 5f;

    // UI
    public const string LAP_FORMAT = "Lap: {0}/{1}";
    public const string SPEED_FORMAT = "Speed: {0:F1}";
    public const string TIMER_FORMAT = "{0:00}:{1:00}";
}
