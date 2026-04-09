using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // Racing Controls
    public Vector2 MoveDirection;  // WASD input
    public bool    IsDrifting;     // Shift key
    public bool    UsePowerup;     // Q key
    
    // Legacy (keeping for compatibility)
    public Vector2 Direction;
    public bool    IsPausing;
    public bool    PressE;         // E key
    public bool    PressR;         // R key
    public bool    PressF;         // F key
}
