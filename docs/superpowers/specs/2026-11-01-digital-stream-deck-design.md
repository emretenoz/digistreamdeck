# Dijital Stream Deck — Tasarım

## Amaç

Windows'ta açık uygulamaların üzerinde duran, `Alt + Mouse Button 4` ile gösterilip gizlenen küçük bir dijital Stream Deck oluşturmak. Kullanıcı altı tuşun her birini bir uygulama veya web adresi açacak şekilde düzenleyebilmeli. Uygulama tek kullanıcı için hızlı erişim aracı olacak; kurulumdan sonra internet ya da harici servis gerektirmeyecek.

## Başarı ölçütleri

- Uygulama ilk açılışta kullanılabilir bir 3×2 panel gösterir.
- `Alt + Mouse Button 4` tek basışta paneli görünür/gizli duruma geçirir.
- Panel normal ve kenarlıksız pencere modunda çalışan uygulamaların üzerinde kalır.
- Her tuş sol tıklamayla atanmış uygulamayı veya `http/https` URL'sini açar.
- Her tuş sağ tıklamayla düzenlenir; ad, emoji/simge metni, renk, eylem türü ve hedef değiştirilebilir.
- Ayarlar ve panel konumu uygulama yeniden başlatıldığında korunur.
- Geçersiz hedef ya da başlatma hatası uygulamayı çökertmez; kullanıcıya anlaşılır Türkçe hata gösterilir.

## Kapsam dışı

- Donanımsal Stream Deck cihazlarıyla entegrasyon.
- Makro dizileri, tuş gönderme, ses mikseri, eklenti sistemi veya bulut eşitleme.
- Gerçek tam ekran/özel (exclusive fullscreen) oyunların üzerinde görünme garantisi.
- Çoklu sayfalar veya altıdan fazla tuş.
- Otomatik Windows başlangıcı ve yükleyici; ilk sürüm çalıştırılabilir proje olarak teslim edilir.

## Anti-hedefler

- Sürekli tuş durumunu sorgulayan ve boşta CPU harcayan bir döngü kullanılmayacak.
- Kullanıcı ayarı sessizce silinmeyecek veya geçersiz hedef başarı gibi gösterilmeyecek.
- Gereksiz NuGet bağımlılığı ya da geleceğe dönük eklenti mimarisi eklenmeyecek.

## Seçilen yaklaşım

### 1. Yerel WPF + Win32 kancası — seçildi

.NET 10 WPF uygulaması, `WH_MOUSE_LL` düşük seviye fare kancasıyla `XBUTTON1` (Mouse Button 4) basışını dinler ve o anda `Alt` durumunu Windows API üzerinden kontrol eder. Eşleşen `Alt + XBUTTON1` olayı tüketilir ve alttaki uygulamaya iletilmez; böylece tarayıcı “geri” gibi ikinci bir eylem çalıştırmaz. Tek tıklama için debounce uygulanır. Kanca uygulama kapanırken mutlaka kaldırılır. Ayarlar standart `System.Text.Json` ile yerel kullanıcı verisi klasöründe tutulur.

Bu yaklaşım az kod ve sıfır harici paketle güvenilir global tetikleme sağlar.

### 2. Harici global giriş kütüphanesi — elendi

Win32 ayrıntılarını azaltır, ancak bu küçük kapsam için gereksiz paket ve sürüm bağımlılığı getirir.

### 3. Periyodik giriş sorgulama — elendi

Yazımı kolaydır; boşta kaynak tüketir, basış kaçırabilir ve olay tabanlı davranıştan daha kırılgandır.

## Kullanıcı deneyimi

### Ana panel

- Kenarlıksız, koyu temalı, yuvarlak köşeli ve `Topmost` bir pencere.
- Üst çubukta “STREAM DECK” başlığı, ayar ipucu ve kapatma düğmesi bulunur.
- Gövde 3 sütun × 2 satırdan oluşur.
- Her tuş renkli zemin, büyük emoji/simge metni ve kısa bir ad gösterir.
- Pencere üst çubuktan sürüklenebilir; son konumu kaydedilir.
- İlk açılışta ekran merkezinde ve görünürdür. Sonraki açılışlarda kayıtlı konum kullanılır.
- `Alt + Mouse Button 4` paneli açar/gizler. `Escape` paneli gizler. Kapatma düğmesi uygulamadan tamamen çıkar.

### Tuş düzenleme

Bir tuşa sağ tıklanınca küçük bir düzenleme penceresi açılır:

- Ad: boş olamaz, panelde taşmayacak makul uzunlukta sınırlandırılır.
- Simge: tek emoji veya kısa metin.
- Renk: sınırlı hazır renk paletinden seçim.
- Eylem: “Uygulama” veya “Web adresi”.
- Uygulama hedefi: dosya seçiciyle `.exe` seçimi; mevcut bir dosya olmalı.
- Web hedefi: yalnızca mutlak `http` veya `https` adresi.
- Kaydet/İptal: yalnızca geçerli veri kaydedilir; iptal mevcut ayarı değiştirmez.

Sol tıklama eylemi çalıştırır. Boş/yapılandırılmamış bir tuş sol tıklanınca doğrudan düzenleme penceresini açar.

### Başlangıç içeriği

Altı tuş, kullanıcıya yapıyı gösterecek örnek ad/renklerle gelir; güvenli olmayan veya makineye özel uygulama yolu varsayılmaz. URL örnekleri geçerli web adresleri olabilir, uygulama tuşları ise kullanıcı seçene kadar yapılandırılmamış kalır.

## Veri ve hata davranışı

- Ayar dosyası `%LocalAppData%\DigitalStreamDeck\settings.json` altında tutulur.
- Kaydetme önce geçici dosyaya yazılıp sonra hedef dosyanın yerini alacak şekilde yapılır; yarım yazım riski azaltılır.
- Dosya yoksa varsayılanlar oluşturulur.
- Dosya okunamıyor veya JSON bozuksa uygulama varsayılanlarla açılır ve kullanıcıya ayarların yüklenemediği bildirilir; bozuk dosya otomatik silinmez.
- Uygulamalar ve URL'ler `ProcessStartInfo` ve `UseShellExecute = true` ile Windows kabuğu üzerinden açılır.
- Başlatma hataları kullanıcı dostu mesajla gösterilir; ham istisna metni ana mesaj olarak sunulmaz.

## Teknik yapı

- `DigitalStreamDeck.App`: WPF başlangıcı ve yaşam döngüsü.
- `MainWindow`: panel, tuşlar, sürükleme, görünürlük ve düzenleme akışı.
- `EditButtonWindow`: doğrulamalı tuş düzenleyici.
- `GlobalShortcutService`: fare kancası, Alt kontrolü, debounce ve temizleme.
- `SettingsService`: JSON yükleme/kaydetme ve güvenli varsayılanlar.
- `ActionLauncher`: uygulama/URL doğrulama ve başlatma.
- `DeckSettings` / `DeckButton`: altı tuş ve pencere konumu modelleri.

MVVM çerçevesi veya bağımlılık enjeksiyonu paketi eklenmeyecek; küçük kapsam için WPF veri bağlama ve açık sınıf bağımlılıkları kullanılacak.

## Platform sınırlamaları

- Hedef Windows 10/11 x64 ve `.NET 10` olacaktır.
- Overlay normal masaüstü ve kenarlıksız pencere uygulamalarında üstte kalır. Exclusive fullscreen, Windows güvenli masaüstü veya bazı korumalı/yükseltilmiş pencerelerde üstte kalması garanti edilmez.
- Mouse Button 4, Windows `XBUTTON1` olarak yorumlanır.

## Doğrulama

- Birim testleri: URL doğrulama, uygulama yolu doğrulama, varsayılan ayarlar, JSON gidiş-dönüşü ve bozuk ayar dosyası davranışı.
- Yerel duman testi: uygulamayı açma, paneli sürükleme, konumu yeniden başlatmada koruma, `Alt + Mouse Button 4` ile aç/kapa, URL açma, bir `.exe` seçip çalıştırma, hatalı hedefte hata gösterme.
- Görsel kontrol: 3×2 düzen, metin taşması, sağ tık düzenleme ve farklı Windows ölçeklendirme oranları.

## Uygulayıcının kararına bırakılanlar

- Koyu tema içindeki kesin renk tonları, boşluklar ve animasyon süresi.
- Örnek URL'lerin hangileri olacağı.
- Test projesinin adlandırması ve test çatısı; ek bağımlılığı en aza indiren güncel, desteklenen seçenek kullanılmalı.
