using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using Zenject;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Datas
{
    public class AudioPoolWarmup : IInitializable
    {
        private readonly IAudioSourcePool _pool;
        private readonly int _count;

        public AudioPoolWarmup(IAudioSourcePool pool, int count)
        {
            _pool = pool;
            _count = count;
        }

        public void Initialize() => _pool.Prewarm(_count);
    }
}