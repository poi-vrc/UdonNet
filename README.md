# UdonNet

A work-in-progress, experimental networking system for Udon. UdonNet aims to make Udon P2P communication easier and simplier.

## Usage

### Installation

1. Import the latest VRCSDK3 and UdonSharp to your project.

2. Import the latest UdonNet Unity package from the releases to your own project.

3. Either drag the ```UdonNet``` prefab to your scene, or create empty GameObjects with the following structure. You have to create a pool of NetworkedUNPlayer by duplicating the 1,2,3... GameObjects to match the hard-cap of your world.
```
UdonNet (Add the "UdonBehaviour" component and set the program asset as "UdonNetController")
 |
 -> 1 (Add the "UdonBehaviour" component and set the program asset as "NetworkedUNPlayer")
 |
 -> 2
 |
 -> 3
 |
 ....
 ....
```

### To send data to the world players

```ExampleBroadcastCubeScript.cs``` is an example for broadcasting data while a player interacts a cube.

```csharp
//Shows this field in the inspector to allow you to use the UdonNetController in your code
[Header("Drag your UdonNet GameObject here")]
public UdonNetController udonNet;

public override void Interact()
{
    NetworkedUNPlayer unp = udonNet.GetLocalUNPlayer(); //Retrieves this player's own assigned UNPlayer
    unp.BroadcastString("Hello World!"); //Broadcasts this message to all players
}
```

### To receive/process data from players

```ExampleLoggerScript.cs``` is an example for receiving and logging data.

> The variable names are temporary and might be changed in the future.

1. Implement your own code to receive UdonNet events. All variables and methods have to be public to allow data to be modified and code to be executed.
```csharp
public object[] _udonNetReceivedData; //A 4-length array that contains all the received packet data
public VRCPlayerApi _udonNetFromPlayer; //The VRCPlayerApi of the player that sent this data

public int _udonNetTargetedPlayer; //Targeted player ID of this packet. Only available if packet flag PacketTargetedPlayer is on.
public string _udonNetStringData; //Decoded ASCII string from the buffer. Only available if packet flag PacketDataTypeString is on.

//Called when the event broadcasts to all players (i.e. announcements, game status)
public void OnUdonNetBroadcastEvent() {
{
    // variables available:
    // _udonNetReceivedData, _udonNetFromPlayer, _udonNetStringData
}

//Called when the event targets at a player (i.e. Private Messages)
public void OnUdonNetPlayerEvent() {
{
    // variables available:
    // _udonNetReceivedData, _udonNetFromPlayer, _udonNetTargetedPlayer, _udonNetStringData
}

//Called both in broadcast and targeted player event
public void OnUdonNetEvent() {
{
    // always available:
    // _udonNetReceivedData, _udonNetFromPlayer
    
    // you have determine it youself by checking the packet flags from "_udonNetReceivedData"
}
```

2. Drag your connected GameObject into your scene's UdonNet (```UdonNetController```) event listeners' array.

## Development stage and known issues

- UdonSharpBehaviours can now send data to other players via the API
- Lossless mode is still experimental and for targeted player mode only, unexpected behaviour will happen with broadcast
- Code is not optimized for anything
- Due to Udon and UdonSharp limitations, some data and codes are either hard-coded or dirty written. (i.e. data are packed into a ```object[]``` instead of using a custom class for holding things)

## Limitations

- The communication is quite slow (1-2 sec) due to Udon limitation
- Data cannot be sent too frequently (at least 1 sec one packet), otherwise data may lost

## License

This project is licensed under the MIT License.