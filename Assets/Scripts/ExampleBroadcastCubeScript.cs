
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

    [Header("Request for an acknowledgement from receivers")]
    public bool losslessMode = false;

    void Start()
    {

    }

    public override void Interact()
    {
        NetworkedUNPlayer unp = udonNet.GetLocalUNPlayer();
        if (losslessMode)
        {
            unp.BroadcastStringLossless("Lossless " + customStringToSend + " at time " + Time.realtimeSinceStartup);
        } else
        {
            unp.BroadcastString(customStringToSend + " at time " + Time.realtimeSinceStartup);
        }
    }
}
