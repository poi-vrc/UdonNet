
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
    //public const byte DataTypeString = 0x01;
    //public const byte DataTypeJson = 0x02;
    //public const int DataTypeVRCUrl = 2;

    public const int ProtocolVersion = 1;
    public const int CompatProtocolVersion = 1;
    
    public const byte PacketLossless = 0x01;
    public const byte PacketTargetedPlayer = 0x02;
    //0x04, 0x08 reserved
    public const byte PacketEnquiry = 0x10;
    public const byte PacketAck = 0x20;
    public const byte PacketNegAck = 0x40;
    //0x80 reserved

    [Header("Connect this UNPlayer to the UdonNetController of the scene")]
    public UdonNetController udonNetController;

    [UdonSynced] private string data;
    private string lastData;

    private uint eventId = 0;

    void Start()
    {

    }

    public bool SendRawDataToPlayer(int targetPlayerId, string stringData)
    {
        return SendPacket(0, targetPlayerId, stringData);
    }

    public bool SendRawDataToPlayerLossless(int targetPlayerId, string stringData)
    {
        return SendPacket(PacketLossless, targetPlayerId, stringData);
    }

    public bool BroadcastRawData(string stringData)
    {
        return SendPacket(0, 0, stringData);
    }

    public bool BroadcastRawDataLossless(string stringData)
    {
        return SendPacket(PacketLossless, 0, stringData);
    }

    /// <summary>
    /// Sends data to the world players. It can only be called by this current associated player/GameObject owner.
    /// </summary>
    /// <returns>It returns whether this data is permitted to send, that the caller is the GameObject owner and the string data does not exceed the limit.</returns>
    public bool SendPacket(byte packetType, int targetPlayerId, string stringData)
    {
        Debug.Log(string.Format("attempting to send data to player {0} and string data {1}", targetPlayerId, stringData));
        if (!Networking.IsOwner(gameObject))
        {
            Debug.Log("not owner in networking. aborting");
            return false;
        }

        Debug.Log("encoding data...");
        string encoded = Encode(
            eventId++,
            packetType,
            targetPlayerId,
            stringData
            );
        Debug.Log(string.Format("encoded data: {0}", encoded));

        if (encoded == null)
        {
            Debug.Log("encoded is null. aborting");
            return false;
        }

        //TODO: Put in queuing
        data = encoded;

        return true;
    }

    public void Ping()
    {

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
            Debug.Log(string.Format("New deserialized data received: {0}", data));
            lastData = data;
            if (udonNetController == null)
            {
                Debug.Log("No UdonNetController is connected to handle received data!");
                return;
            }

            udonNetController.Handle(Networking.GetOwner(gameObject), Decode(data));
        }
    }

    //
    // Data encoding
    //

    //TODO: Improve the payload encoding
    /// <summary>
    /// Encodes specified data into a synchronizable Base64 string
    /// </summary>
    /// <returns>Base64 string</returns>
    public string Encode(uint eventId, byte packetType, int targetPlayerId, string stringData)
    {
        if (udonNetController == null)
        {
            return null;
        }

        Debug.Log("preparing array");
        byte[] arr = new byte[udonNetController.networkFrameSize];
        int offset = 0;

        Debug.Log(string.Format("eventId {0}", eventId));
        byte[] eventIdArr = Uint32ToBytes(eventId);
        Debug.Log(string.Format("offset {0} arrLen/len {1}/{2}", offset, eventIdArr.Length, arr.Length));
        for (int i = 0; i < eventIdArr.Length; i++)
        {
            arr[offset + i] = eventIdArr[i];
        }
        offset += eventIdArr.Length;

        arr[offset++] = packetType;
        
        if ((packetType & PacketTargetedPlayer) != 0)
        {
            Debug.Log(string.Format("targetplayer {0}", targetPlayerId));
            byte[] targetPlayerArr = Int32ToBytes(targetPlayerId);
            Debug.Log(string.Format("offset {0} arrLen/len {1}/{2}", offset, targetPlayerArr.Length, arr.Length));
            for (int i = 0; i < targetPlayerArr.Length; i++)
            {
                arr[offset + i] = targetPlayerArr[i];
            }
            offset += targetPlayerArr.Length;
        }

        //arr[offset++] = dataType;

        Debug.Log(string.Format("stringdata {0}", stringData));
        byte[] stringDataArr = StringToBytes(stringData);
        Debug.Log(string.Format("offset {0} arrLen/len {1}/{2}", offset, stringDataArr.Length, arr.Length));

        if (offset + stringDataArr.Length > udonNetController.networkFrameSize)
        {
            Debug.Log(string.Format("Cannot encode UdonNetData because string data is too long (>{0} bytes): {1}", udonNetController.networkFrameSize, stringDataArr.Length));
            return null;
        }

        Debug.Log("stringdataarr");
        for (int i = 0; i < stringDataArr.Length; i++)
        {
            arr[offset + i] = stringDataArr[i];
        }
        offset += stringDataArr.Length;

        Debug.Log("converting into b64");
        return Convert.ToBase64String(arr);
    }

    /// <summary>
    /// Decodes the specified Base64 string to UdonNetData
    /// </summary>
    /// <param name="rawData">Raw Base64 data string</param>
    /// <returns> decoded UdonNetData</returns>
    public object[] Decode(string rawData)
    {
        Debug.Log(string.Format("decoding data: {0}", rawData));

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
        } else
        {
            udonNetData[2] = null;
        }

        udonNetData[3] = BytesToString(arr, offset);

        Debug.Log(string.Format("returning data: {0},{1},{2},{3}", udonNetData[0], udonNetData[1], udonNetData[2], udonNetData[3]));
        return udonNetData;
    }

    //
    // Byte operations
    //

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
