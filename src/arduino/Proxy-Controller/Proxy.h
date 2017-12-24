/*
  Proxy.h - Library to control a generic proxy object prototype
  Created by André Zenner, April 24, 2016.
*/

#ifndef Proxy_h
#define Proxy_h

#define CALIBRATION_PHASE_NONE 0
#define CALIBRATION_PHASE_UP 1
#define CALIBRATION_PHASE_DOWN 2

#define DEFAULT_SPEED 500

#include "Arduino.h"
#include <AccelStepper.h>
#include <Wire.h>
#include <Adafruit_MotorShield.h>
#include "utility/Adafruit_MS_PWMServoDriver.h"
#include "Button.h"

class Proxy
{
  public:
    Proxy(int stepsPerRevolution, int stepperPort, int stepperMode, int buttonPin = 0, void (*beep)(int)= NULL, void (*light)(int) = NULL);
    void init();
    void calibrationStart();
    void calibrationMaximumReached();
    void calibrationMinimumReached();
    int calibrating();
    void setTargetPosition(float pos);
    void setCurrentSpeed(int velo);
    int getCurrentSpeed();
    void go();
    bool operating();
    float getCurrentPosition();
    bool isTargetReached();
    long getExpectedTimeTo(float pos);
    int getCurrentButtonState();
    int getButtonEvent();
    void stopNow();
    void savePower();
    void setStepperMode(int mode);
    
  private:
    unsigned long _startTime, _endTime;
    unsigned long _referenceTime;
    int _referenceSpeed;
    void (*_beep)(int);
    void (*_light)(int);
    int _stepperPort, _stepperMode;
    bool _operating;
    long _maxPosition;
    int _currentSpeed;
    int _calibrationPhase;
    int _buttonPin, _prevButtonState;
    bool _powerOn;
    Adafruit_MotorShield _AFMS;
    Adafruit_StepperMotor *_adaStepper;
    AccelStepper _stepper;
    int _stepsPerTurn;            //28BYJ-48 data:
                                  //32 * 16 for the 12V edition (1/16 gearing) (define in Proxy constructor)
                                  //32 * 64 for the 5V edition  (1/64 gearing)
                                  //12V 1/16 mit echten 12V: speed max. 300 -> schafft 125g Gewicht auf 50cm  

                                  //NEMA 14 (14HS10-0404S) [2015.6.8]
                                  //200 steps per revolution
                                  //12V

    void startOperating();
    void stopOperating();

    static Proxy *_activeProxy;
    static void _forwardStep();
    static void _backwardStep();
};

#endif
