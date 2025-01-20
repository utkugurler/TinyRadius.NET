# TinyRadius.NET  

TinyRadius.NET, TinyRadius Java projesinin .NET için yeniden uygulanmış halidir. Bu proje, RADIUS protokolü ile çalışan uygulamalar geliştirmenizi sağlar. RADIUS (Remote Authentication Dial-In User Service), kimlik doğrulama ve yetkilendirme için yaygın olarak kullanılan bir protokoldür.

## Özellikler  

- **RADIUS İstekleri**: Kimlik doğrulama ve muhasebe isteklerini kolayca oluşturun ve işleyin.  
- **CoA (Change of Authorization)** ve **Disconnect** desteği.  
- **Esnek Yapılandırma**: Sözlük dosyaları kullanarak özelleştirilebilir yapı.  
- **Kolay Entegrasyon**: Modern .NET projeleriyle uyumlu.  

## Kurulum  

### 1. Kaynak Kod  
Projeyi GitHub deposundan klonlayarak başlayın:  
```bash
git clone https://github.com/utkugurler/TinyRadius.NET.git
```

### 2. NuGet Paketi (Opsiyonel)  
Proje gelecekte bir NuGet paketi olarak yayınlanacaktır. Yayınlandığında aşağıdaki komutla ekleyebilirsiniz:  
```bash
dotnet add package TinyRadius.NET
```

## Kullanım  

### Örnek: Kimlik Doğrulama İsteği Gönderme  
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

### Örnek: Disconnect (CoA) İsteği Gönderme  
```csharp
CoaRequest coaRequest = new CoaRequest();
RadiusClient client = new RadiusClient("127.0.0.1", "secret");
coaRequest.SetPacketType(RadiusPacket.DISCONNECT_REQUEST);
coaRequest.AddAttribute("User-Name", "testuser@turk.net");
RadiusPacket response = client.Communicate(coaRequest, coaPort);
```

## Katkıda Bulunma  
- Hataları bildirmek veya özellik önerilerinde bulunmak için [Issues](https://github.com/utkugurler/TinyRadius.NET/issues) sekmesini kullanabilirsiniz.

## Lisans  
Bu proje MIT Lisansı ile lisanslanmıştır. Daha fazla bilgi için `LICENSE` dosyasına bakabilirsiniz.  
