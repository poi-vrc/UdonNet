
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UdonNetController : UdonSharpBehaviour
{
    [Header("Create your pool of NetworkedUNPlayer")]
    public NetworkedUNPlayer[] pool;

    [Header("Network Frame Size (Default 37 bytes, too large will block the network)")]
    public int networkFrameSize = 37;

    void Start()
    {

    }

    public void Handle(VRCPlayerApi player, object[] udonNetData)
    {
        Debug.Log(string.Format("Handling! {0},{1},{2},{3}", udonNetData[0], udonNetData[1], udonNetData[2], udonNetData[3]));
    }

    public NetworkedUNPlayer GetLocalUNPlayer()
    {
        return GetNetworkedUNPlayer(Networking.LocalPlayer);
    }

    public NetworkedUNPlayer GetNetworkedUNPlayer(VRCPlayerApi player)
    {
        if (pool == null)
        {
            return null;
        }

        if (Networking.IsOwner(gameObject) && player.playerId == Networking.LocalPlayer.playerId)
        {
            return pool[0];
        }

        for (int i = 1; i < pool.Length; i++)
        {
            if (Networking.IsOwner(player, pool[i].gameObject))
            {
                return pool[i];
            }
        }

        return null;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            Debug.Log("player join event detected. assigning one UNPlayer to player");

            bool assigned = false;
            for (int i = 1; i < pool.Length; i++)
            {
                if (Networking.IsOwner(pool[i].gameObject))
                {
                    assigned = true;
                    Networking.SetOwner(player, pool[i].gameObject);
                    break;
                }
            }

            if (!assigned)
            {
                Debug.Log("Warning: Pool is full. No any other available GameObjects for assignment! Some players may not be able to send game events.");
            }
        }
    }
}
