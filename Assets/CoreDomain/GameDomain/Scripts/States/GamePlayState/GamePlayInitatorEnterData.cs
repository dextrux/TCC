using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Services.NetworkService;

namespace CoreDomain.GameDomain.Scripts.States.GamePlayState
{
    public class GamePlayInitatorEnterData : IInitiatorEnterData
    {
        public readonly string LevelToEnter;
        public readonly NetworkSessionMode SessionMode;
        public readonly string HostProductUserId;

        public GamePlayInitatorEnterData(string levelToEnter, NetworkSessionMode sessionMode = NetworkSessionMode.Offline, string hostProductUserId = "")
        {
            LevelToEnter = levelToEnter;
            SessionMode = sessionMode;
            HostProductUserId = hostProductUserId;
        }
    }
}