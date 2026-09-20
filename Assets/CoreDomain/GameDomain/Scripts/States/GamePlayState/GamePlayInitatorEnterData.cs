using CoreDomain.Scripts.CoreInitiator.Base;

namespace CoreDomain.GameDomain.Scripts.States.GamePlayState
{
    public class GamePlayInitatorEnterData : IInitiatorEnterData
    {
        public string LevelToEnter;

        public GamePlayInitatorEnterData(string levelNumberToEnter)
        {
            LevelToEnter = levelNumberToEnter;
        }
    }
}
