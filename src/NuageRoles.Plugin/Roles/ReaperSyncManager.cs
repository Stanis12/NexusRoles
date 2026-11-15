using NuageRoles;
using Photon.Pun;
using UnityEngine;

namespace NuageRoles.Roles
{
    public class ReaperSyncManager : MonoBehaviourPunCallbacks
    {
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            var localAvatar = SemiFunc.PlayerAvatarGetFromSteamID(PlayerController.instance.playerSteamID);
            var rMan = ((Component)localAvatar).GetComponent<ReaperManager>();
            if (rMan != null && rMan.isReaper)
            {
                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    NuageRolesPlugin.Logger.LogWarning($"Sending late-joiner Reaper status RPC to {newPlayer.NickName}");
                    rMan.photonView.RPC("setReaperStatusRPC", newPlayer, PlayerController.instance.playerSteamID, true);
                }
            }
        }
    }
}
