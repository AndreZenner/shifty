/*
 * The Proxy-Controller Software
 * 
 * Author: André Zenner
 * 
 * 
 * USED LIBRARIES:
 *    * AccelStepper (Adafruit fork to support MotorShield v2.3 https://github.com/adafruit/AccelStepper
 *    * Adafruit MotorShield v2 Library https://github.com/adafruit/Adafruit_Motor_Shield_V2_Library
 *    * WeeESP8266 Library by itead for the WiFi functionality https://github.com/itead/ITEADLIB_Arduino_WeeESP8266
 *    * Standard Arduino Libraries (SoftwareSerial, ...)
 */

/*-----( Import needed libraries )-----*/
#include "Proxy.h"
#include "ProxyControlServer.h"


/*-----( Declare Constants and Pin Numbers )-----*/
//PROXY-CONTROLLER
#define VERSION 17
#define USE_WIFI true
#define CONNECT_TO_WIFI true
#define ENABLE_BUTTON true

//MOTOR CONTROL                   
#define motorPort 2
#define motorStepsPerRevolution 200

//WIFI
#define SSID        "WIFINAME1"
#define PASSWORD    "WIFIPASSWORD1"
#define SSID2       "WIFINAME2"
#define PASSWORD2   "WIFIPASSWORD2"

//USER INPUT
#define buttonPin 2

//USER OUTPUT
#define LEDPin 3
#define buzzerPin 4


/*-----( Declare objects )-----*/
//Declarations of functions passed to other parts of the system
void beep(int mil);
void light(int mil);

// NEMA 14: stepper motor with 200 steps per revolution (1.8 degree)
// connected to motor port #2 (M3 and M4)
Proxy proxy(motorStepsPerRevolution, motorPort, DOUBLE, ENABLE_BUTTON ? buttonPin : 0, &beep, &light);
ProxyControlServer server;


/*-----( Declare Variables )-----*/
//none

void setup()   /****** SETUP: RUNS ONCE ******/
{
  Serial.begin(9600);
  Serial.println(F("\nProxy-Controller initializing ...\n"));

  /* VERSION INFO */
  Serial.print(F("\tProxy Firmware Version: v"));
  Serial.println(VERSION);
  
  /* INIT USER I/O */
  Serial.println(F("\tUser I/O initializing ..."));
  pinMode(buttonPin, INPUT);
        //DEBUG
        //attachInterrupt(digitalPinToInterrupt(buttonPin), onInterrupt, CHANGE);
        //END DEBUG
  pinMode(LEDPin, OUTPUT);
  pinMode(buzzerPin, OUTPUT);
  tone(buzzerPin, 261, 100);
  Serial.println(F("\tUser I/O initialized!"));

  /* INIT MOTOR */
  proxy.init();
  
  /* INIT WIFI */
  if(USE_WIFI){
    bool initialPress = (digitalRead(buttonPin) == HIGH);
    if(initialPress){
      tone(buzzerPin, 800, 1000);
      delay(2000);
    }
    //bool connectWifi = CONNECT_TO_WIFI && !initialPress;
    String ssid = initialPress ? SSID2 : SSID;
    String password = initialPress ? PASSWORD2 : PASSWORD;
    server.init(CONNECT_TO_WIFI, ssid, password, 8090, 7200, &proxy, VERSION, &beep, &light);
    server.startServer();
  }else{
    Serial.println(F("\tSkipping WiFi according to configuration"));
  }
  
  notifyReady();
  Serial.println(F("Proxy-Controller initialized!\n\n"));

  /* START INITIAL CALIBRATION */
  proxy.calibrationStart();  

}//--(end setup )---

//DEBUG
void onInterrupt(){
  Serial.println(analogRead(buttonPin));  
}
//END DEBUG


void loop()   /****** LOOP: RUNS CONSTANTLY ******/
{

  /* USER I/O */
  if(buttonPressed(buttonPin) && proxy.calibrating() != CALIBRATION_PHASE_NONE){
    if(proxy.calibrating() == CALIBRATION_PHASE_UP){
      tone(buzzerPin, 800, 100);
      delay(500);
      proxy.calibrationMaximumReached();
    }else if(proxy.calibrating() == CALIBRATION_PHASE_DOWN){
      proxy.calibrationMinimumReached();
      tone(buzzerPin, 800, 100);
      delay(200);
      tone(buzzerPin, 800, 100);
      delay(200);
      delay(2000);
    }
  }
  
  if(ENABLE_BUTTON && proxy.calibrating() == CALIBRATION_PHASE_NONE){
    int buttonEvent = proxy.getButtonEvent();
    if(buttonEvent != BUTTON_EVENT_NONE){
      proxy.stopNow();
      server.sendButtonEvent(server.getLastMuxID(), buttonEvent, proxy.getCurrentPosition()); 
      //if(buttonEvent == BUTTON_EVENT_DOWN){
      //  proxy.savePower();
      //}
    }
  }
  
  /* WIFI */
  if(USE_WIFI && !proxy.operating()){
    server.listenForCommands();
  }

  /* MOTOR */
  proxy.go();
  
}//--(end main loop )---



/*-----( Declare User-written Functions )-----*/

/* Blinks the LED x times for y millies */
void blink(int ledPin, int times, int mil){
  for(int i=0; i<times; i++){
    digitalWrite(ledPin, HIGH);
    delay(mil);
    digitalWrite(ledPin,LOW);
    delay(mil);
  }
}

bool buttonPressed(int pin){
  int buttonState = digitalRead(pin);
  delay(1);
  int buttonState2 = digitalRead(pin);
  return buttonState == buttonState2 && buttonState == HIGH;
}

void notifyReady() {
  tone(buzzerPin, 261, 100);
  blink(LEDPin, 3, 100);
  tone(buzzerPin, 523, 100);
  delay(100);
  tone(buzzerPin, 523, 100);
}

//to be passed to other system parts
void beep(int mil){
  tone(buzzerPin, 261, mil);
}

//to be passed to other system parts
void light(int mil){
  if(mil == -1){
    digitalWrite(LEDPin, LOW);
  }else if(mil == 0){
    digitalWrite(LEDPin, HIGH);
  }else{
    blink(LEDPin, 1,mil);
  }
}

