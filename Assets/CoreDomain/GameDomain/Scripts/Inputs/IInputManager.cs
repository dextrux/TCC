using UnityEngine;
namespace CoreDomain.GameDomain.Scripts.Inputs {
    public interface IInputManager {
        void ChangeToPLayerInput();
        void ChangeToUIInput();
        void Setup();
    }
}
