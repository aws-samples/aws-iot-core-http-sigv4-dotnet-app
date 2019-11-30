# 1. Overview

There are multiple options available for publishing and subscribing messages with AWS IoT Core. The message broker supports the use of the MQTT protocol to publish and subscribe and the HTTPS protocol to publish. Both protocols are supported through IP version 4 and IP version 6. The message broker also supports MQTT over the WebSocket protocol.

Here is a simple table that shows various protocol and port options available for handshake with AWS IoT Core.

|No |Protocol        |Authentication     |Port    |
|---|----------------|-------------------|------- |
| 1 |MQTT            |ClientCertificate  |8883,443|
| 2 |HTTP            |ClientCertificate  |8443    |
| 3 |HTTP            |SigV4              |443     |
| 4 |MQTT+WebSocket  |SigV4              |443     |

More details are available here https://docs.aws.amazon.com/iot/latest/developerguide/protocols.html

In this post, we'll cover the option #3 of leveraging HTTP protocol with AWS Sigv4 authentication for communicating with AWS IoT Core using .NET Framework and .NET Core. 

# 2. AWS IoT .NET Framework application using HTTP and AWS Sigv4 authentication
The following sub-sections 2a, 2b, and 2c offer guidance on creating a .NET Framework app that publishes messages to AWS IoT Core using HTTP and AWS Sigv4 authentication.

## 2a. Development environment
- Windows 10 with latest updates
- Visual Studio 2017 with latest updates
- Windows Subsystem for Linux 

## 2b. Visual Studio Solution & Project

Open the solution file located at 'Dotnet\aws-iot-http-sigv4-dotnet-app\aws-iot-http-sigv4-dotnet-app.sln' and navigate to the Program.cs class.

Take a look at the PublishMessageToTopic method and change the URI variable to your AWS IoT custom endpoint.  Next, navigate to the App.config file and substitute your access key and secret key in the respective appsettings sections.  Note that the access key and secret key should not be checked in to source control.

Navigate back to the Program.cs class and we'll walk through this sample's behavior.  When this application is run, it enters an infinite loop that publishes a message to a topic every 5 seconds.  The topic is defined by the "topic" variable in the Main method.  The Main method then invokes the PublishMessageToTopic method with the JSON payload and the destination topic.

The PublishMessageToTopic method constructs the HTTP request by first building the headers with SigV4 authentication and then invoking the HTTP request.  The BuildHeaders method constructs the necessary headers for SigV4 authorization by using the classes found in the signers folder.  For more information on the details of the SigV4 process, see this link: https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html

The AWS IoT Core service then verifies that the SigV4 signature is valid and either allows or denies the request.

## 2c. Compile, Run and Verify messages sent to AWS IoT Core
Compile and run the solution. You should see that messages are published successfully from .NET app to AWS IoT Core.

![](/images/pic1.jpg)

You should also see the messages getting published in the AWS IoT Test Console.

![](/images/pic2.png)


# 3. AWS IoT .NET Core application using HTTP and AWS Sigv4 authentication
The folowing sub-sections 3a,3b,3c,3d,3e and 3f offer guidance on implementing a .NET core app that publishes messages to AWS IoT Core using
HTTP and AWS Sigv4 authentication.

## 3a. Development environment
- Mac OS with latest updates 
- .NET Core 2.1 or higher
- Visual Studio for Mac

## 3b. Visual Studio Project

Open the solution file located at 'Dotnet Core\aws-iot-http-sigv4-dotnet-app\aws-iot-http-sigv4-dotnet-app.sln' and navigate to the Program.cs class.

Take a look at the PublishMessageToTopic method and change the URI variable to your AWS IoT custom endpoint.  Next, navigate to the appsettings.json file and substitute your access key and secret key.  Note that these settings should not be checked in to source control.

Navigate back to the Program.cs class and we'll walk through this sample's behavior.  When this application is run, it enters an infinite loop that publishes a message to a topic every 5 seconds.  The topic is defined by the "topic" variable in the Main method.  The Main method then invokes the PublishMessageToTopic method with the JSON payload and the destination topic.

The PublishMessageToTopic method constructs the HTTP request by first building the headers with SigV4 authentication and then invoking the HTTP request.  The BuildHeaders method constructs the necessary headers for SigV4 authorization by using the classes found in the signers folder.  For more information on the details of the SigV4 process, see this link: https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html

The AWS IoT Core service then verifies that the SigV4 signature is valid and either allows or denies the request.


## 3f. Compile, run and verify
Compile and run the solution. You should see that messages are published successfully from .NET app to AWS IoT Core.

![](/images/pic4.png)

You should also see messages getting published in AWS IoT Test Console.

![](/images/pic3.png)

## 4. Conclusion

In this post, we have developed a .NET framework app that publishes messages to AWS IoT Core using HTTP protocol and AWS Sigv4 authentication. We also developed a .NET core equivalent of the same in the later part of this.
This completes the post on building .NET framework and .NET core reference implementation for AWS IoT Core using HTTP and Sigv4.

## 5. License Summary

This sample code is made available under the MIT-0 license. See the LICENSE file.


