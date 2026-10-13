using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using UnityEngine;

public class LevelScenarioView : MonoBehaviour {
    [SerializeReference] public List<NoiseListener> _noiseListeners = new List<NoiseListener>();
    
    public void Setup(INoiseManager noiseManager)
    {
        foreach (var noiseListener in _noiseListeners)
        {
            noiseListener.Construct(noiseManager);
        }
    }
}
