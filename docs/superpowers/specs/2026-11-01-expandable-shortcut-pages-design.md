# Sayfalı Tuşlar ve Windows Kısayolları — Tasarım

## Amaç

Mevcut kompakt 3×2 Stream Deck panelini büyütmeden altıdan fazla tuş eklenebilmesini sağlamak. Kullanıcı `.exe` dosyalarının yanında masaüstü ve diğer klasörlerdeki Windows `.lnk` kısayollarını seçebilmeli; uygulama tuşunda emoji yerine seçilen dosyanın gerçek Windows simgesi otomatik gösterilmelidir.

## Başarı ölçütleri

- Panel her sayfada en fazla altı tuş gösterir ve mevcut boyutunu korur.
- Alt çubuktaki `+` düğmesi yeni tuş düzenleyicisini açar; Kaydet yalnızca geçerli tuşu listenin sonuna ekler.
- Önceki/sonraki kontrolleri ve `1 / 3` biçiminde sayfa göstergesi vardır; geçersiz yöndeki kontrol devre dışıdır.
- Sağ tıkla açılan mevcut düzenleyicide tuş silinebilir; kullanıcı silmeyi açıkça onaylar.
- Uygulama seçicisinde `.exe` ve `.lnk` dosyaları birlikte seçilebilir.
- `.lnk` dosyası Windows kabuğu üzerinden doğrudan açılır.
- `.exe` veya `.lnk` seçildiğinde Windows Shell tarafından sağlanan büyük simge otomatik olarak tuşta ve düzenleyici önizlemesinde görünür.
- Eski altı tuşlu `settings.json` dosyaları veri kaybı olmadan çalışmaya devam eder.
- Eklenen, düzenlenen ve silinen tuşlar yeniden başlatmada korunur.

## Kapsam dışı

- Donanımsal Stream Deck entegrasyonu.
- Klavye makroları veya açık uygulamaya tuş gönderme.
- Windows internet kısayolu `.url`, klasör, belge veya rastgele dosya açma.
- URL favicon indirme; URL tuşları mevcut emoji/metin simgesini kullanır.
- Sayfaları yeniden adlandırma, klasörleme, sürükle-bırak sıralama veya sayfa başına farklı ızgara boyutu.
- Simge görsellerini diske kopyalama veya ayar JSON'una gömme.

## Seçilen yaklaşım

### 1. Sayfalı 3×2 düzen — seçildi

Tüm tuşlar tek sıralı listede saklanır. Arayüz listeyi altışarlı sayfalara böler. Panelin alt çubuğunda önceki, sayfa göstergesi, sonraki ve ekle kontrolleri bulunur. Böylece overlay açık uygulamanın daha fazlasını kaplamadan istenen kadar tuş taşıyabilir.

### 2. Kaydırılabilir ızgara — elendi

Uygulaması basittir ancak çok sayıda tuşta konum hissini ve hızlı erişimi zayıflatır.

### 3. Boyutlandırılabilir panel — elendi

Daha fazla tuşu aynı anda gösterir fakat overlay'in kompakt kalması hedefiyle çelişir.

## Kullanıcı deneyimi

### Ana panel

- Üst alan ve 3×2 tuş ızgarası mevcut görünümünü korur.
- Alt çubukta soldan sağa `‹`, `1 / 1`, `›`, `+` kontrolleri ile kısa sağ-tık ipucu bulunur.
- Tek sayfada önceki/sonraki devre dışıdır.
- Son sayfa altıdan az tuş içerebilir; boş hücreler gösterilmez.
- Hiç tuş kalmadığında ızgara alanında “Henüz tuş yok” ve “+ ile ekleyin” mesajı görünür.
- Uygulama her başlangıçta ilk sayfayı açar; aktif sayfa kalıcı ayar değildir.
- Bir tuş silindiğinde mevcut sayfa artık yoksa kullanıcı en yakın geçerli sayfaya taşınır.
- Sol tıklama mevcut açma davranışını, sağ tıklama düzenlemeyi korur.

### Yeni tuş ekleme

- `+` yeni, kaydedilmemiş bir taslakla mevcut düzenleyiciyi açar.
- İptal hiçbir değişiklik yapmaz.
- Kaydet doğrulama ve ayar dosyasına güvenli yazma başarılı olduktan sonra tuşu listenin sonuna ekler ve yeni tuşun bulunduğu son sayfayı gösterir.
- Yeni tuş benzersiz GUID kimliği alır.

### Düzenleme ve silme

- Mevcut sağ-tık düzenleyicisi kullanılır.
- Yalnızca mevcut bir tuş düzenlenirken kırmızı “Tuşu sil” düğmesi görünür.
- Silme düğmesi tuş adını içeren bir onay sorusu gösterir.
- Onaydan sonra silme önce ayar dosyasına kaydedilir; kayıt başarısızsa tuş ekrandan kaldırılmaz.

### Uygulama ve kısayol seçimi

- Dosya filtresi: `Uygulamalar ve kısayollar (*.exe;*.lnk)|*.exe;*.lnk`.
- Uygulama eylemi yalnızca mevcut `.exe` veya `.lnk` hedefini kabul eder.
- Seçilen yol `Target` olarak saklanır ve `ProcessStartInfo(UseShellExecute = true)` ile açılır.
- `.lnk` çözümlemesi uygulamanın sorumluluğu değildir; Windows Shell hedefi ve çalışma klasörünü uygular.

### Otomatik simge

- `DeckButton` modeline geriye uyumlu, boş varsayılanlı `IconPath` eklenir.
- `.exe`/`.lnk` seçildiğinde `IconPath = Target` olur. URL seçildiğinde `IconPath` temizlenir ve mevcut emoji/metin alanı kullanılır.
- Windows Shell `SHGetFileInfo` ile var olan dosyanın büyük simgesini sağlar; `.lnk` için Shell'in gösterdiği kısayol/hedef simgesi kullanılır.
- Yerel simge servisi `HICON` kaynağını güvenle serbest bırakır, WPF `BitmapSource` üretip dondurur ve aynı oturumda yol bazında önbelleğe alır.
- Simge okunamazsa tuş çökmez; `Icon` emoji/metni yedek olarak gösterilir.
- Simge diske yazılmaz. Uygulama yeniden başladığında dosyadan tekrar okunur; böylece `.lnk` simgesi değişirse sonraki açılışta güncellenir.

## Veri modeli ve geçiş

- Altı tuş sınırı kaldırılır; `DeckSettings.Normalize()` artık fazla tuşları kesmez.
- Normalize, null kayıtları atar/onarır, benzersiz kimlikleri korur veya üretir ve eksik eski alanlara güvenli varsayılan verir.
- Mevcut altı varsayılan yalnızca tamamen yeni ayar dosyasında oluşturulur; kullanıcı tüm tuşları sildiyse Normalize bunları geri eklemez.
- `IconPath` eski JSON'da bulunmadığında boş kalır ve mevcut emoji görünümü devam eder.
- Yeni sayfa durumu modelde saklanmaz.

## Teknik yapı

- `DeckSettings`: sınırsız liste normalizasyonu ve yeni tuş fabrikası.
- `DeckPagination`: toplam sayı/aktif sayfa için saf sayfalama hesabı; UI'dan bağımsız test edilir.
- `ShellIconService`: `.exe`/`.lnk` Shell simgesini WPF görüntüsüne dönüştürür ve önbelleğe alır.
- `IconPathToImageConverter`: `IconPath` bağlamasını görüntüye çevirir; null sonuç emoji yedeğini görünür bırakır.
- `MainWindow`: görünür altı tuşu bağlar, sayfa kontrollerini günceller, ekleme/silme ve güvenli kayıt akışını yönetir.
- `EditButtonWindow`: yeni/mevcut kipleri, `.exe;*.lnk` seçimi, simge önizlemesi ve silme sonucu.
- `ActionLauncher`: `.exe` ve `.lnk` doğrulaması/açılması.

Harici NuGet paketi, MVVM çerçevesi veya COM kısayol çözümleyicisi eklenmez.

## Hata davranışı

- Geçersiz veya kaybolmuş `.lnk`/`.exe` hedefi mevcut Türkçe doğrulama/açma hatasını gösterir.
- Shell simgesi alınamazsa tuş çalışmaya devam eder ve emojiye döner.
- Ekleme, düzenleme veya silme sırasında ayar kaydı başarısızsa görünür liste değişikliği geri alınır ve Türkçe hata gösterilir.
- Bozuk ayar dosyası kurtarma davranışı aynen korunur.

## Doğrulama

- Birim testleri: altıdan fazla tuşun korunması; eski JSON geçişi; benzersiz kimlik; 0, 1, 6, 7 ve çoklu sayfa hesapları; ekleme/silme sonrası sayfa sıkıştırma; `.exe`/`.lnk` kabulü ve diğer uzantıların reddi.
- Servis testi: bilinen bir Windows `.exe` dosyasından simge elde etme ve eksik dosyada null/fallback.
- Yerel duman testi: masaüstü `.lnk` seçme, gerçek simgeyi görme, kısayolu açma, 7+ tuş ekleme, sayfalar arasında geçme, silme/iptal, yeniden başlatma kalıcılığı.
- Release test/build/publish ve yayımlanmış `.exe` ile tekrar duman testi.

## Uygulayıcının kararına bırakılanlar

- Alt çubuk düğmelerinin kesin boşluk ve renk tonları.
- Sayfa değişiminde kısa animasyon kullanılıp kullanılmaması; animasyon eklenirse 150 ms'yi geçmemeli.
- Simge önbelleğinin basit sözlük iç yapısı; süreç ömrünü aşmamalı.
