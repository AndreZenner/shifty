using System;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

/*
    The ProxyControlAPI Client Library

    Some basic functions used to remote-control the Proxy Object running the ProxyControlServer via WiFi and to receive button presses.

    Version: .NET2.0
    Author: André Zenner, 30.04.2016
    */
namespace ProxyControlAPI
{

    public enum COMMANDS : byte
    {
        CHECK_CURRENT_POSITION,
        SEND_NEW_TARGET_POSITION,
        SEND_NEW_SPEED,
        CHECK_IS_TARGET_REACHED,
        CHECK_CURRENT_SPEED,
        RECALIBRATE,
        CHECK_EXPECTED_TIME,
        EVENT_BUTTON_DOWN,          //No request sent for ButtonDown/Up/Change -> clients directly subscribe callbacks to the events
        EVENT_BUTTON_UP,
        SEND_SAVE_POWER,
        DISCONNECT,
        STEPPING_MODE,
        VERSION_INFO
    }

    public enum STEPPING_MODES : byte
    {
        SINGLE,
        DOUBLE,
        INTERLEAVE,
        MICROSTEPPING
    }

    public class Tuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public Tuple(T1 i1, T2 i2)
        {
            Item1 = i1;
            Item2 = i2;
        }

    }

    public class ProxyControlClient
    { 
        protected const int PROTOCOL_PACKET_SIZE = 5;

        protected IPEndPoint ipep;
        protected Socket server;

        public delegate void ConnectionAttemptCallback(bool success);
        public delegate void ConnectionCheckCallback(bool isAlive);
        public delegate void ReconnectionAttemptCallback(bool success);
        public delegate void DisconnectionCallback();
        public delegate void CurrentPositionCallback(float pos);
        public delegate void NewTargetCallback(float payload);
        public delegate void NewSpeedCallback(float payload);
        public delegate void IsTargetReachedCallback(bool isReached);
        public delegate void CurrentSpeedCallback(int speed);
        public delegate void RecalibrateCallback(float payload);
        public delegate void ExpectedTimeCallback(long time);
        public delegate void ButtonDownCallback(float pos);     
        public delegate void ButtonUpCallback(float pos);
        public delegate void ButtonChangeCallback(bool nowUp, float pos);
        public delegate void SavePowerCallback(float payload);
        public delegate void SteppingModeCallback(float payload);
        public delegate void VersionInfoCallback(float payload);

        public event ConnectionAttemptCallback ConnectionAttemptEvent;
        public event ConnectionCheckCallback ConnectionCheckEvent;
        public event ReconnectionAttemptCallback ReconnectionAttemptEvent;
        public event DisconnectionCallback DisconnectionEvent;
        public event CurrentPositionCallback CurrentPositionEvent;
        public event NewTargetCallback NewTargetEvent;
        public event NewSpeedCallback NewSpeedEvent;
        public event IsTargetReachedCallback IsTargetReachedEvent;
        public event CurrentSpeedCallback CurrentSpeedEvent;
        public event RecalibrateCallback RecalibrateEvent;
        public event ExpectedTimeCallback ExpectedTimeEvent;
        public event ButtonDownCallback ButtonDownEvent;
        public event ButtonUpCallback ButtonUpEvent;
        public event ButtonChangeCallback ButtonChangeEvent;
        public event SavePowerCallback SavePowerEvent;
        public event SteppingModeCallback SteppingModeEvent;
        public event VersionInfoCallback VersionInfoEvent;


        /* default constructor */
        public ProxyControlClient() {}

        /* Connects to the proxy. Blocking function */
        public bool connectToProxy(string ip, int port)
        {
            ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try { server.Connect(ipep); return true; }catch(SocketException e) { PRINT(e.Message); return false; }
        }

        /* Connects to the proxy and then calls the callback. Blocking function */
        public bool connectToProxy(string ip, int port, ConnectionAttemptCallback callback)
        {
            ConnectionAttemptCallback realCallback = null;
            realCallback = delegate (bool success)
            {
                callback(success);
                ConnectionAttemptEvent -= realCallback;
            };
            ConnectionAttemptEvent += realCallback;
            bool res = connectToProxy(ip, port);
            if (ConnectionAttemptEvent != null)
                ConnectionAttemptEvent(res);
            return res;
        }

        /* Connects to the proxy in a new thread and then calls the callback from this thread. Non-Blocking function */
        public void connectToProxyAsync(string ip, int port, ConnectionAttemptCallback callback)
        {
            ConnectionAttemptCallback realCallback = null;
            realCallback = delegate (bool success)
            {
                callback(success);
                ConnectionAttemptEvent -= realCallback;
            };
            ConnectionAttemptEvent += realCallback;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                bool res = connectToProxy(ip, port);
                if(ConnectionAttemptEvent != null)
                    ConnectionAttemptEvent(res);

            });
            bw.RunWorkerAsync();
        }


        /* Disconnects from and then reconnects to the proxy. Blocking function */
        public bool reconnectToProxy(string ip, int port)
        {
            disconnect();
            return connectToProxy(ip, port);
        }

        /* Disconnects from, then reconnects to the proxy and then calls the callback. Blocking function */
        public bool reconnectToProxy(string ip, int port, ReconnectionAttemptCallback callback)
        {
            ReconnectionAttemptCallback realCallback = null;
            realCallback = delegate (bool success)
            {
                callback(success);
                ReconnectionAttemptEvent -= realCallback;
            };
            ReconnectionAttemptEvent += realCallback;
            bool res = reconnectToProxy(ip, port);
            if (ReconnectionAttemptEvent != null)
                ReconnectionAttemptEvent(res);
            return res;
        }

        /* Disconnects from, then reconnects to the proxy in a new thread and then calls the callback from this thread. Non-Blocking function */
        public void reconnectToProxyAsync(string ip, int port, ReconnectionAttemptCallback callback)
        {
            ReconnectionAttemptCallback realCallback = null;
            realCallback = delegate (bool success)
            {
                callback(success);
                ReconnectionAttemptEvent -= realCallback;
            };
            ReconnectionAttemptEvent += realCallback;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                bool res = reconnectToProxy(ip, port);
                if (ReconnectionAttemptEvent != null)
                    ReconnectionAttemptEvent(res);

            });
            bw.RunWorkerAsync();
        }




        /* Checks the connection by sending a test request and reading the answer. Upon reception calls the callback. Non-Blocking */
        public void checkConnection(ConnectionCheckCallback callback)
        {
            ConnectionCheckCallback realCallback = null;
            realCallback = delegate (bool isAlive)
            {
                callback(isAlive);
                ConnectionCheckEvent -= realCallback;
            };
            ConnectionCheckEvent += realCallback;
            send((byte) COMMANDS.CHECK_CURRENT_POSITION, 0);
        }

        /* Requests the current weight position from the proxy (in [0,1]) and calls the callback when received. Non-Blocking */
        public void sendCurrentPositionRequest(CurrentPositionCallback callback)
        {
            CurrentPositionCallback realCallback = null;
            realCallback = delegate (float pos)
            {
                callback(pos);
                CurrentPositionEvent -= realCallback;
            };
            CurrentPositionEvent += realCallback;
            send((byte) COMMANDS.CHECK_CURRENT_POSITION, 0);
        }

        /* Sends a new target position for the weight to the proxy (in [0,1]) and then calls the callback upon ACK. Non-Blocking */
        public void sendNewTargetRequest(NewTargetCallback callback, float newTarget)
        {
            if(newTarget < 0 || newTarget > 1)
            {
                return;
            }

            NewTargetCallback realCallback = null;
            realCallback = delegate (float payload)
            {
                callback(payload);
                NewTargetEvent -= realCallback;
            };
            NewTargetEvent += realCallback;
            send((byte)COMMANDS.SEND_NEW_TARGET_POSITION, newTarget);
        }

        /* Sends a new speed for the weight to the proxy (int) and then calls the callback upon ACK. Non-Blocking */
        public void sendNewSpeedRequest(NewSpeedCallback callback, int newSpeed)
        {
            NewSpeedCallback realCallback = null;
            realCallback = delegate (float payload)
            {
                callback(payload);
                NewSpeedEvent -= realCallback;
            };
            NewSpeedEvent += realCallback;
            send((byte)COMMANDS.SEND_NEW_SPEED, newSpeed);
        }

        /* Checks whether the weight reached the target position and calls the callback when the check result is received. Non-Blocking */
        public void sendIsTargetReachedRequest(IsTargetReachedCallback callback)
        {
            IsTargetReachedCallback realCallback = null;
            realCallback = delegate (bool isReached)
            {
                callback(isReached);
                IsTargetReachedEvent -= realCallback;
            };
            IsTargetReachedEvent += realCallback;
            send((byte)COMMANDS.CHECK_IS_TARGET_REACHED, 0);
        }

        /* Requests the current speed from the proxy (int) and calls the callback when received. Non-Blocking */
        public void sendCurrentSpeedRequest(CurrentSpeedCallback callback)
        {
            CurrentSpeedCallback realCallback = null;
            realCallback = delegate (int speed)
            {
                callback(speed);
                CurrentSpeedEvent -= realCallback;
            };
            CurrentSpeedEvent += realCallback;
            send((byte)COMMANDS.CHECK_CURRENT_SPEED, 0);
        }

        /* Requests a recalibration of the proxy and calls the callback when received ACK. Non-Blocking */
        public void sendRecalibrationRequest(RecalibrateCallback callback)
        {
            RecalibrateCallback realCallback = null;
            realCallback = delegate (float payload)
            {
                callback(payload);
                RecalibrateEvent -= realCallback;
            };
            RecalibrateEvent += realCallback;
            send((byte)COMMANDS.RECALIBRATE, 0);
        }

        /* Requests the expected time to move the weight to the given position and calls the callback when received. Non-Blocking */
        public void sendExpectedTimeRequest(ExpectedTimeCallback callback, float pos)
        {
            ExpectedTimeCallback realCallback = null;
            realCallback = delegate (long time)
            {
                callback(time);
                ExpectedTimeEvent -= realCallback;
            };
            ExpectedTimeEvent += realCallback;
            send((byte)COMMANDS.CHECK_EXPECTED_TIME, pos);
        }

        /* Sends the command to save motor power consumption by releasing the motor and then calls the callback upon ACK. Non-Blocking */
        public void sendSavePowerRequest(SavePowerCallback callback)
        {
            SavePowerCallback realCallback = null;
            realCallback = delegate (float payload)
            {
                callback(payload);
                SavePowerEvent -= realCallback;
            };
            SavePowerEvent += realCallback;
            send((byte)COMMANDS.SEND_SAVE_POWER, 0);
        }

        /* Sends a new speed for the weight to the proxy (int) and then calls the callback upon ACK. Non-Blocking */
        public void sendNewSteppingModeRequest(SteppingModeCallback callback, STEPPING_MODES mode)
        {
            SteppingModeCallback realCallback = null;
            realCallback = delegate (float payload)
            {
                callback(payload);
                SteppingModeEvent -= realCallback;
            };
            SteppingModeEvent += realCallback;
            send((byte)COMMANDS.STEPPING_MODE, (float) mode);
        }

        /* Sends a new speed for the weight to the proxy (int) and then calls the callback upon ACK. Non-Blocking */
        public void sendNewSteppingModeRequest(SteppingModeCallback callback, float mode)
        {
            SteppingModeCallback realCallback = null;
            realCallback = delegate (float payload)
            {
                callback(payload);
                SteppingModeEvent -= realCallback;
            };
            SteppingModeEvent += realCallback;
            send((byte)COMMANDS.STEPPING_MODE, (float)mode);
        }

        /* Checks the proxy's firmware version and calls the callback when the check result is received. Non-Blocking */
        public void sendVersionInfoRequest(VersionInfoCallback callback)
        {
            VersionInfoCallback realCallback = null;
            realCallback = delegate (float version)
            {
                callback(version);
                VersionInfoEvent -= realCallback;
            };
            VersionInfoEvent += realCallback;
            send((byte)COMMANDS.VERSION_INFO, 0);
        }


        /* Called in the loop to handle the connection and incoming messages. Non-Blocking */
        public void receiveAndHandleCallbacks()
        {
            Tuple<byte, float> incoming = receive();
            if(incoming != null)
            {
                byte command = incoming.Item1;
                float payload = incoming.Item2;

                if(command == (byte)COMMANDS.CHECK_CURRENT_POSITION)            //CurrentPosition result received
                {
                    if(CurrentPositionEvent != null)
                    {
                        CurrentPositionEvent(payload);
                        PRINT("CurrentPosition response received -> callback executed!");
                    }
                    if(ConnectionCheckEvent != null)
                    {
                        ConnectionCheckEvent(true);
                        PRINT("Connection to proxy is still alive!");
                    }
                }
                else if(command == (byte)COMMANDS.SEND_NEW_TARGET_POSITION)      //NewTarget result received
                {
                    if(NewTargetEvent != null)
                    {
                        NewTargetEvent(payload);
                        PRINT("NewTarget response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.SEND_NEW_SPEED)              //NewSpeed result received
                {
                    if (NewSpeedEvent != null)
                    {
                        NewSpeedEvent(payload);
                        PRINT("NewSpeed response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.CHECK_IS_TARGET_REACHED)              //NewSpeed result received
                {
                    if (IsTargetReachedEvent != null)
                    {
                        IsTargetReachedEvent(payload == 0 ? false : true);
                        PRINT("IsTargetReached response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.CHECK_CURRENT_SPEED)            //CurrentPosition result received
                {
                    if (CurrentSpeedEvent != null)
                    {
                        CurrentSpeedEvent((int) payload);
                        PRINT("CurrentSpeed response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.RECALIBRATE)      //NewTarget result received
                {
                    if (RecalibrateEvent != null)
                    {
                        RecalibrateEvent(payload);
                        PRINT("Recalibrate response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.CHECK_EXPECTED_TIME)      //Expected Time result received
                {
                    if (ExpectedTimeEvent != null)
                    {
                        ExpectedTimeEvent((long) payload);
                        PRINT("Expected Time response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.EVENT_BUTTON_DOWN)      //Button Down received
                {
                    if (ButtonDownEvent != null)
                    {
                        ButtonDownEvent(payload);
                        PRINT("Button Down received -> callback executed!");
                    }
                    if (ButtonChangeEvent != null)
                    {
                        ButtonChangeEvent(false, payload);
                        PRINT("Button Change received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.EVENT_BUTTON_UP)      //Expected Time result received
                {
                    if (ButtonUpEvent != null)
                    {
                        ButtonUpEvent(payload);
                        PRINT("Button Up received -> callback executed!");
                    }
                    if (ButtonChangeEvent != null)
                    {
                        ButtonChangeEvent(true, payload);
                        PRINT("Button Change received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.SEND_SAVE_POWER)      //Save Power ACK received
                {
                    if (SavePowerEvent != null)
                    {
                        SavePowerEvent(payload);
                        PRINT("Save Power ACK received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.STEPPING_MODE)              //SteppingMode result received
                {
                    if (SteppingModeEvent != null)
                    {
                        SteppingModeEvent(payload);
                        PRINT("SteppingMode response received -> callback executed!");
                    }
                }
                else if (command == (byte)COMMANDS.VERSION_INFO)              //VersionInfo result received
                {
                    if (VersionInfoEvent != null)
                    {
                        VersionInfoEvent(payload);
                        PRINT("VersionInfo response received -> callback executed!");
                    }
                }
                else
                {
                    PRINT("Unknown response (" + command + ") received from server!");
                }
            }
        }

        /* Called by receiveAndHandleCallbacks to check for incoming messages. Non-Blocking */
        protected Tuple<byte, float> receive()
        {
            if (server.Connected && server.Available > 0)
            {
                byte[] buffer = new byte[PROTOCOL_PACKET_SIZE];
                int len = server.Receive(buffer);
                if(len > 0) {
                    PRINT("Received " + len + " bytes!");
                    if(len == PROTOCOL_PACKET_SIZE)
                    {
                        byte command = buffer[0];
                        float payload = System.BitConverter.ToSingle(buffer, 1);
                        PRINT("Received command: " + command);
                        PRINT("Received payload: " + payload);
                        return new Tuple<byte, float>(command, payload);
                    }
                    else
                    {
                        PRINT("Received data is not protocol conform!");
                    }
                }

            }
            return null;
        }

        /* Sends the command and the payload in the correct format to the proxy */
        public void send(byte command, float payload)
        {
            if (server.Connected)
            {
                PRINT("Preparing buffer for sending");
                byte[] buffer = new byte[PROTOCOL_PACKET_SIZE];
                buffer[0] = command;
                byte[] payloadBuffer = System.BitConverter.GetBytes(payload);
                for (int i = 0; i < payloadBuffer.Length; i++)
                {
                    buffer[i + 1] = payloadBuffer[i];
                }

                server.Send(buffer);
                PRINT("Data sent!");
            }
        }



        /* Disconnects from the proxy if connected. Blocking */
        public void disconnect()
        {
            if (server.Connected)
            {
                send((byte) COMMANDS.DISCONNECT, 0);
                server.Shutdown(SocketShutdown.Both);
                server.Close();
                PRINT("Disconnected from server!");
            }
        }

        /* Disconnects from the proxy if connected, then calls the callback. Blocking */
        public void disconnect(DisconnectionCallback callback)
        {
            if (server.Connected)
            {
                DisconnectionCallback realCallback = null;
                realCallback = delegate ()
                {
                    callback();
                    DisconnectionEvent -= realCallback;
                };
                DisconnectionEvent += realCallback;
                send((byte) COMMANDS.DISCONNECT, 0);
                server.Shutdown(SocketShutdown.Both);
                server.Close();
                PRINT("Disconnected from server!");
                if (DisconnectionEvent != null)
                    DisconnectionEvent();
            }
        }

        /* Disconnects from the proxy in a new thread if connected, then calls the callback from this thread. Non-Blocking function */
        public void disconnectAsync(DisconnectionCallback callback)
        {
            DisconnectionCallback realCallback = null;
            realCallback = delegate ()
            {
                callback();
                DisconnectionEvent -= realCallback;
            };
            DisconnectionEvent += realCallback;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                disconnect();
                if (DisconnectionEvent != null)
                    DisconnectionEvent();

            });
            bw.RunWorkerAsync();
        }


        //DEFINE CONSOLE OR DEBUG OUTPUT HERE
        protected void PRINT(string s)
        {
            Console.WriteLine(s);
        }
    }
}
