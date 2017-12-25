# Shifty
This repository allows you to build your own **Shifty** - a weight-shifting dynamic passiv haptic proxy for virtual reality!

![Image of the Shifty prototype](pics/shifty-2.jpg "The Shifty prototype")

## Introduction
**Shifty**, as introduced in our TVCG paper [Shifty: A Weight-Shifting Dynamic Passive Haptic Proxy to Enhance Object Perception in Virtual Reality](https://doi.org/10.1109/TVCG.2017.2656978 "Shifty @ IEEE Xplore Digital Library") is a weight-shifting VR controller that allows you to better feel the weight of virtual objects and transformations of them.
For this, it shifts an internal mass while you interact with virtual objects in VR.
Using appropriate visualizations in the virtual environment in synchronization with Shifty's changes in mass distribution (and thus the felt moment of inertia and lever arm), allows for immersive haptic experiences in VR. 

For more information on its haptic effect, the scientific background and user study results, please take a look at the paper.
Shifty was presented at the [IEEE Virtual Reality Conference 2017 in Los Angeles](https://youtu.be/Tzx6ZOIjLhY "Watch the talk!").
To get a first impression, the [corresponding video](https://youtu.be/1l0wKk6q_ss "Watch the video!") is probably a good starting point.

## Contents
This repository contains the files, source code and information you need to make your own Shifty prototype.

- [3d-prints](3d-prints/) contains the files to 3D print 
  - Shifty's internal weight hull (you have to fill it with lead granulate)
  - A mount for the HTC Vive tracker (you have to get a 1/4" camera screw) that can be plugged on Shifty
- [circuit](circuit/) contains the [Fritzing](http://fritzing.org/home/) file and a drawing of the used Arduino circuit
- [laser-cut](laser-cut/) contains the .svg cutter file to cut the box holding the electronics
- [pics](pics/) contains pictures of parts, intermediate building steps and the final prototype
- [src](src/) contains the source code
  - [src/arduino](src/arduino/) holds the Arduino firmware to deploy on the Arduino Uno, handling WiFi communication and motor control
  - [src/api](src/api/) holds the C# API that allows programs to talk to Shifty's Arduino and that can be used to control the weight shift and receive button presses. As configured, this Visual Studio project currently builds a .dll to include in your own programs (e.g. used in Unity to communicate with Shifty)
  - [src/unity](src/unity/) holds a small minimal-example project that demonstrates how to integrate the API in Unity using the .dll file (Unity 5.6)
  
## Parts you need
You will probably need the following parts to build the prototype:
- Power:
  - charger for 12V rechargeable battery
  - 12V rechargeable battery (we used a lead accumulator with 7.2 Ah and 6.3mm connectors)
  - DC 12V Step Up Step Down Converter
  - 2x On-Off switch
- Mechanics:
  - plexiglas	pipe outer-Ø: 40,0mm, inner-Ø: 36,0mm
  - Nema 14 Step Motor 12V 0.4A 14Ncm/20oz.in 35x35x26mm 1.8deg Bipolar 4-wire
  - small pulley (GT2 Pulley, 20 teeth, Ø8mm, gear)
  - big pulley (GT2 Pulley, 40 teeth, Ø5mm, gear)
  - toothed belt (GT2)
  - ball bearings for small pulley 5x8x2,5mm
  - ball bearings for weight-wheels 6x12x4mm
  - M5 screw
  - M5 wing nut
  - cable ties for the motor
  - rubberband 25mm width to hold the button
  - velcro strap to fix the rubberband holding the button
  - fine lead granulate ø 0,6 - 1,5 mm
  - backpack to put the battery and electronics in
- Electronics:
  - Arduino Uno R3
  - Adafruit Motor/Stepper/Servo Shield for Arduino Version 2.3
  - Adafruit stacking header set
  - ESP8266 WiFi Module
  - Mini-Breadboard - 400 contacts
  - Mini-Button, placed on Shifty with the rubberband (e.g. SMD Button 3x6x2.5mm)
  - capacitors (104, 100nF)
  - resistors as shown in the circuit schematics
  - LED as shown in the circuit schematics 
  - Buzzer 5V / 12mm
  - shielded microphone cable (4 x 0.14 mm²)
  - connector (8-pin)
  - some jumper wires
  
Additionally, to make the weight and the Vive Tracker mount, you will need access to a 3D printer (we used an Ultimaker 2+; PLA) and to laser-cut the box, access to a laser cutter.

## How-To Build
To build a Shifty prototype, follow these steps:

### Prepare the ESP8266 WiFi chip
We used the ESP8266 module with AT version 0.18 - as it works with the [WeeESP8266](https://github.com/itead/ITEADLIB_Arduino_WeeESP8266) Arduino library. For this to work, you need the corresponding firmware running on your ESP module. To flash the firmware on the module, you can use the [esptool](https://github.com/themadinventor/esptool) and follow [this video](https://www.youtube.com/watch?v=PycRnjcXMRI). A working firmware to flash can be found [here](http://wiki.fablab-nuernberg.de/w/Ding:ESP8266#Firmware) (click V0.9.2.4).
<img src="pics/esp8266.jpg" alt="The ESP8266 WiFi module" width="500">

### Building Shifty
Having all required parts ready, you can build the prototype:
#### Step 1: Pipe, Motor, Pulley
1. Take the plexiglas pipe, cut it in the desired proxy length and make a cutout for the motor to reside on <img src="pics/cutout-motor-1.jpg" alt="motor cutout w/o motor" width="500"> <img src="pics/cutout-motor-2.jpg" alt="motor cutout w/ motor" width="500">
2. Using the cable ties, fix the motor with the big pulley screwed on the shaft, so that the pulley is exactly in the center of the pipe. You can put leather between pipe and motor to dampen vibrations and sound. <img src="pics/motor.jpg" alt="motor" width="500"> <img src="pics/pulley-big.jpg" alt="big pulley" width="500"> <img src="pics/cutout-motor-3.jpg" alt="motor fixed" width="500">
3. On the other end of the pipe, cut two lengthy recesses for the M5 screw (e.g. by drilling holes and connecting them using a Dremel) <p> <img src="pics/cutout-pulley-1.jpg" alt="pulley cutout holes" width="500"> <img src="pics/cutout-pulley-2.jpg" alt="pulley cutout" width="250"> </p>
4. Take the screw, put the small pulley with the ball bearings on it and insert it into the pipe through the recesses. Fix it with the wing nut. <p> <img src="pics/pulley.jpg" alt="small pulley" width="500"> <img src="pics/cutout-pulley-3.jpg" alt="pulley fixed" width="500"> </p>

#### Step 2: Weight, Belt
1. Print the weight hull two times with a 3D printer
2. Fill both halves with the lead granulate <p><img src="pics/weight.jpg" alt="pulley fixed" width="500"></p>
3. Measure the distance between the big pulley on the motor and the small pulley on the top end of the pipe to determine the length of the belt. Then cut the belt. <p><img src="pics/belt.jpg" alt="belt" width="500"></p>
4. Insert the bearings as weight-wheels and fix the two weight halves. Then attach the belt on the weight. <img src="pics/bearings.jpg" alt="bearings" width="500"> <img src="pics/weight-belt.jpg" alt="assembled weight attached to belt" width="500">

#### Step 3: Button
1. Take the small button and attach a cable on both ends. <img src="pics/button-part.jpg" alt="small button" width="500">
2. Cut the rubberband and the velcron strap. Stick the two cables through the band in order to attach the button on the pipe. <p><img src="pics/button.jpg" alt="button fixed with rubberband on Shifty" width="500"></p>

#### Step 4: Arduino, Circuit, Cables
1. Prepare the ESP8266 module as described above.
2. Solder the stacking headers to the motor shield and stack the shield on the Arduino.
3. Create the circuit. <img src="circuit/shifty-circuit.svg" alt="Shifty circuit">
4. Solder the cable connections and connectors. <p><img src="pics/connectors.jpg" alt="connectors" width="500"> <img src="pics/cables.jpg" alt="cables at Shifty" width="500"> </p><p>The electronics should look like this:</p><p> <img src="pics/circuit-foto.jpg" alt="photo of the curcuit" width="500"></p>
5. Using the voltage converter and switches, prepare the power supply (e.g. on the font panel of the box) <img src="pics/voltage-regulator.jpg" alt="voltage converter" width="500"> <img src="pics/switch.jpg" alt="switches" width="500"> <img src="pics/front-panel.jpg" alt="front panel" width="500">

#### Step 5: Final Assembly, Vive Tracker Mount
1. Insert the weight and the belt in the pipe. Fasten the belt by adjusting the position of the top pulley / screw. <img src="pics/shifty.jpg" alt="shifty with inserted weight" width="500">
2. If too loud, you can use small pieces of cork to dampen the sound of the internal weight. <img src="pics/cork.jpg" alt="cork on the weight" width="500">
3. Putting everything together, the result should look similar to this: <img src="pics/complete.jpg" alt="complete Shifty prototype" width="500">
4. Deploy the [Shifty Arduino software](src/arduino/) on the Arduino and test if it works. After start-up, Shifty has to be calibrated. For this, the weight will automatically move towards the top and you have to press the button at the top-most end. After that, the weight moves downwards and you have to set the lowest position again by pressing the button. Upon completion of this calibration, Shifty is ready to go!
5. If everything works, take a backpack and insert the battery and the electronics with the box, just leaving a cable connection to Shifty. Wearing the backback, you can use Shifty even in room-scale VR experiences. <img src="pics/complete-backpack.jpg" alt="complete Shifty prototype with backpack" width="500">
6. Finally, 3D print the Vive Tracker mount and place the tracker on Shifty to easily track it while in VR.

### Controlling Shifty
Use the [ProxyControlAPI](src/api/) to connect to Shifty, to pass commands and to read the button states.
The library currently builds a .dll file containing the API functions.
See the [Unity Example](src/unity/), and especially the corresponding [ExampleProxyConnection.cs](src/unity/Shifty-Unity-Minimal-Example/Assets/github-minimal/ExampleProxyConnection.cs) script, as well as the functions offered by the API defined in [ProxyControlAPI.cs](src/api/ProxyControlAPI/ProxyControlClient/ProxyControlAPI.cs), for examples and reference.

To test your prototype, you can build and use the small [ProxyControlAPI-Console tool](src/api/ProxyControlAPI/) that allows you to send commands manually in order to test your device.

## Credits
Before use, please see the [LICENSE](LICENSE.md) for copyright and license details.

Authors of the [paper](https://doi.org/10.1109/TVCG.2017.2656978 "Shifty @ IEEE Xplore Digital Library"):  
**André Zenner** - andre.zenner@dfki.de  
**Prof. Dr. Antonio Krüger** - krueger@dfki.de

This work was supported by the [Deutsches Forschungszentrum für Künstliche Intelligenz GmbH](https://www.dfki.de/) (DFKI; German Research Center for Artificial Intelligence) and [Saarland University](https://www.uni-saarland.de/).
<img src="pics/dfki-logo.jpg" alt="DFKI Logo" width="250">
<img src="pics/uds-logo.png" alt="Saarland University Logo" width="250">
