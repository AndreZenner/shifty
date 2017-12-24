/*
  ProxyControlServer.cpp - A TCP server that allows to control the proxy object remotely via a simple WiFi protocol.
  Created by André Zenner, April 30, 2016.
*/

#include "Arduino.h"
#include "ProxyControlServer.h"

#define PROTOCOL_PACKET_SIZE 5

ProxyControlServer::ProxyControlServer() : _wifi(_serial1) {
  _serial1.begin(9600);
}

void ProxyControlServer::init(bool connectToWifi, String ssid, String pw, int port, int timeout, Proxy* proxy, float versioninfo, void (*beep)(int), void (*light)(int)) {
  if (proxy == NULL) {
    Serial.print(F("ERROR: ProxyControlServer initialized with 'NULL' Proxy pointer!"));
  }
  _ssid = ssid;
  _pw = pw;
  _proxy = proxy;
  _port = port;
  _timeout = timeout;
  _commandsReceived = 0;
  _beep = beep;
  _light = light;
  _version = versioninfo;

  /* INIT WIFI */
  bool initSuccess = true;
  Serial.print(F("\tESP8266-WiFi Module AT Version:"));
  Serial.println(_wifi.getVersion().c_str());

  if (_wifi.setOprToStationSoftAP()) {
    Serial.print(F("\tOperation Mode set to 'station + softap' ... OK\r\n"));
    initSuccess &= true;
  } else {
    Serial.print(F("\tOperation Mode set to 'station + softap' ... ERROR\r\n"));
    initSuccess &= false;
    _beep(200);
    delay(200);
    _beep(200);
    delay(200);
    _beep(400);
  }

  if (connectToWifi) {
    if (_wifi.joinAP(_ssid, _pw)) {
      Serial.println(_ssid);
      Serial.print(F("\tJoined WiFi Network ... SUCCESS\r\n"));
      initSuccess &= true;
    } else {
      Serial.println(_ssid);
      Serial.print(F("\tJoined WiFi Network ... ERROR\r\n"));
      initSuccess &= false;
    }
  } else {
    Serial.print(F("\tWiFi Network opened! Not joining external WiFi Network according to configuration!\r\n"));
    initSuccess &= true;
  }

  Serial.print(F("\tIP: "));
  Serial.println(_wifi.getLocalIP().c_str());


  if (_wifi.enableMUX()) {
    Serial.print(F("\tEnabled 'multi-client' Mode ... SUCCESS\r\n"));
    initSuccess &= true;
  } else {
    Serial.print(F("\tEnabled 'multi-client' Mode ... ERROR\r\n"));
    initSuccess &= false;
  }

  if (initSuccess) {
    Serial.print(F("\t\t--> WiFi Module ready!\n\n"));
  } else {
    Serial.print(F("\t\t--> Error initializing WiFi Module!\n\n"));
  }
}


bool ProxyControlServer::startServer() {
  bool startServerSuccess = true;
  if (_wifi.startTCPServer(_port)) {
    Serial.print(F("\tPort: "));
    Serial.print(_port);
    Serial.print(F("\n"));
    Serial.print(F("\tStarting TCP Server ... SUCCESS\r\n"));
    startServerSuccess &= true;
  } else {
    Serial.print(F("\tStarting TCP Server ... ERROR\r\n"));
    startServerSuccess &= false;
  }

  if (_wifi.setTCPServerTimeout(_timeout)) {
    Serial.print(F("\tSet TCP Server Timeout to "));
    Serial.print(_timeout);
    Serial.print(F(" seconds ... SUCCESS\r\n"));
    startServerSuccess &= true;
  } else {
    Serial.print(F("\tSet TCP Server Timeout to "));
    Serial.print(_timeout);
    Serial.print(F(" seconds ... ERROR\r\n"));
    startServerSuccess &= false;
  }

  if (startServerSuccess) {
    Serial.println(F("\t\t--> Ready to receive remote commands!\n\n"));
  } else {
    Serial.println(F("\t\t--> Could not start TCP server correctly!\n\n"));
  }
}

bool ProxyControlServer::listenForCommands() {
  uint8_t buffer[PROTOCOL_PACKET_SIZE] = {0};
  uint8_t mux_id;
  uint32_t len = _wifi.recv(&mux_id, buffer, sizeof(buffer), 100);   //do not use a long timeout here (maybe 100), as button presses will be checked at this frequency
  if (len > 0) {
    Serial.print(F("\tReceived data from remote ("));
    Serial.print(mux_id);
    Serial.print(F("):"));
    _lastMuxID = mux_id;
    //Serial.print(F("["));
    //Serial.print(_wifi.getIPStatus().c_str());
    //Serial.println(F("]"));

    Serial.print(F("\t\t--> ["));
    for (uint32_t i = 0; i < PROTOCOL_PACKET_SIZE; i++) {
      Serial.print((char)buffer[i]);
    }
    Serial.print(F("]\r\n"));

    //correct protocol package
    if (len == 5) {
      uint8_t command = buffer[0];
      float payload = *((float*)(&(buffer[1])));

      Serial.print(F("\t\t--> ["));
      Serial.print(command);
      Serial.print(F(", "));
      Serial.print(payload, 8);
      Serial.print(F("]\r\n)"));

      handleCommand(mux_id, command, payload);

      //Serial.print(F("Server Status:["));
      //Serial.print(_wifi.getIPStatus().c_str());
      //Serial.println(F("]"));

    } else {
      Serial.println(F("\t\t--> Data is not protocol conform!"));
    }

    Serial.print(F("\n\n"));
  }
}

/* handling the protocol */
void ProxyControlServer::handleCommand(uint8_t mux_id, uint8_t command, float payload) {

  if (command == 0) { //client requested current position
    Serial.print(F("\t-> Client requested current position ..."));
    if (_light != NULL)
      _light(5);
    _commandsReceived++;
    float retPayload = _proxy->getCurrentPosition();
    sendResponse(mux_id, command, retPayload);
    Serial.println(F(" sent!"));

  } else if (command == 1) { //client sends new target position
    Serial.println(F("\t-> Client sends new target position!"));
    if (_light != NULL)
      _light(2);
    _commandsReceived++;
    sendResponse(mux_id, command, _proxy->getExpectedTimeTo(payload));
    _proxy->setTargetPosition(payload);
    Serial.println(F("\t-> ACK sent!"));

  } else if (command == 2) {
    Serial.println(F("\t-> Client sends new speed ..."));
    if (_light != NULL)
      _light(2);
    _commandsReceived++;
    sendResponse(mux_id, command, *((float*)(&"OK")));
    _proxy->setCurrentSpeed((int) payload);
    Serial.println(F("\t-> ACK sent!"));

  } else if (command == 3) {
    Serial.println(F("\t-> Client requests isTargetReached ..."));
    if (_light != NULL)
      _light(5);
    _commandsReceived++;
    float retPayload = _proxy->isTargetReached() ? 1 : 0;
    sendResponse(mux_id, command, retPayload);
    Serial.println(F(" sent!"));

  } else if (command == 4) {
    Serial.println(F("\t-> Client requests current speed ..."));
    if (_light != NULL)
      _light(5);
    _commandsReceived++;
    float retPayload = (float) _proxy->getCurrentSpeed();
    sendResponse(mux_id, command, retPayload);
    Serial.println(F(" sent!"));

  } else if (command == 5) {
    Serial.println(F("\t-> Client requests recalibration ..."));
    if (_light != NULL)
      _light(2);
    if (_beep != NULL) {
      _beep(200);
      delay(400);
      _beep(200);
    }
    _commandsReceived++;
    sendResponse(mux_id, command, *((float*)(&"OK")));
    _proxy->calibrationStart();
    Serial.println(F("\t-> ACK sent!"));

  } else if (command == 6) {
    Serial.println(F("\t-> Client requests expected time ..."));
    if (_light != NULL)
      _light(5);
    _commandsReceived++;
    float retPayload = _proxy->getExpectedTimeTo(payload);
    sendResponse(mux_id, command, retPayload);
    Serial.println(F("\t-> Time sent!"));

  } else if (command == 9) {
    Serial.println(F("\t-> Client requests power saving ..."));
    if (_light != NULL)
      _light(5);
    _commandsReceived++;
    _proxy->savePower();
    sendResponse(mux_id, command, *((float*)(&"OK")));
    Serial.println(F("\t-> Power Saving ACK sent!"));

  } else if (command == 10) {  //see SDK Enumeration for COMMAND list
    Serial.println(F("\t-> Client wants to disconnect ..."));

  } else if (command == 11) {
    Serial.println(F("\t-> Client sends new stepping mode ..."));
    if (_light != NULL)
      _light(2);
    _commandsReceived++;
    switch ((int)payload) {            //for the mapping to the payload see SDK ENUMERATION
      case 0:
        _proxy->setStepperMode(SINGLE);
        Serial.println(F("\t-> Stepping Mode set to SINGLE"));
        break;
      case 1:
        _proxy->setStepperMode(DOUBLE);
        Serial.println(F("\t-> Stepping Mode set to DOUBLE"));
        break;
      case 2:
        _proxy->setStepperMode(INTERLEAVE);
        Serial.println(F("\t-> Stepping Mode set to INTERLEAVE"));
        break;
      case 3:
        _proxy->setStepperMode(MICROSTEP);
        Serial.println(F("\t-> Stepping Mode set to MICROSTEP"));
        break;
      default:
        Serial.print(F("\t-> Stepping Mode UNKNOWN = "));
        Serial.println(payload);
        break;
    }
    sendResponse(mux_id, command, *((float*)(&"OK")));
    Serial.println(F("\t-> ACK sent!"));
    
  } else if (command == 12) {
    Serial.println(F("\t-> Client requests VersionInfo ..."));
    if (_light != NULL)
      _light(5);
    _commandsReceived++;
    float retPayload = _version;
    sendResponse(mux_id, command, retPayload);
    Serial.print(_version);
    Serial.println(F(" sent!"));

  }
}

  /* sends button changes */
  void ProxyControlServer::sendButtonEvent(uint8_t mux_id, int buttonEvent, float payload) {
    if (buttonEvent == BUTTON_EVENT_DOWN || buttonEvent == BUTTON_EVENT_UP) {
      sendResponse(mux_id, 6 + buttonEvent, payload);
    }
  }

  void ProxyControlServer::sendResponse(uint8_t mux_id, uint8_t command, float payload) {
    //prepare buffer to send
    uint8_t buffer[PROTOCOL_PACKET_SIZE] = {0};
    buffer[0] = command;
    uint8_t* payloadBuffer = (uint8_t*)(&payload);
    for (int i = 0; i < 4; i++) {
      buffer[i + 1] = payloadBuffer[i];
      Serial.print(payloadBuffer[i]);
    }

    Serial.print(F("\t\tBuffer prepared for sending: ["));
    for (uint32_t i = 0; i < PROTOCOL_PACKET_SIZE; i++) {
      Serial.print((char)buffer[i]);
    }
    Serial.print(F("]\r\n"));
    Serial.println(F("\tTesting buffer ..."));
    uint8_t testCommand = buffer[0];
    float testPayload = *((float*)(&(buffer[1])));
    Serial.print(F("\t\t--> ["));
    Serial.print(testCommand);
    Serial.print(F(", "));
    Serial.print(testPayload, 8);
    Serial.print(F("]\r\n)"));
    if (testCommand == command && testPayload == payload) {
      Serial.println(F("\t\t--> Test passed!"));
    } else {
      Serial.println(F("\t\t--> Test not passed!"));
    }

    if (_wifi.send(mux_id, buffer, sizeof(buffer))) {
      Serial.print(F("\t\t--> Data sent!"));
    } else {
      Serial.print(F("\t\t--> ERROR sending data!"));
    }
  }

  bool ProxyControlServer::closeServer() {
    if (_wifi.stopTCPServer()) {
      Serial.println(F("\t\t--> Stopping TCP Server ... SUCCESS"));
    } else {
      Serial.println(F("\t\t--> Stopping TCP Server ... ERROR"));
    }
  }

  uint8_t ProxyControlServer::getLastMuxID() {
    return _lastMuxID;
  }

