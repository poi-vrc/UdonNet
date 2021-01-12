
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UdonNetController : UdonSharpBehaviour
{
    private NetworkedUNPlayer[] pool;

    [Header("Event Listeners (sent via \"OnUdonNetEvent\" and \"_receivedUdonNetData\")")]
    public Component[] eventListeners;

    [Header("Network Frame Size (Default 37 bytes, too large will block the network)")]
    public int networkFrameSize = 37;

    //
    // Protocol (Udon does not support reading constants from other classes, this is a copy from NetworkedUNPlayer)
    //

    public const int ProtocolVersion = 1;
    public const int CompatProtocolVersion = 1;

    public const byte PacketLossless = 0x01;
    public const byte PacketTargetedPlayer = 0x02;
    //0x04, 0x08 reserved
    public const byte PacketEnquiry = 0x10;
    public const byte PacketAck = 0x20;
    public const byte PacketNegAck = 0x40;
    //0x80 reserved

    void Start()
    {
        int len = transform.childCount;
        pool = new NetworkedUNPlayer[len];
        for (int i = 0; i < len; i++)
        {
            pool[i] = (NetworkedUNPlayer) transform.GetChild(i).GetComponent(typeof(UdonBehaviour));
        }
    }

    public void Handle(VRCPlayerApi player, object[] udonNetData)
    {
        if (player == null)
        {
            Debug.Log("[UdonNet] handle player is null");
        }
        else
        {
            Debug.Log(string.Format("[UdonNet] handle player is not null ({0} ({1}))", player.displayName, player.playerId));
        }
        //TODO: UdonSharp does not support custom classes currently, so the data is packed in object[]
        uint eventId = (uint) udonNetData[0];
        byte packetType = (byte) udonNetData[1];
        int targetPlayer = (int) udonNetData[2];
        string stringData = (string) udonNetData[3];

        NetworkedUNPlayer self = GetLocalUNPlayer();
        if ((packetType & PacketEnquiry) != 0)
        {
            Debug.Log(string.Format("[UdonNet] Protocol enquiry received from player {0} ({1}), sending version {2}", player.displayName, player.playerId, ProtocolVersion));
            self.SendRawDataToPlayer(player.playerId, ProtocolVersion.ToString());
        } else
        {
            Debug.Log("[UdonNet] sending to event listeners");
            if (eventListeners != null)
            {
                Debug.Log(string.Format("[UdonNet] el len {0}", eventListeners.Length));
                for (int i = 0; i < eventListeners.Length; i++)
                {
                    Debug.Log(string.Format("[UdonNet] checking index {0} result {1} for null", i, eventListeners[i] == null));
                    if (eventListeners[i] != null) //TODO: check instance type
                    {
                        Component[] insts = eventListeners[i].GetComponents(typeof(UdonBehaviour));
                        Debug.Log(string.Format("[UdonNet] get components len {0}", insts.Length));
                        for (int j = 0; j < insts.Length; j++)
                        {
                            Debug.Log(string.Format("[UdonNet] typeof index {0} = {1}", j, insts[j].GetType()));
                            UdonBehaviour inst = (UdonBehaviour) insts[j];
                            
                            inst.SetProgramVariable("_udonNetReceivedData", udonNetData);
                            inst.SetProgramVariable("_udonNetFromPlayer", player);
                            inst.SetProgramVariable("_udonNetTargetedPlayer", targetPlayer);
                            inst.SetProgramVariable("_udonNetStringData", stringData);
                            if ((packetType & PacketTargetedPlayer) != 0)
                            {
                                inst.SendCustomEvent("OnUdonNetPlayerEvent");
                            }
                            else
                            {
                                inst.SendCustomEvent("OnUdonNetBroadcastEvent");
                            }
                            inst.SendCustomEvent("OnUdonNetEvent");
                        }
                    }
                }
            }
        }
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
            Debug.Log(string.Format("[UdonNet] New player {0} ({1}) joined, attempting to assign available UNPlayer from pool.", player.displayName, player.playerId));

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
                Debug.LogWarning("[UdonNet] [Warning] Pool is full. No any other available GameObjects for assignment! Some players may not be able to send game events.");
            }
        }
    }
}
