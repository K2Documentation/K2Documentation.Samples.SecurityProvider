# K2Documentation.Samples.SecurityProvider
Sample projects that demonstrate how to extend the K2 platform by implementing a custom security provider/user manager. 

Find more information in the K2 Developers Reference here:
https://help.k2.com/onlinehelp/k2five/DevRef/current/default.htm#Extend/Svr/User-Manager.htm

This project contains sample code that demonstrates how to implement a custom user manager. 

## Prerequisites
The sample code has the following dependencies: 
* .NET Assemblies (the K2 client-side tools install includes the assemblies and they are also included in the project's References folder)
  * SourceCode.Hosting.Server.Interfaces

## Getting started
* Use these code snippets to learn about building basic custom controls. This project demonstrates how to extend the basic custom control. 
* There are two CS projects in this solution: the first (K2Documentation.Samples.SecurityProvider.SampleSecurityProvider) is a 'skeleton' project with the basic implementation, and the second (SourceCode.Security.Providers.XmlFileProvider) is a sample project that uses an XML schema to return user information. 
* Note that these projects are only intended as sample code. They may compile but may not actually run as-is. You will need to edit the code snippets and SQL scripts to work in your environment. 
* Fetch or Pull the appropriate branch of this project for your version of K2. 
* Consider the Master branch the latest, up-to-date version of the sample project. Branches contain older versions. For example, there may be a branch called K2-Five-5.0 that is specific to K2 Five version 5.0. There may be another branch called K2-Five-5.3 that is specific to K2 Five version 5.3. Assume that the master branch represents the latest release version of K2 Five. 
* The Visual Studio project contains a folder called "References" where you can find the referenced .NET assemblies or other uncommon assemblies. By default, try to reference these assemblies from your own environment for consistency, but we provide the referenced assemblies as a convenience in case you are not able to locate or use the referenced assemblies in your environment. 
* The Visual Studio project contains a folder called "Resources". This folder contains additional resources that may be required to use the same code, such as K2 deployment packages, sample files, SQL scripts and so on. 
   
## License
This project is licensed under the MIT license. Find the MIT license in LICENSE.

## Notes
 * The sample code is provided as-is without warranty.
 * These sample code projects are not supported by K2 product support. 
 * The sample code is not necessarily comprehensive for all operations, features or functionality. 
 * We only accept code contributions that are compatible with the MIT license (essentially, MIT and Public Domain).

