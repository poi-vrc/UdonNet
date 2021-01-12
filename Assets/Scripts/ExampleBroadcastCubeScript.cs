
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExampleBroadcastCubeScript : UdonSharpBehaviour
{

    [Header("Connects this cube to the UdonNet")]
    public UdonNetController udonNet;

    [Header("Just some random string to send to the network")]
    public string customStringToSend;

    void Start()
    {

    }

    public override void Interact()
    {
        NetworkedUNPlayer unp = udonNet.GetLocalUNPlayer();
        unp.BroadcastRawData(customStringToSend + " at time " + Time.realtimeSinceStartup);
    }
}
