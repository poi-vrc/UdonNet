
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CubeA : UdonSharpBehaviour
{

    [Header("Put your UdonNet here")]
    public UdonNetController udonNet;

    void Start()
    {
        
    }

    public override void Interact()
    {
        Debug.Log("Touched cube A!");
        Debug.Log(string.Format("i am palyerId {0}", Networking.LocalPlayer.playerId));
        if (Networking.IsOwner(gameObject))
        {
            Debug.Log("I am Owner!");
        }
        else
        {
            Debug.Log("I am not Owner!");
        }
        float t = Time.realtimeSinceStartup;
        Debug.Log(string.Format("trying to send data {0}", t));

        NetworkedUNPlayer unp = udonNet.GetNetworkedUNPlayer(Networking.LocalPlayer);
        Debug.Log(string.Format("result: {0}", unp.BroadcastRawData(System.Convert.ToString(t))));
    }
}
