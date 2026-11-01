# Digital Stream Deck

Windows için altı özelleştirilebilir düğmeli bir hızlı erişim paneli.

## Çalıştırma

Windows x64 ve .NET 10 Desktop Runtime gereklidir. Yayın klasöründeki `DigitalStreamDeck.exe` dosyasını çalıştırın. Kaynaktan çalıştırmak için .NET 10 SDK ile:

```powershell
dotnet run --project src/DigitalStreamDeck/DigitalStreamDeck.csproj -c Release
```

Test etmek ve Windows x64 için framework-dependent tek dosya halinde yayınlamak için:

```powershell
dotnet test DigitalStreamDeck.slnx -c Release
dotnet publish src/DigitalStreamDeck/DigitalStreamDeck.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o outputs/DigitalStreamDeck
```

## Kullanım

- `Alt + Mouse Button 4` (genellikle farenin geri/yan düğmesi) paneli gösterir veya gizler.
- Yapılandırılmış bir kutuya sol tıklamak web adresini veya uygulamayı açar. Boş bir kutuya sol tıklamak düzenleyiciyi açar; herhangi bir kutuya sağ tıklayarak da düzenleyebilirsiniz.
- Düzenleyicide başlık, simge, renk ve HTTP/HTTPS adresi veya mevcut bir `.exe` dosyası seçin; **Kaydet** ile uygulayın veya **İptal** ile vazgeçin.
- Başlık alanından sürükleyerek paneli taşıyın. `Esc` paneli gizler; sağ üstteki **×** ayarları kaydedip uygulamadan çıkar.

Düğmeler ve panel konumu `%LOCALAPPDATA%\DigitalStreamDeck\settings.json` dosyasında saklanır.

## Sınırlamalar

Panelin ve kısayolun özel tam ekran uygulamalarda çalışacağı garanti edilmez. Yönetici olarak çalıştırılan pencerelerle etkileşim, Windows'un yetki sınırları nedeniyle kısıtlanabilir.
