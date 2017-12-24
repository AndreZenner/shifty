/*
  Proxy.cpp - Library to control a generic proxy object prototype
  Created by André Zenner, April 24, 2016.
*/

#include "Arduino.h"
#include "Proxy.h"

Proxy *Proxy::_activeProxy;

Proxy::Proxy(int stepsPerRevolution, int stepperPort, int stepperMode, int buttonPin, void(*beep)(int), void(*light)(int))
{
  Serial.println(F("Creating Proxy object ..."));

  Proxy::_activeProxy = this;
  _stepsPerTurn = stepsPerRevolution;
  _currentSpeed = DEFAULT_SPEED;
  _stepperPort = stepperPort;
  _stepperMode = stepperMode;
  _operating = false;
  _maxPosition = 0;
  _calibrationPhase = CALIBRATION_PHASE_NONE;
  _beep = beep;
  _light = light;
  _startTime = 0;
  _endTime = 0;
  _referenceSpeed = _currentSpeed;
  _referenceTime = 0;
  _buttonPin = buttonPin;
  _prevButtonState = LOW;
  _powerOn = true;

  _AFMS = Adafruit_MotorShield();   //default I2C address
  _adaStepper = _AFMS.getStepper(stepsPerRevolution, stepperPort);
  _stepper = AccelStepper(_forwardStep, _backwardStep);

  Serial.print(F("Proxy object created and set active.\n"));
}

void Proxy::init()
{
  Serial.println(F("\tInitializing Proxy ..."));

  _AFMS.begin();
  TWBR = ((F_CPU /400000l) - 16) / 2; // Change the i2c clock to 400KHz
  _stepper.setMaxSpeed(10000.0);
  //_stepper.setAcceleration(300.0);
  //_stepper.setSpeed(100.0);
  
  Serial.println(F("\tProxy initialized.\n"));
}

void Proxy::calibrationStart(){
  if(!_operating){
    Serial.println(F("[Calibration]--> Proxy object calibration starts..."));
    _calibrationPhase = CALIBRATION_PHASE_UP;
    _maxPosition = 0;
    _stepper.setSpeed(_currentSpeed);
    _referenceSpeed = _currentSpeed;
    startOperating();
  }
}

void Proxy::calibrationMaximumReached(){
  if(_operating){
    _stepper.setSpeed(0);
    _stepper.setCurrentPosition(0);
    _calibrationPhase = CALIBRATION_PHASE_DOWN;
    _stepper.setSpeed(-_currentSpeed);
    Serial.println(F("[Calibration]--> Proxy object reached maximum position."));
    _startTime = millis();
  }
}

void Proxy::calibrationMinimumReached(){
  if(_operating){
    _endTime = millis();
    _referenceTime = _endTime - _startTime;
    _stepper.setSpeed(0);
    _maxPosition = -_stepper.currentPosition();
    _stepper.setCurrentPosition(0);
    _stepper.moveTo(0);   //new
    _calibrationPhase = CALIBRATION_PHASE_NONE;
    stopOperating();
    Serial.println(F("[Calibration]--> Proxy object is calibrated!"));
  }
}

int Proxy::calibrating(){
  return _calibrationPhase;
}

/* pos in [0,1] after calibration */
void Proxy::setTargetPosition(float pos)
{
  //CW = HOCH (-)
  //CCW = RUNTER (+)
  
   long target = pos * _maxPosition;
   if(target != _stepper.currentPosition()){
      _stepper.moveTo(target);
     startOperating();
     Serial.print(F("Set target to "));
     Serial.print(target);
     Serial.print(F("\n"));
     if(_beep != NULL)
        _beep(100);
     Serial.print(F("Expected time (ms) = "));
     Serial.println(getExpectedTimeTo(pos));
     _startTime = millis();
   } 
}

void Proxy::setCurrentSpeed(int velo){
  _currentSpeed = velo;
}

int Proxy::getCurrentSpeed(){
  return _currentSpeed;
}

float Proxy::getCurrentPosition(){
  return ((float)_stepper.currentPosition() / (float)_maxPosition);
}

bool Proxy::isTargetReached(){
  return _stepper.distanceToGo() == 0;
}

long Proxy::getExpectedTimeTo(float pos){
  return (((float)_referenceSpeed / (float) _currentSpeed) * _referenceTime) * abs(pos - getCurrentPosition());
}

int Proxy::getCurrentButtonState(){
  if(_buttonPin <= 0){
    return -1;
  }else{
    return digitalRead(_buttonPin);
  }
}

int Proxy::getButtonEvent(){
  if(_buttonPin <= 0){
    return BUTTON_EVENT_NONE;
  }else{
    int currentState = getCurrentButtonState();
    if(_prevButtonState != currentState){
      _prevButtonState = currentState;
      if(currentState == LOW){
        return BUTTON_EVENT_DOWN;
      }else{
        return BUTTON_EVENT_UP;
      }
    }else{
      return BUTTON_EVENT_NONE;
    }
  }
}

void Proxy::stopNow(){
  _stepper.moveTo(_stepper.currentPosition());
  _endTime = millis();
  Serial.print(F("Measured time (ms) = "));
  Serial.println(_endTime - _startTime);
  stopOperating();
}

void Proxy::savePower(){
  stopNow();
  _adaStepper->release();
  _powerOn = false;
  if(_light)
    _light(-1);   //means lights off
  Serial.println(F("Motor power off!"));
}

void Proxy::setStepperMode(int mode){
  _stepperMode = mode;
}

void Proxy::go(){
  if(_calibrationPhase != CALIBRATION_PHASE_NONE){    //DIRECTIONAL MOVEMENT IN CALIBRATION
    _stepper.runSpeed();
  }else{                                              //TARGET BASED MOVEMENT IN NORMAL OPERATOIN
    if(_stepper.distanceToGo() != 0){
      _stepper.setSpeed(_stepper.distanceToGo() < 0 ? -_currentSpeed : _currentSpeed);
      _stepper.runSpeedToPosition();
      //_stepper.runToPosition();
    }else if(_operating){
      //_endTime = millis();
      //Serial.print(F("Measured time (ms) = "));
      //Serial.println(_endTime - _startTime);
      //stopOperating();
      stopNow();
    }
  }
}

bool Proxy::operating(){
  return _operating;
}

void Proxy::startOperating(){
  _operating = true;
  _powerOn = true;
  if(_light)
    _light(0);    //means always light up
  //Serial.println("\t\t--> Proxy operating true");
}

void Proxy::stopOperating(){
  _operating = false;
  //Serial.println("\t\t--> Proxy operating false");
}

void Proxy::_forwardStep(){
  Proxy::_activeProxy->_adaStepper->onestep(FORWARD, Proxy::_activeProxy->_stepperMode);
  if(Proxy::_activeProxy->_stepperMode == MICROSTEP){   //16 micro steps = 1 normal step
    for(int i=0; i< 15; i++){
      Proxy::_activeProxy->_adaStepper->onestep(FORWARD, Proxy::_activeProxy->_stepperMode);
    }
  }else if(Proxy::_activeProxy->_stepperMode == INTERLEAVE){  //2 interleaved steps = 1 normal step
      Proxy::_activeProxy->_adaStepper->onestep(FORWARD, Proxy::_activeProxy->_stepperMode);
  }
}

void Proxy::_backwardStep(){
  Proxy::_activeProxy->_adaStepper->onestep(BACKWARD, Proxy::_activeProxy->_stepperMode);
  if(Proxy::_activeProxy->_stepperMode == MICROSTEP){
    for(int i=0; i< 15; i++){
      Proxy::_activeProxy->_adaStepper->onestep(BACKWARD, Proxy::_activeProxy->_stepperMode);
    }
  }else if(Proxy::_activeProxy->_stepperMode == INTERLEAVE){
      Proxy::_activeProxy->_adaStepper->onestep(BACKWARD, Proxy::_activeProxy->_stepperMode);
  }
}

