
using System;
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

    #region Protocol Specification
    /// <summary>
    /// The version of the protocol
    /// </summary>
    public readonly int ProtocolVersion = 2;

    /// <summary>
    /// The least compatible version of the protocol
    /// </summary>
    public readonly int CompatProtocolVersion = 2;

    /// <summary>
    /// The flag that enables lossless mode
    /// </summary>
    public readonly byte FlagLosslessMode = 0x01;

    /// <summary>
    /// The flag that requests the target end to finish the connection
    /// </summary>
    public readonly byte FlagFin = 0x02;

    /// <summary>
    /// The flag that requests the target end to synchronize sequence numbers
    /// </summary>
    public readonly byte FlagSyn = 0x04;

    /// <summary>
    /// The flag that requests the target end to reset connection
    /// </summary>
    public readonly byte FlagRst = 0x08;

    /// <summary>
    /// The flag that requests the target end to push the buffered data directly instead of waiting other segments to arrive
    /// </summary>
    public readonly byte FlagPsh = 0x10;

    /// <summary>
    /// The flag that tells the target end the acknowledgement sequence ID is included within the header
    /// </summary>
    public readonly byte FlagAck = 0x20;
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
        /*
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
            uint targetEventId = BytesToUint32(buffer, 0);
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
                                string stringData = BytesToString(buffer, 0);
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
        */
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


    //
    // Data encoding
    //

    public int CalculateSegmentsCount(byte packetFlags, int bufferLen)
    {
        int headerLen = 8; //target player ID (ushort) + sequenceId (ushort) + flags (byte) + window size (byte)
        int availableSize = networkFrameSize - headerLen;
        return (int)Mathf.Ceil(bufferLen / availableSize);
    }

    //TODO: Improve the payload encoding
    /// <summary>
    /// Encodes specified data into a synchronizable Base64 string
    /// </summary>
    /// <returns>Base64 string</returns>
    public string Encode(uint eventId, byte packetFlags, int targetPlayerId, byte[] buffer, int bufferLen, byte segmentIndex)
    {
        return null;
        /*
        byte[] arr = new byte[networkFrameSize];
        int offset = 0;

        byte[] eventIdArr = Uint32ToBytes(eventId);
        for (int i = 0; i < eventIdArr.Length; i++)
        {
            arr[offset + i] = eventIdArr[i];
        }
        offset += eventIdArr.Length;

        arr[offset++] = packetFlags;

        if ((packetFlags & PacketTargetedPlayer) != 0)
        {
            byte[] targetPlayerArr = Int32ToBytes(targetPlayerId);
            for (int i = 0; i < targetPlayerArr.Length; i++)
            {
                arr[offset + i] = targetPlayerArr[i];
            }
            offset += targetPlayerArr.Length;
        }

        int availableSize = networkFrameSize - offset;

        if ((packetFlags & PacketSegmentedPacket) != 0)
        {
            int count = CalculateSegmentsCount(packetFlags, bufferLen);

            if (segmentIndex < 0 || segmentIndex >= count)
            {
                Debug.LogError(string.Format("[UdonNet] Cannot encode UdonNetData because wrong segment index is provided: {0}/{1}", segmentIndex, count));
                return null;
            }

            if (segmentIndex == 0)
            {
                arr[5] |= PacketSynchronizeSequenceNumber;
            }

            arr[offset++] = segmentIndex;

            int segmentMaxSize = availableSize - 1;
            int bufferOffset = segmentMaxSize * segmentIndex;
            int remain = bufferLen - bufferOffset;
            ushort segmentLen = remain > segmentMaxSize ? (ushort)segmentMaxSize : (ushort)remain;

            byte[] segmentLenArr = UshortToBytes(segmentLen);
            arr[offset++] = segmentLenArr[0];
            arr[offset++] = segmentLenArr[1];

            for (int i = bufferOffset; i < segmentLen; i++)
            {
                arr[offset + i] = buffer[i];
            }

            if (remain == 0)
            {
                arr[5] |= PacketFinish;
            }
        }
        else
        {
            if (availableSize < bufferLen)
            {
                Debug.LogError(string.Format("[UdonNet] Cannot encode UdonNetData because byte buffer length is too long (>{0} bytes): {1}/{2}", networkFrameSize, bufferLen, availableSize));
                return null;
            }

            byte[] bufferLenArr = UshortToBytes((ushort)bufferLen);
            arr[offset++] = bufferLenArr[0];
            arr[offset++] = bufferLenArr[1];

            for (int i = 0; i < bufferLen; i++)
            {
                arr[offset + i] = buffer[i];
            }
        }


        return Convert.ToBase64String(arr);
        */
    }

    /// <summary>
    /// Decodes the specified Base64 string to UdonNetData
    /// </summary>
    /// <param name="rawData">Raw Base64 data string</param>
    /// <returns> decoded UdonNetData</returns>
    public object[] Decode(string rawData)
    {
        return null;
        /*
        byte[] arr = Convert.FromBase64String(rawData);

        object[] udonNetData = new object[4];

        int offset = 0;

        udonNetData[0] = BytesToUint32(arr, offset);
        offset += 4;

        udonNetData[1] = arr[offset];
        offset += 1;

        if ((arr[4] & PacketTargetedPlayer) != 0)
        {
            udonNetData[2] = BytesToInt32(arr, offset);
            offset += 4;
        }
        else
        {
            udonNetData[2] = null;
        }

        ushort bufferLen = BytesToUshort(arr, offset);
        offset += 2;

        byte[] buffer = new byte[bufferLen];
        for (int i = 0; i < bufferLen; i++)
        {
            buffer[i] = arr[offset + i];
        }
        udonNetData[3] = buffer;

        return udonNetData;
        */
    }

    //
    // Byte operations
    //

    public byte[] UshortToBytes(ushort x)
    {
        ushort bits = 8;
        byte[] output = new byte[2];
        output[0] = (byte)(x >> bits);
        output[1] = (byte)x;
        return output;
    }

    public byte[] ShortToBytes(short x)
    {
        short bits = 8;
        byte[] output = new byte[2];
        output[0] = (byte)(x >> bits);
        output[1] = (byte)x;
        return output;
    }

    public byte[] Uint32ToBytes(uint x)
    {
        byte[] output = new byte[4];
        output[0] = (byte)(x >> 24);
        output[1] = (byte)(x >> 16);
        output[2] = (byte)(x >> 8);
        output[3] = (byte)x;
        return output;
    }

    public byte[] Int32ToBytes(int x)
    {
        byte[] output = new byte[4];
        output[0] = (byte)(x >> 24);
        output[1] = (byte)(x >> 16);
        output[2] = (byte)(x >> 8);
        output[3] = (byte)x;
        return output;
    }

    public byte[] StringToBytes(string str)
    {
        //This only encodes ASCII
        char[] arr = str.ToCharArray();
        byte[] output = new byte[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            output[i] = Convert.ToByte(arr[i]);
        }
        return output;
    }

    public ushort BytesToUshort(byte[] bytes, int offset)
    {
        ushort output = 0;
        output |= (ushort)(bytes[offset] << 8);
        output |= bytes[offset + 1];
        return output;
    }

    public short BytesToShort(byte[] bytes, int offset)
    {
        short output = 0;
        output |= (short)(bytes[offset] << 8);
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
        output |= bytes[offset + 1];
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
        return output;
    }

    public uint BytesToUint32(byte[] bytes, int offset)
    {
        uint output = 0;
        output |= (uint)bytes[offset] << 24;
        output |= (uint)bytes[offset + 1] << 16;
        output |= (uint)bytes[offset + 2] << 8;
        output |= bytes[offset + 3];
        return output;
    }

    public int BytesToInt32(byte[] bytes, int offset)
    {
        int output = 0;
        output |= bytes[offset] << 24;
        output |= bytes[offset + 1] << 16;
        output |= bytes[offset + 2] << 8;
        output |= bytes[offset + 3];
        return output;
    }

    public string BytesToString(byte[] bytes, int offset)
    {
        string output = "";
        for (int i = offset; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b == 0x00) //null
            {
                break;
            }
            output += Convert.ToChar(b);
        }
        return output;
    }
}
