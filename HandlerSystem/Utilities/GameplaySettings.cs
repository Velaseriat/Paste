using UnityEngine;

namespace Felsan.Scripts.Shared.Utilities
{
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "Felsan/Gameplay Settings")]
    public class GameplaySettings : ScriptableObject
    {
        [SerializeField] private string data;
    }
}