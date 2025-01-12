# TinyRadius.NET

TinyRadius.NET is a .NET reimplementation of the TinyRadius project written in Java. This project enables you to develop applications that work with the RADIUS protocol. RADIUS (Remote Authentication Dial-In User Service) is a widely used protocol for authentication and authorization.

## Features

- **RADIUS Requests**: Easily create and process authentication and accounting requests.  
- **CoA (Change of Authorization)** and **Disconnect** support.  
- **Flexible Configuration**: Customizable structure using dictionary files.  
- **Easy Integration**: Compatible with modern .NET projects.  

## Installation

### 1. Source Code
Start by cloning the project from the GitHub repository:
```bash
git clone https://github.com/utkugurler/TinyRadius.NET.git
```

### 2. NuGet Package (Optional)
The project will be released as a NuGet package in the future. Once available, you can add it using the following command:
```bash
dotnet add package TinyRadius.NET
```

## Usage

### Example: Sending an Authentication Request
```csharp
RadiusClient client = new RadiusClient("127.0.0.1", "secret");
AccessRequest request = new AccessRequest("testuser@turk.net", "123456789");
request.SetAuthProtocol(AccessRequest.AUTH_PAP);
request.AddAttribute("Framed-Protocol", "PPP");
request.AddAttribute("Connect-Info", "4294967295/0");
request.AddAttribute("NAS-Port-Type", "Virtual");
request.AddAttribute("NAS-Identifier", "CISCO|CGNAT|0");
request.AddAttribute("Service-Type", "Framed-User");
request.AddAttribute("NAS-IP-Address", "192.168.1.1");
RadiusPacket result = client.Authenticate(request);
```

### Example: Sending a Disconnect (CoA) Request
```csharp
CoaRequest coaRequest = new CoaRequest();
RadiusClient client = new RadiusClient("127.0.0.1", "secret");
coaRequest.SetPacketType(RadiusPacket.DISCONNECT_REQUEST);
coaRequest.AddAttribute("User-Name", "testuser@turk.net");
RadiusPacket response = client.Communicate(coaRequest, coaPort);
```

## Contributing
- Report bugs or suggest features using the [Issues](https://github.com/utkugurler/TinyRadius.NET/issues) tab.

## License
This project is licensed under the MIT License. For more details, see the `LICENSE` file.
