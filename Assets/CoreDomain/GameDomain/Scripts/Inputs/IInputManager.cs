using UnityEngine;
namespace CoreDomain.GameDomain.Scripts.Inputs {
    public interface IInputManager {
        void EnablePLayerInput();
        void DisablePLayerInput();
        void EnableUIInput();
        void DisableUIInput();
        void Setup();
    }
}
