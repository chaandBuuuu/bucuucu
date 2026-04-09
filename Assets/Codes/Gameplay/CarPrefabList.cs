using UnityEngine;
using Fusion;                    // ← THÊM DÒNG NÀY

[CreateAssetMenu(menuName = "Racing/Car Prefab List", fileName = "CarPrefabList")]
public class CarPrefabList : ScriptableObject
{
    [Tooltip("0 = Hacker, 1 = Ghost Hunter, 2 = Priest, 3 = Scientist")]
    public NetworkObject[] carPrefabs = new NetworkObject[4];
}