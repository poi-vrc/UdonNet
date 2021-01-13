
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

    [Header("Time to cooldown before sending the next packet  (in ms)")]
    public int packetCooldownTime = 1000;

    [Header("Packet queue maximum length while cooling down")]
    public int packetQueueLength = 100;

    [Header("Wait acknowledgement list maximum length")]
    public int waitAckLength = 200;

    [Header("Wait timeout for acknowledgment (in ms)")]
    public int waitAckTimeout = 10000;

    [Header("Maximum retries for packet send")]
    public int waitAckMaxRetries = 5;

    #region protocol_spec
    //
    // Protocol
    //

    public int ProtocolVersion = 1;
    public int CompatProtocolVersion = 1;

    public byte PacketLossless = 1; //0x01;
    public byte PacketTargetedPlayer = 2; //0x02;
    public byte PacketSegmentedPacket = 4; //0x04;
    public byte PacketDataTypeString = 8; //0x08;

    public byte PacketEnquiry = 16; //0x10;
    public byte PacketAcknowledgement = 32; //0x20;
    public byte PacketSynchronizeSequenceNumber = 64; //0x40;
    public byte PacketFinish = 128; //0x80;
    #endregion

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
        //TODO: UdonSharp does not support custom classes currently, so the data is packed in object[]
        uint eventId = (uint) udonNetData[0];
        byte packetType = (byte) udonNetData[1];
        int targetPlayer = (int) udonNetData[2];
        byte[] buffer = (byte[]) udonNetData[3];

        NetworkedUNPlayer self = GetLocalUNPlayer();
        if ((packetType & PacketEnquiry) != 0)
        {
            Debug.Log(string.Format("[UdonNet] Protocol enquiry received from player {0} ({1}), sending version {2}", player.displayName, player.playerId, ProtocolVersion));
            self.SendVersion(player.playerId);
        } else if ((packetType & PacketAcknowledgement) != 0) {
            uint targetEventId = self.BytesToUint32(buffer, 0);
            Debug.Log(string.Format("[UdonNet] Acknowledgement received from player {0} ({1}), clearing wait ack for event ID {2}", player.displayName, player.playerId, targetEventId));
            bool suc = self.ClearWaitAck(targetEventId);
            if (!suc)
            {
                Debug.Log("[UdonNet] Clear wait ack not successful. Such event ID does not exist in wait ack list.");
            }
        } else
        {
            if (eventListeners != null)
            {
                for (int i = 0; i < eventListeners.Length; i++)
                {
                    if (eventListeners[i] != null) //TODO: check instance type
                    {
                        Component[] insts = eventListeners[i].GetComponents(typeof(UdonBehaviour));
                        for (int j = 0; j < insts.Length; j++)
                        {
                            UdonBehaviour inst = (UdonBehaviour) insts[j];
                            
                            inst.SetProgramVariable("_udonNetReceivedData", udonNetData);
                            inst.SetProgramVariable("_udonNetFromPlayer", player);
                            inst.SetProgramVariable("_udonNetTargetedPlayer", targetPlayer);
                            
                            if ((packetType & PacketDataTypeString) != 0)
                            {
                                string stringData = self.BytesToString(buffer, 0);
                                inst.SetProgramVariable("_udonNetStringData", stringData);
                            }

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

            if ((packetType & PacketLossless) != 0)
            {
                self.SendAck(player.playerId, eventId);
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
