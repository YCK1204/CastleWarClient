using System.Collections.Concurrent;
using CWFramework;

namespace GameLogic.Player
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        private ConcurrentDictionary<ulong, Player> _players = new ConcurrentDictionary<ulong, Player>();

        public void RegisterPlayer(Player player)
        {
            _players.TryAdd(player.Id, player);
        }

        public bool UnRegisterPlayer(Player player)
        {
            if (player == null)
                return false;
            _players.TryRemove(player.Id, out player);
            return player != null;
        }

        public bool UnRegisterPlayer(ulong id)
        {
            return _players.TryRemove(id, out Player player);
        }
        
        public Player GetPlayer(ulong id)
        {
            return _players.TryGetValue(id, out Player player) ? player : null;
        }
    }
}