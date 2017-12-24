using System;

using ProxyControlAPI;
using ConnectionCheckCallback = ProxyControlAPI.ProxyControlClient.ConnectionCheckCallback;
using CurrentPositionCallback = ProxyControlAPI.ProxyControlClient.CurrentPositionCallback;
using NewTargetCallback = ProxyControlAPI.ProxyControlClient.NewTargetCallback;
using NewSpeedCallback = ProxyControlAPI.ProxyControlClient.NewSpeedCallback;
using IsTargetReachedCallback = ProxyControlAPI.ProxyControlClient.IsTargetReachedCallback;
using CurrentSpeedCallback = ProxyControlAPI.ProxyControlClient.CurrentSpeedCallback;
using RecalibrateCallback = ProxyControlAPI.ProxyControlClient.RecalibrateCallback;
using ExpectedTimeCallback = ProxyControlAPI.ProxyControlClient.ExpectedTimeCallback;
using SavePowerCallback = ProxyControlAPI.ProxyControlClient.SavePowerCallback;
using SteppingModeCallback = ProxyControlAPI.ProxyControlClient.SteppingModeCallback;
using VersionInfoCallback = ProxyControlAPI.ProxyControlClient.VersionInfoCallback;

namespace ProxyControlAPI_Console
{
    class Program
    {

        static string proxyIP = "192.168.178.33";      //connect to ESP_DE7216 hotspot -> 192.168.4.1
        static int proxyPort = 8090;

        static void Main(string[] args)
        {

            Console.WriteLine("The ProxyControlClient Console\n\nType 'command;payload'\nConnecting to proxy object ...");

            ProxyControlAPI.ProxyControlClient client = new ProxyControlAPI.ProxyControlClient();
            if(!client.connectToProxy(proxyIP, proxyPort))
            {
                Console.WriteLine("Could not connect to the proxy! Terminating.");
            }
            else
            {
                Console.WriteLine("... connected!");
            }


            string input = "";
            while ((input = Console.ReadLine()) != "#")
            {
                if(input == "reconnect")
                {
                    Console.WriteLine("Reconnecting ... ");
                    if(!client.connectToProxy(proxyIP, proxyPort))
                    {

                        Console.WriteLine("Could not connect to the proxy!");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("... connected!");
                    }
                }else if(input == "C")
                {
                    ConnectionCheckCallback callback = delegate (bool isAlive) { Console.WriteLine("IS ALIVE ! " + isAlive); };
                    client.checkConnection(callback);
                }
                
                string[] split = input.Split(';');
                if (split.Length == 2)
                {
                    try
                    {
                        byte command = Byte.Parse(split[0]);
                        float payload = float.Parse(split[1]);
                        if (command == (byte)COMMANDS.CHECK_CURRENT_POSITION)
                        {
                            CurrentPositionCallback callback = delegate (float pos) { Console.WriteLine("Current Position: " + pos);};
                            client.sendCurrentPositionRequest(callback);
                        }
                        else if (command == (byte)COMMANDS.SEND_NEW_TARGET_POSITION)
                        {
                            NewTargetCallback callback = delegate (float pos) { Console.WriteLine("NewTarget Answer from server: " + System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(pos))); };
                            client.sendNewTargetRequest(callback, payload);
                        }
                        else if (command == (byte)COMMANDS.SEND_NEW_SPEED)
                        {
                            NewSpeedCallback callback = delegate (float pos) { Console.WriteLine("NewSpeed Answer from server: " + System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(pos))); };
                            client.sendNewSpeedRequest(callback, (int) payload);
                        }
                        else if (command == (byte)COMMANDS.CHECK_IS_TARGET_REACHED)
                        {
                            IsTargetReachedCallback callback = delegate (bool isReached) { Console.WriteLine(isReached ? "Target reached!" : "Target not yet reached!"); };
                            client.sendIsTargetReachedRequest(callback);
                        }
                        else if (command == (byte)COMMANDS.CHECK_CURRENT_SPEED)
                        {
                            CurrentSpeedCallback callback = delegate (int speed) { Console.WriteLine("Current Speed: " + speed); };
                            client.sendCurrentSpeedRequest(callback);
                        }
                        else if (command == (byte)COMMANDS.RECALIBRATE)
                        {
                            RecalibrateCallback callback = delegate (float pos) { Console.WriteLine("Recalibrate Answer from server: " + System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(pos))); };
                            client.sendRecalibrationRequest(callback);
                        }
                        else if (command == (byte)COMMANDS.CHECK_EXPECTED_TIME)
                        {
                            ExpectedTimeCallback callback = delegate (long time) { Console.WriteLine("Expected Time Estimation from server: " + System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(time))); };
                            client.sendExpectedTimeRequest(callback, payload);
                        }
                        else if (command == (byte)COMMANDS.SEND_SAVE_POWER)
                        {
                            SavePowerCallback callback = delegate (float p) { Console.WriteLine("Save Power ACK from server: " + System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(p))); };
                            client.sendSavePowerRequest(callback);
                        }
                        else if (command == (byte)COMMANDS.STEPPING_MODE)
                        {
                            SteppingModeCallback callback = delegate (float pos) { Console.WriteLine("SteppingMode Answer from server: " + System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(pos))); };
                            client.sendNewSteppingModeRequest(callback, (int)payload);
                        }
                        else if (command == (byte)COMMANDS.VERSION_INFO)
                        {
                            VersionInfoCallback callback = delegate (float version) { Console.WriteLine("VersionInfo Answer from server: " + version); };
                            client.sendVersionInfoRequest(callback);
                        }
                        else
                        {
                            Console.WriteLine("Unknown command input! Try again!");
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception caught! Try again!");
                        Console.WriteLine(e.Message);
                    }
                }

                client.receiveAndHandleCallbacks();
            }

            Console.WriteLine("Disconnecting from server...");
            client.disconnect();
            Console.ReadLine();
        }
    }
}
