# Digital Stream Deck

Windows için sayfalara ayrılmış, özelleştirilebilir bir hızlı erişim paneli. Her sayfada en fazla altı tuş gösterilir.

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
- **+** ile tuş ekleyin. Altıdan fazla tuş olduğunda oklarla sayfalar arasında geçin; `N / M` geçerli ve toplam sayfa sayısını gösterir.
- Yapılandırılmış bir tuşa sol tıklamak web adresini veya uygulamayı açar. Bir tuşa sağ tıklayarak düzenleyin veya düzenleyicide **Sil** seçeneğiyle onay vererek silin.
- Düzenleyicide başlık, emoji, renk ve HTTP/HTTPS adresi veya mevcut bir `.exe` ya da `.lnk` dosyası seçin; **Kaydet** ile uygulayın veya **İptal** ile vazgeçin. Uygulama ve kısayollar için Windows simgesi otomatik kullanılır; simge alınamazsa seçtiğiniz emoji gösterilir.
- Başlık alanından sürükleyerek paneli taşıyın. `Esc` paneli gizler; sağ üstteki **×** ayarları kaydedip uygulamadan çıkar.

Düğmeler ve panel konumu `%LOCALAPPDATA%\DigitalStreamDeck\settings.json` dosyasında saklanır. Önceki altı tuşlu ayarlar okunmaya devam eder; eklenen tuşlar aynı dosyada saklanır.

## Sınırlamalar

Panelin ve kısayolun özel tam ekran uygulamalarda çalışacağı garanti edilmez. Yönetici olarak çalıştırılan pencerelerle etkileşim, Windows'un yetki sınırları nedeniyle kısıtlanabilir.
