# OBDX Pro Templates and Examples

The purpose of this repository is to provide developers with working examples and template projects to get started quickly with OBDX scantools.

These examples are for Visual Studio C# for Windows PC using .net framework and also MAUI using .NetCore. We will also be looking into creating examples for native Android and iOS in the future.

## Contents ##

1. [Getting Started](#GettingStarted)
1.1 [What is in the Templates?](#WhatsInTheTemplates)
1.2 [Which Vehicles Work with the Templates?](#WhichVehiclesWork)
1.3 [Can I use J2534 Commands?](#j2534toOBDX)
2. [Supported Scantools and Functions](#SupportedScantoolsAndFunctions)
2.1 [Supported OBD2 Protocols](#SupportedOBD2Protocols)
2.2 [Protocol Specific Commands](#protocolspecificcommands)
2.3 [Custom OBDX DLL/Nuget](#customobdxdll)
3. [Issues and Support](#IssuesAndSupport)
4. [Common Questions and Answers](#CommonQandA)

[========]
 

<a name="GettingStarted"></a>
## 1. Getting Started ##
 
First make sure to download the latest template release to your PC and extract the zip file.
All templates have been made using Visual Studio 2022, but will be updated to newest Visual studio versions as time goes on.
Select the template you want to start with and open the solution file to get started.

We have commented the code to explain what each step is doing so we highly recommend reading through the code first to understand the process before making any significant changes

<a name="WhatsInTheTemplates"></a>
### 1.1. What is in the Templates? ###
All templates are a basic UI screen that allows connecting to a scantool and performing basic communication to an engine computer. Both Windows and MAUI templates use our premade OBDX DLL library which is hosted as a Nuget. This allows updating the library independently of your project files as we bring out new capabilities or support. This library handles all the low level communication for reading/writing to the Scantool, while only exposing easy to use functions.

Each template has been setup to connect to a scantool and perform various different actions and examples. This includes connecting to a scantool, setting the desired OBD protocol, setting a filter and finally sending/receive a message from a connected vehicle or ECU.

<a name="WhichVehiclesWork"></a>
### 1.2. Which Vehicles Work with the Templates? ###
These templates have been made with GM, Ford and Chrysler/Jeep ECUs on the bench but can easily be modified to suit other vehicles or modules.

If you add support for a different vehicle, let us know what protocol/filter/commands you have set and we can add it to one of our examples.

<a name="j2534toOBDX"></a>
### 1.3. Can I use J2534 Commands? ###

For J2534 developers looking to move to mobile devices, we have also made examples which use a slightly altered J2534 command set to allow for a simple switch over from dedicated J2534 DLLs to direct communication to the scantool without having to rewrite huge amounts of an application. There are some differences since our library is ASYNC which means there have been changes to input/outputs of functions, but all function names and capabilities remain the same.


[========]


<a name="SupportedScantoolsAndFunctions"></a>
## 2. Support Scantools and Functions ##
This section indicates all the supports scantools and functions, this section will be updated as new scantools are released.
If a scantool does not support a specific action, it will report "unsupported" as the error code. 

<a name="SupportedOBD2Protocols"></a>
### 2.1. Supported OBD2 Protocols ###
Indicated below is each OBD protocol supported on each available scantool. Knowing the supported protocol is important for when using the library as you will only be able to utilize protocols available on that tool. Some tools may have functions that are specific to that protocol (ie. GMLAN High Voltage Wakeup) which will only be supported on tools that support GMLAN.

  | OBDX Pro VT  | OBDX Pro VX | OBDX Pro GT | OBDX Pro FT
------------- | ------------- | ------------- | ------------- | -------------
HSCAN (pin 6/14)   | No  | Yes  | Yes  | Yes
MSCAN (pin 3/11)   | No  | No  | No  | Yes
GMLAN (pin 1)   | No  | No  | Yes  | No
J1850 VPW (pin 2)   | Yes  | Yes  | Yes  | No
GM UART ALDL (Pin 9)   | No  | Yes  | Yes  | No
Ford 18v FEPs (Pin 13)   | No  | No  | No  | Yes

<a name="protocolspecificcommands"></a>
### 2.2. Protocol Specific Commands ###
Some protocols have specific commands to perform special abilities. This could include features such as:
- Changing to a different baud rate
- Performing a high voltage wakeup
- Engaging relays to switch CAN (HS to MS)
- Controlling FEPs
- And any other protocol specific ability

(To be completed)



<a name="customobdxdll"></a>
### 2.3. Custom OBDX DLL/Nuget ###
For developers looking to modify or create their own versions of the OBDX DLL/Nuget, please contact us to get a copy of the source code. We do highly recommend using the provided DLL as this ensures you have a functioning backend for Scantool communication to minimise any problems.

[========]

<a name="IssuesAndSupport"></a>
## 3. Issues and Support ##
If you come across an issue while using the templates, please open an issue in github so we can try replicate the issue on our end. Do ensure to include exactly what scantool you are using and what ECU/Vehicle/OBD2 protocol you are also using.

Our OBDX library does have a option to enable debugging, this will print out information to the console log which will help trace back any possible issues that may occur within the DLL, we will ask to have a log provided to trace any issues.

[========]

<a name="CommonQandA"></a>
## 4. Common Questions and Answers ##
**Q: Can we use the templates in commercial products?**
A: You absolutely can! The purpose of these tempaltes is to give developers a big kickstart with known working examples.

- - - -

**Q: Can we request specific examples?**
A: Definitely! Our examples are limited to show how to use the scantool and its available commands, we do not write entire projects to do actions such as reflashing cars.

- - - -

**Q: I have an error and cannot figure out the issue, can you help?**
A: We can do our best to help, but we do rely on you to do majority of the debugging and identifying the issue. We cannot help fix problems due to programming errors in your own application, but if the problem is within the obdx library, we can get this fixed and updated quickly.

- - - -

**Q: I need a custom function added to the OBDX Library, can you add it?**
A:The obdx library is specifically designed to expose all the OBDX commands available in an easy to use format. Any custom commands for communciating with a vehicle should be done in their own funtion/class. We will update the OBDX library to have new commands as more advanced tools are released.

- - - -

**Q: Can you make an example in (enter common programming IDE)?**
A: Our current supported IDEs and templates are the only ones we will maintain and produce at this time. You can make requests for other languages/IDEs, as the more requests we get, the more chance we will look into.

For developers that are proficient in another langauge/IDE and would like to contribute a template, please do forward a working template and information to have it added to our list! We can ensure your templates are properly credited to your hard work!

- - - -


Regards,
OBDX Pro Team
