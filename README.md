GoogleTestRunner
================

GoogleTestRunner Visual Studio 2013-2015 extension / unit testing adapter.

![GoogleTestRunner UI](/data/vs_googletestrunner_screenshot.png)


Installation
-------------
1. Download software installer from http://visualstudiogallery.msdn.microsoft.com/9dd47c21-97a6-4369-b326-c562678066f0

Usage
-------------
1. Open Test Explorer from "Test | Windows" menu
2. GoogleTestRunner will find tests from executables that end with "test" or "tests"
3. You can run or start debugging single or multiple tests by right clicking them
4. Extension output can be seen from Output tab, by selecting "Show output from: Tests"

Development
-------------
1. Install Microsoft Visual Studio 2015 SDK 
    * Instructions here https://msdn.microsoft.com/en-us/library/bb166441%28v=vs.140%29.aspx
2. Build & install with VS2015
3. To debug the unit test adapter, attach to process vstest.discoveryengine.x86.exe or vstest.executionengine.x86.exe

Contributors
-------------
Markus Lindqvist, Bryan Roth, Veli-Matti Visuri
