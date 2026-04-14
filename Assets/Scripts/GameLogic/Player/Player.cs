namespace GameLogic.Player
{
    public class Player
    {
        private ulong _id;
        public ulong Id => _id;

        public Player(ulong id)
        {
            _id = id;
        }
    }
}