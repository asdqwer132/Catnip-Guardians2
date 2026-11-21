using UnityEngine;

public sealed class BattlePlayerManager : MonoBehaviour
{
    [SerializeField] private Player player;

    public PlayerData CurrentPlayer => player != null ? player.CurrentPlayerData : null;

    public void Prepare(PlayerData selectedPlayer) => player?.ApplyPlayerData(selectedPlayer);
}
