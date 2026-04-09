using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Direction;
    public Vector2 MoveDirection;  // For gameplay
    public bool    IsPausing;
    public bool    PressE;         // For abilities
    public bool    PressR;
    public bool    PressF;
}
