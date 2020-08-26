using UnityEngine;
using System.Collections;

using Oculus.Platform;
using Oculus.Platform.Models;

// Helper class to manage the Voice-over-IP connection to the
// remote users
public class VoipMan
{
    public VoipMan()
    {
        Voip.SetVoipConnectRequestCallback(VoipConnectRequestCallback);
        Voip.SetVoipStateChangeCallback(VoipStateChangedCallback);
    }

    public void ConnectTo(ulong userID)
    {
        // ID comparison is used to decide who initiates and who gets the Callback
        if (SocialMan.MyID < userID)
        {
            Voip.Start(userID);
            SocialMan.LogOutput("Voip connect to " + userID);
        }
    }


    public void Disconnect(ulong userID)
    {
        if (userID != 0)
        {
            Voip.Stop(userID);

            RemotePlayer remote = SocialMan.GetRemoteUser(userID);
            if (remote != null)
            {
                remote.voipConnectionState = PeerConnectionState.Unknown;
            }
        }
    }

    void VoipConnectRequestCallback(Message<NetworkingPeer> msg)
    {
        SocialMan.LogOutput("Voip request from " + msg.Data.ID);

        RemotePlayer remote = SocialMan.GetRemoteUser(msg.Data.ID);
        if (remote != null)
        {
            SocialMan.LogOutput("Voip request accepted from " + msg.Data.ID);
            Voip.Accept(msg.Data.ID);
        }
    }

    void VoipStateChangedCallback(Message<NetworkingPeer> msg)
    {
        SocialMan.LogOutput("Voip state to " + msg.Data.ID + " changed to  " + msg.Data.State);

        RemotePlayer remote = SocialMan.GetRemoteUser(msg.Data.ID);
        if (remote != null)
        {
            remote.voipConnectionState = msg.Data.State;

            // ID comparison is used to decide who initiates and who gets the Callback
            if (msg.Data.State == PeerConnectionState.Timeout && SocialMan.MyID < msg.Data.ID)
            {
                // keep trying until hangup!
                Voip.Start(msg.Data.ID);
                SocialMan.LogOutput("Voip re-connect to " + msg.Data.ID);
            }
        }
    }
}
