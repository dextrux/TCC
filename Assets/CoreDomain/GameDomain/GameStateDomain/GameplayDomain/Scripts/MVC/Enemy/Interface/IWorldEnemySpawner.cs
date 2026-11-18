using System;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Interface
{
    public interface IWorldEnemySpawner : IDisposable
    {
        // So o servidor/host cria inimigos
        void StartSpawning();
    }
}
