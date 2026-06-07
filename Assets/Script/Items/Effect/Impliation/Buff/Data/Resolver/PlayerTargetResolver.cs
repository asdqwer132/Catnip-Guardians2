using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTargetResolver", menuName = "Game/Buff Target/Player")]
public class PlayerTargetResolver : BuffTargetResolver
{
    public bool targetAllPlayers = true;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results == null)
            return;

        if (targetAllPlayers)
        {
            results.Add(BuffTargetHandle.AllPlayers());
            return;
        }

        Player player = null;

        if (context != null && context.owner != null)
            player = context.owner.GetComponent<Player>();

        if (player == null)
            return;

        results.Add(BuffTargetHandle.Player(player));
    }
}