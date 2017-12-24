using UnityEngine;
using ProxyControlAPI;      //using the Proxy API (the .dll)

/*
 *  An example of how to control Shifty from within Unity.

    Author: André Zenner, December 2017
*/

public class ExampleProxyConnection : MonoBehaviour
{
    public string ip = "192.168.0.101";
    public int port = 8090;
    public int proxySpeed = 500;
    public bool AutoConnectOnStart = true;

    [HideInInspector]
    public bool connected = false;
    public ProxyControlClient proxy = new ProxyControlClient();     //using the Proxy API

    //quick fix
    bool subscribedAlready = false;

    //for demo
    int speed1 = 300;
    int speed2 = 400;

    void Start()
    {
        if (AutoConnectOnStart)
            connect();
    }

    //establishes a connection to the proxy and sets it up to be used
    public void connect()
    {
        ProxyControlAPI.ProxyControlClient.ConnectionAttemptCallback cb = delegate (bool isSuccess)
        {
            connected = isSuccess;

            if (isSuccess)
            {
                ProxyControlAPI.ProxyControlClient.VersionInfoCallback cbVersion = delegate (float version)
                {
                    Debug.Log("Shifty firmware version: " + version);
                    Debug.Log("CONNECTED " + isSuccess);

                    //send new speed
                    ProxyControlAPI.ProxyControlClient.NewSpeedCallback cbSpeed = delegate (float payload)
                    {
                        Debug.Log("SPEED SET TO " + proxySpeed);
                        Debug.Log("Proxy READY");
                    };
                    proxy.sendNewSpeedRequest(cbSpeed, proxySpeed);
                };
                proxy.sendVersionInfoRequest(cbVersion);
            }
            else
            {
                Debug.Log("CONNECTED " + isSuccess);
            }
        };
        proxy.connectToProxyAsync(ip, port, cb);

        ProxyControlAPI.ProxyControlClient.ButtonChangeCallback buttoncb = delegate (bool nowDown, float pos)
        {
            Debug.Log("CurrentPos " + pos);
        };
        if (!subscribedAlready)
        {
            proxy.ButtonChangeEvent += buttoncb;
            subscribedAlready = true;
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (proxy != null && connected)
            proxy.receiveAndHandleCallbacks();

        if (Input.GetKeyDown(KeyCode.C))
        {
            connect();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) && connected)
        {
            ProxyControlAPI.ProxyControlClient.NewSpeedCallback cb = delegate (float payload)
            {
                Debug.Log("SET SPEED TO " + speed1 + " - " + payload);
            };
            proxy.sendNewSpeedRequest(cb, speed1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) && connected)
        {
            ProxyControlAPI.ProxyControlClient.NewSpeedCallback cb = delegate (float payload)
            {
                Debug.Log("SET SPEED TO " + speed2 + " - " + payload);
            };
            proxy.sendNewSpeedRequest(cb, speed2);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && connected)
        {
            ProxyControlAPI.ProxyControlClient.NewTargetCallback cb = delegate (float payload)
            {
                Debug.Log("SET TARGET TO " + 1 + " - " + payload);
            };
            proxy.sendNewTargetRequest(cb, 1);
        }

        if (Input.GetKeyDown(KeyCode.Insert) && connected)
        {
            ProxyControlAPI.ProxyControlClient.NewTargetCallback cb = delegate (float payload)
            {
                Debug.Log("SET TARGET TO " + .5f + " - " + payload);
            };
            proxy.sendNewTargetRequest(cb, .5f);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && connected)
        {
            ProxyControlAPI.ProxyControlClient.NewTargetCallback cb = delegate (float payload)
            {
                Debug.Log("SET TARGET TO " + 0 + " - " + payload);
            };
            proxy.sendNewTargetRequest(cb, 0);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ProxyControlAPI.ProxyControlClient.ReconnectionAttemptCallback cb = delegate (bool success)
            {
                Debug.Log("RECONNECT " + success);
            };
            proxy.reconnectToProxyAsync(ip, port, cb);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            ProxyControlAPI.ProxyControlClient.DisconnectionCallback cb = delegate ()
            {
                Debug.Log("DISCONNECTED");
            };
            proxy.disconnectAsync(cb);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            ProxyControlAPI.ProxyControlClient.RecalibrateCallback cb = delegate (float payload)
            {
                Debug.Log("RECALIBRATION " + payload);
            };
            proxy.sendRecalibrationRequest(cb);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ProxyControlAPI.ProxyControlClient.SavePowerCallback cb = delegate (float payload)
            {
                Debug.Log("SAVE POWER: " + payload);
            };
            proxy.sendSavePowerRequest(cb);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            //IF CONNECTION PROBLEM: COMPLETE RESET
            Debug.LogWarning("DOING A COMPLETE CONNECTION RESET!");
            this.proxy = new ProxyControlClient();
            Start();
            Debug.LogWarning("COMPLETE CONNECTION RESET FINISHED!");
        }
    }
}
