using CoreDomain.Scripts.CoreInitiator.Base;

namespace CoreDomain.GameDomain.Scripts.States.GamePlayState
{
    public class GamePlayInitatorEnterData : IInitiatorEnterData
    {
        public int LevelNumberToEnter;

        public GamePlayInitatorEnterData(int levelNumberToEnter)
        {
            LevelNumberToEnter = levelNumberToEnter;
        }
    }
}
