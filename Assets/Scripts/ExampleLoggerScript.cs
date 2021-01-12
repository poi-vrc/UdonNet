
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class ExampleLoggerScript : UdonSharpBehaviour
{
    private Text text;

    public VRCPlayerApi _udonNetFromPlayer;

    public string _udonNetStringData;

    void Start()
    {
        text = (Text) GetComponent(typeof(Text));
        text.text += string.Format("[UdonNetExampleLogger] Initialized at {0}!", Time.realtimeSinceStartup);
    }

    public void OnUdonNetEvent()
    {
        Debug.Log("[UdonNetExampleLogger] OnUdonNetEvent received!");
    }

    public void OnUdonNetBroadcastEvent()
    {
        Debug.Log("[UdonNetExampleLogger] OnUdonNetBroadcastEvent received!");

        if (_udonNetFromPlayer == null)
        {
            text.text += string.Format("[unknown][{0}] {1}\n", Time.realtimeSinceStartup, _udonNetStringData);
        } else
        {
            text.text += string.Format("[{0} ({1})][{2}] {3}\n", _udonNetFromPlayer.displayName, _udonNetFromPlayer.playerId, Time.realtimeSinceStartup, _udonNetStringData);
        }
    }

    public void OnUdonNetPlayerEvent()
    {
        Debug.Log("[UdonNetExampleLogger] OnUdonNetPlayerEvent received!");
    }
}
