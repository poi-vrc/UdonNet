
using System;
using System.Text;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// The class that handles sending and receiving data from and to external.
/// </summary>
public class NetworkedUNPlayer : UdonSharpBehaviour
{
    private UdonNetController udonNetController;

    [UdonSynced] private string data;
    private string lastData;

    private uint eventId = 1;

    private float lastPacketCooldownTime = -1;

    private float lastCheckWaitAckTime = -1;

    private int packetQueueHeader = 0;

    private int packetQueueTrailer = 0;

    private string[] packetQueue;

    private uint[] packetQueueEventIds;

    private bool[] packetQueueWaitAck;

    private string[] waitAckPackets;

    private uint[] waitAckEventIds;

    private float[] waitAckEventTimes;

    private int[] waitAckRetries;

    void Start()
    {
        udonNetController = (UdonNetController) transform.parent.GetComponent(typeof(UdonBehaviour));
        packetQueue = new string[udonNetController.packetQueueLength];
        packetQueueEventIds = new uint[udonNetController.packetQueueLength];
        packetQueueWaitAck = new bool[udonNetController.packetQueueLength];
        waitAckEventIds = new uint[udonNetController.waitAckLength];
        waitAckEventTimes = new float[udonNetController.waitAckLength];
        waitAckRetries = new int[udonNetController.waitAckLength];
        waitAckPackets = new string[udonNetController.waitAckLength];
    }

    void Update()
    {
        if (lastPacketCooldownTime == -1 || Time.time - lastPacketCooldownTime > udonNetController.packetCooldownTime / 1000)
        {
            lastPacketCooldownTime = Time.time;
            if (Time.timeSinceLevelLoad > 10)
            {
                SendNextQueuedPacket();
            }
        }
        if (lastCheckWaitAckTime == -1 || Time.time - lastCheckWaitAckTime > udonNetController.waitAckTimeout / 1000)
        {
            lastCheckWaitAckTime = Time.time;
            if (Time.timeSinceLevelLoad > 10)
            {
                CheckWaitAckTimeout();
            }
        }
    }

    void SendNextQueuedPacket()
    {
        if (packetQueueHeader == packetQueueTrailer)
        {
            return;
        }
        Debug.Log(string.Format("[UdonNet] Sending the next queued packet at position {0}/{1} (H{2}T{3})", packetQueueHeader, packetQueue.Length, packetQueueHeader, packetQueueTrailer));
        string packet = packetQueue[packetQueueHeader];
        uint packetEventId = packetQueueEventIds[packetQueueHeader];
        if (packetQueueWaitAck[packetQueueHeader])
        {
            Debug.Log("[UdonNet] The next queued packet requests for ack from receiver, putting into list.");
            WaitAck(packetEventId, packet);
        }
        data = packet;
        packetQueueHeader = (packetQueueHeader + 1) % packetQueue.Length;
    }

    void AddPacketToQueue(uint eventId, string encoded, bool lossless)
    {
        int i = packetQueueTrailer;
        packetQueueWaitAck[i] = lossless;
        packetQueue[i] = encoded;
        packetQueueEventIds[i] = eventId;
        packetQueueTrailer = (packetQueueTrailer + 1) % packetQueue.Length;
        Debug.Log(string.Format("[UdonNet] Packet added into queue at position {0}/{1} (H{2}T{3})", i, packetQueue.Length, packetQueueHeader, packetQueueTrailer));
    }

    public bool SendRawToPlayer(int targetPlayerId, byte[] buffer, int bufferLen)
    {
        return SendPacket(0, targetPlayerId, buffer, bufferLen, 0);
    }

    public bool SendRawToPlayerLossless(int targetPlayerId, byte[] buffer, int bufferLen)
    {
        return SendPacket(udonNetController.PacketLossless, targetPlayerId, buffer, bufferLen, 0);
    }

    public bool SendStringToPlayer(int targetPlayerId, string stringData)
    {
        byte[] buffer = StringToBytes(stringData);
        return SendPacket(
            udonNetController.PacketDataTypeString,
            targetPlayerId,
            buffer,
            buffer.Length,
            0);
    }
    
    public bool SendStringToPlayerLossless(int targetPlayerId, string stringData)
    {
        byte[] buffer = StringToBytes(stringData);
        return SendPacket(
            (byte) (udonNetController.PacketLossless | udonNetController.PacketDataTypeString),
            targetPlayerId,
            buffer,
            buffer.Length,
            0);
    }

    public bool BroadcastRaw(byte[] buffer, int bufferLen)
    {
        return SendPacket(0, 0, buffer, bufferLen, 0);
    }

    public bool BroadcastRawLossless(byte[] buffer, int bufferLen)
    {
        return SendPacket(udonNetController.PacketLossless, 0, buffer, bufferLen, 0);
    }

    public bool BroadcastString(string stringData)
    {
        byte[] buffer = StringToBytes(stringData);
        return SendPacket(
            udonNetController.PacketDataTypeString,
            0,
            buffer,
            buffer.Length,
            0);
    }

    public bool BroadcastStringLossless(string stringData)
    {
        byte[] buffer = StringToBytes(stringData);
        return SendPacket(
            (byte)(udonNetController.PacketLossless | udonNetController.PacketDataTypeString),
            0,
            buffer,
            buffer.Length,
            0);
    }

    public bool SendAck(int targetPlayerId, uint eventId)
    {
        byte[] buffer = Uint32ToBytes(eventId);
        return SendPacket(
            (byte)(
                udonNetController.PacketTargetedPlayer |
                udonNetController.PacketAcknowledgement
            ),
            targetPlayerId,
            buffer,
            buffer.Length,
            0);
    }

    void WaitAck(uint eventId, string encoded)
    {
        int pos = -1;
        for (int i = 0; i < waitAckEventIds.Length; i++)
        {
            if (waitAckEventIds[i] == 0)
            {
                pos = i;
                waitAckEventIds[i] = eventId;
                waitAckEventTimes[i] = Time.time;
                waitAckRetries[i] = 0;
                waitAckPackets[i] = encoded;
                break;
            }
        }

        if (pos == -1)
        {
            Debug.LogWarning(string.Format("[UdonNet] No place remaining for waiting acknowledgement for event ID {0}. It may experience packet loss.", eventId));
            return;
        }
        Debug.Log(string.Format("[UdonNet] Packet waiting for ack at position {0}/{1}", pos, waitAckEventIds.Length));
    }

    public bool ClearWaitAck(uint eventId)
    {
        for (int i = 0; i < waitAckEventIds.Length; i++)
        {
            if (waitAckEventIds[i] == eventId)
            {
                waitAckEventIds[i] = 0;
                waitAckEventTimes[i] = 0;
                waitAckRetries[i] = 0;
                waitAckPackets[i] = null;
                return true;
            }
        }
        return false;
    }

    void CheckWaitAckTimeout()
    {
        for (int i = 0; i < waitAckEventIds.Length; i++)
        {
            if (waitAckEventIds[i] != 0 && Time.time - waitAckEventTimes[i] > udonNetController.waitAckTimeout / 1000)
            {
                if (waitAckRetries[i] >= udonNetController.waitAckMaxRetries)
                {
                    Debug.LogError(string.Format("[UdonNet] Maximum send retries reached ({0}), packet event ID {1} failed to send.", udonNetController.waitAckMaxRetries, waitAckEventIds[i]));
                    ClearWaitAck(waitAckEventIds[i]);
                } else
                {
                    waitAckEventTimes[i] = Time.time;
                    waitAckRetries[i]++;
                    Debug.LogWarning(string.Format("[UdonNet] Wait ack timeout reached. Attempting to re-send packet event ID {0} (retry {1}/{2})", waitAckEventIds[i], waitAckRetries[i], udonNetController.waitAckMaxRetries));
                    AddPacketToQueue(waitAckEventIds[i], waitAckPackets[i], false);
                }
            }
        }
    }

    public bool SendVersion(int targetPlayerId)
    {
        byte[] buffer = Int32ToBytes(udonNetController.ProtocolVersion);
        return SendRawToPlayer(targetPlayerId, buffer, buffer.Length);
    }

    /// <summary>
    /// Sends data to the world players. It can only be called by this current associated player/GameObject owner.
    /// </summary>
    /// <returns>It returns whether this data is permitted to send, that the caller is the GameObject owner and the string data does not exceed the limit.</returns>
    public bool SendPacket(byte packetFlags, int targetPlayerId, byte[] buffer, int bufferLen, byte segmentIndex)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return false;
        }
        Debug.Log(string.Format("[UdonNet] Attempting to send packet type {0} to player {1} with buffer length {2}", packetFlags, targetPlayerId, bufferLen));

        uint newEventId = eventId++;
        string encoded = Encode(
            newEventId,
            packetFlags,
            targetPlayerId,
            buffer,
            bufferLen,
            segmentIndex
            );
        Debug.Log(string.Format("[UdonNet] Encoded packet data: {0}", encoded));

        if (encoded == null)
        {
            return false;
        }

        AddPacketToQueue(newEventId, encoded, (packetFlags & udonNetController.PacketLossless) != 0);

        return true;
    }

    //
    // Unity Event Handling
    //

    public override void OnDeserialization()
    {
        if (!Networking.IsOwner(gameObject) &&
            data != lastData &&
            Time.timeSinceLevelLoad > 10 &&
            !string.IsNullOrEmpty(data))
        {
            Debug.Log(string.Format("[UdonNet] New deserialized data received: {0}", data));
            lastData = data;
            if (udonNetController == null)
            {
                Debug.LogError("[UdonNet] No UdonNetController is connected to handle received data!");
                return;
            }

            VRCPlayerApi player = Networking.GetOwner(gameObject);
            udonNetController.Handle(player, Decode(data));
        }
    }

    //
    // Data encoding
    //

    public int CalculateSegmentsCount(byte packetFlags, int bufferLen)
    {
        int headerLen = 8; //eventId (uint) + packetFlags (byte) + segmentNumber (byte) + segmentLength (ushort)
        if ((packetFlags & udonNetController.PacketTargetedPlayer) != 0)
        {
           headerLen += 4; //targetPlayer (int)
        }
        int availableSize = udonNetController.networkFrameSize - headerLen;
        return (int) Mathf.Ceil(bufferLen / availableSize);
    }

    //TODO: Improve the payload encoding
    /// <summary>
    /// Encodes specified data into a synchronizable Base64 string
    /// </summary>
    /// <returns>Base64 string</returns>
    public string Encode(uint eventId, byte packetFlags, int targetPlayerId, byte[] buffer, int bufferLen, byte segmentIndex)
    {
        if (udonNetController == null)
        {
            return null;
        }
        
        byte[] arr = new byte[udonNetController.networkFrameSize];
        int offset = 0;
        
        byte[] eventIdArr = Uint32ToBytes(eventId);
        for (int i = 0; i < eventIdArr.Length; i++)
        {
            arr[offset + i] = eventIdArr[i];
        }
        offset += eventIdArr.Length;

        arr[offset++] = packetFlags;
        
        if ((packetFlags & udonNetController.PacketTargetedPlayer) != 0)
        {
            byte[] targetPlayerArr = Int32ToBytes(targetPlayerId);
            for (int i = 0; i < targetPlayerArr.Length; i++)
            {
                arr[offset + i] = targetPlayerArr[i];
            }
            offset += targetPlayerArr.Length;
        }
        
        int availableSize = udonNetController.networkFrameSize - offset;

        if ((packetFlags & udonNetController.PacketSegmentedPacket) != 0)
        {
            int count = CalculateSegmentsCount(packetFlags, bufferLen);

            if (segmentIndex < 0 || segmentIndex >= count)
            {
                Debug.LogError(string.Format("[UdonNet] Cannot encode UdonNetData because wrong segment index is provided: {0}/{1}", segmentIndex, count));
                return null;
            }

            if (segmentIndex == 0)
            {
                arr[5] |= udonNetController.PacketSynchronizeSequenceNumber;
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
                arr[5] |= udonNetController.PacketFinish;
            }
        } else {
            if (availableSize < bufferLen)
            {
                Debug.LogError(string.Format("[UdonNet] Cannot encode UdonNetData because byte buffer length is too long (>{0} bytes): {1}/{2}", udonNetController.networkFrameSize, bufferLen, availableSize));
                return null;
            }

            byte[] bufferLenArr = UshortToBytes((ushort) bufferLen);
            arr[offset++] = bufferLenArr[0];
            arr[offset++] = bufferLenArr[1];

            for (int i = 0; i < bufferLen; i++)
            {
                arr[offset + i] = buffer[i];
            }
        }
        
        
        return Convert.ToBase64String(arr);
    }

    /// <summary>
    /// Decodes the specified Base64 string to UdonNetData
    /// </summary>
    /// <param name="rawData">Raw Base64 data string</param>
    /// <returns> decoded UdonNetData</returns>
    public object[] Decode(string rawData)
    {
        byte[] arr = Convert.FromBase64String(rawData);

        object[] udonNetData = new object[4];

        int offset = 0;

        udonNetData[0] = BytesToUint32(arr, offset);
        offset += 4;

        udonNetData[1] = arr[offset];
        offset += 1;

        if ((arr[4] & udonNetController.PacketTargetedPlayer) != 0)
        {
            udonNetData[2] = BytesToInt32(arr, offset);
            offset += 4;
        } else
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
    }

    //
    // Byte operations
    //

    public byte[] UshortToBytes(ushort x)
    {
        ushort bits = 8;
        byte[] output = new byte[2];
        output[0] = (byte) (x >> bits);
        output[1] = (byte) x;
        return output;
    }

    public byte[] ShortToBytes(short x)
    {
        short bits = 8;
        byte[] output = new byte[2];
        output[0] = (byte) (x >> bits);
        output[1] = (byte) x;
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
        output |= (ushort) (bytes[offset] << 8);
        output |= bytes[offset + 1];
        return output;
    }

    public short BytesToShort(byte[] bytes, int offset)
    {
        short output = 0;
        output |= (short) (bytes[offset] << 8);
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
        output |= bytes[offset + 1];
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
        return output;
    }

    public uint BytesToUint32(byte[] bytes, int offset)
    {
        uint output = 0;
        output |= (uint) bytes[offset] << 24;
        output |= (uint) bytes[offset + 1] << 16;
        output |= (uint) bytes[offset + 2] << 8;
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
