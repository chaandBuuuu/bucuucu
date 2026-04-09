using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Detect lap completion
/// </summary>
public class FinishLineDetector : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    private HashSet<CarController> _carsCrossedThisLap = new HashSet<CarController>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var carController = collision.GetComponent<CarController>();
        if (carController == null || raceManager == null) return;

        raceManager.RegisterLapCompletion(carController);
    }
}
