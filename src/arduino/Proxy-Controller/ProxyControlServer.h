/*
  ProxyControlServer.h - A TCP server that allows to control the proxy object remotely via a simple WiFi protocol.
  Created by André Zenner, April 30, 2016.
*/
#ifndef ProxyControlServer_h
#define ProxyControlServer_h

//TARGET PLATFORM
#define PLATFORM_UNO
//#define PLATFORM_MEGA     //un-comment only the target platform

#ifdef PLATFORM_UNO
  #define SOFT_SERIAL_RX 7
  #define SOFT_SERIAL_TX 8
  //go to the ESP8266.h file in the libraries folder and un-comment "#define ESP8266_USE_SOFTWARE_SERIAL"
#endif

#include "Arduino.h"
#include "Proxy.h"
#include "ESP8266.h"
#ifdef PLATFORM_UNO
  #include <SoftwareSerial.h>
#endif


class ProxyControlServer {
  public:
    ProxyControlServer();
    void init(bool connectToWifi, String ssid, String pw, int port, int timeout, Proxy* proxy, float versioninfo, void (*beep)(int) = NULL, void (*light)(int) = NULL);
    bool startServer();
    bool listenForCommands();
    bool closeServer();
    void sendResponse(uint8_t mux_id, uint8_t command, float payload);
    void sendButtonEvent(uint8_t mux_id, int buttonEvent, float payload);
    uint8_t getLastMuxID();

  private:
    String _ssid, _pw;
    float _version;
    Proxy* _proxy;
    int _port, _timeout;
    int _commandsReceived;
    uint8_t _lastMuxID;
    #ifdef PLATFORM_UNO
      SoftwareSerial _serial1 = SoftwareSerial(SOFT_SERIAL_RX, SOFT_SERIAL_TX);
    #else
      HardwareSerial _serial1 = Serial1;
    #endif
    ESP8266 _wifi;
    void (*_beep)(int);
    void (*_light)(int);

    void handleCommand(uint8_t mux_id, uint8_t command, float payload);  
};
#endif
