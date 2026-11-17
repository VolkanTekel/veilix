# Veilix · WinUI 3 deneme sürümü

Paneller ve yazılar Microsoft.UI.Xaml / WinUI 3 kullanır. Windows 10 2004 ve sonrası x64, .NET 10 Desktop Runtime gerekir. Bu bilgisayarda çalışma zamanı mevcut; Windows App SDK bileşenleri uygulamanın yanındadır.

## Bu sürüm

- Ekran kenarına bağlanan 14 DIP ters kıvrımlar; cam ve tema aynı şekle uyar. Geometri GPU üzerinde yumuşatılır, sert GDI bölgesi kullanılmaz. Kıvrımlar WinUI Path dolgularıdır. Cam modunda gövde ve iki kıvrım aynı canlı host-backdrop fırçasını, tek etkileşimsiz arka plan penceresinde paylaşır; WinUI üzerindeki renk geçişlerinin RGBA değerleri de aynıdır. Sabit gri renk tahmini kullanılmaz. Pencerenin dışı NOREDIRECTIONBITMAP ve GPU şekil kırpmasıyla saydamdır. Arka plan boyutu/konumu, ön yüz ve açılma/kapanma eğrisiyle eşlenir; Cam kapatılınca kaynakları bırakılır. Mika ana gövdede yerel sistem malzemesi, küçük kıvrımlarda şekle katılan nötr taban rengini kullanır.
- Açılıp kapanan gövde ve gölge aynı eğriyi izler. Hızlı yön değiştirmede hareketin mevcut yüksekliği kullanılır. Metinler büyütülüp küçültülmez.
- 2,5 fiziksel piksel ek içerik boşluğu. Sayfalar seçili ekranın çalışma alanına kadar uzar; uzun listeler kayar.
- Ayrı renk galerisi: 50 koyu ve 12 açık palet, Tümü/Koyu/Açık filtresi. Kartlar aynı fiziksel genişlikte ve 104 DIP yüksekliğinde; renk örnekleri 46 DIP ve tam genişlikte, isimler 14 DIP yarı kalındır. Açık temalarda metin ve yerel kontroller koyu yazıya geçer; okunurluk için açık zemin saydamlığı en az %82 tutulur. Özel temada renk ekleme/silme ve geçiş konumu; renk sayısında arayüz sınırı yoktur. Zemin renklerinde metin kontrastı doğrulanır.
- Simgelerin anlamına göre renk: mavi su, beyaz bulut, seviyesine göre yeşil/sarı/kırmızı pil. Saat hover alanı sabit; tarih ve hava yer değiştirmez.
- Gerçek çıkış sesine duyarlı, eşit fiziksel kalınlıkta sütunlar. Yükseklik değişimi Composition ölçeğidir; her karede metin veya sayfa düzeni hesaplanmaz. Örnekleme sınırı ekran Hz / 60–90–120 tercihinden düşük olandır. Kısa GPU geçişlerini Windows sunar; gerçek FPS garantisi verilmez.
- Bluetooth bağlantı olayları göstergeden kopan cihazı çıkarır. Sürücünün bildirmediği pil bilgisi uydurulmaz. Windows izleyiciyi sunamazsa PnP yenilemesi yedektir.
- Bluetooth halkası kaldırıldığında 28 DIP alanı korunur; saat ve tarih kaymaz. Aynı cihaz/pil durumu tekrar geldiğinde gösterge yeniden kurulmaz.
- Zil düğmesi bildirim merkezini açar. Son 100 bildirim yerel olarak saklanır; ses değişikliği gibi art arda gelen olaylar birleştirilir. Liste ilk açılışta 15 kayıt oluşturur. Bildirim ayarları sistem/ses/klavye/ağ/güç, görev, zamanlayıcı ve su tercihlerini içerir. Ses ve ada bildirimi ayrı tercihlerdir. Bu liste Veilix uyarıları ve izlenen sistem değişiklikleridir.
- Rafın “Klasörü temizle” işlemi onaydan sonra raf içindeki kopyaları Geri Dönüşüm Kutusu'na taşır; klasör sınırı doğrulanır, kopyalama sürerken çalışmaz. Onay penceresinin varsayılanı Vazgeç'tir.
- “Windows ile başlat” ayarı kullanıcıya özel `Veilix Desktop Widget` başlangıç görevini yönetir: oturum açıldıktan 12 saniye sonra, yönetici yetkisi istemeden, açık çalışma klasörüyle başlar. Hata durumunda sınırlı yeniden deneme vardır. Bu kurulumun eski Veilix/AdaDesktop Run kayıtları kaldırılır.
- Spotify’a özel oturum seçimi ve bağlantı kurtarma mantığı korunur; Spotify kapanınca son şarkı kalır. Medya, Su, Kontroller, Zaman, Görevler ve Raf sayfaları; günlük hava kartı ve ekran ayarları.

Orijinal WPF sürümü `app/bin` altında korunur. WinUI denemesi ilk kurulumda eski `data` klasörünün kopyasını alır; sonraki çalıştırmalarda kendi verisini kullanır. Raf dosyalarının yolu aynı Belgeler/Veilix/Raf klasörüdür. Dosyayı raftan kaldırmak kopyayı silmez. Hatırlatmalar uygulama açıkken çalışır.

Kapaklar en fazla 512 piksel PNG verisi olarak çözülür ve WinUI'ye doğrudan aktarılır; yeniden WPF ile kodlanmaz. WinUI sürümü WPF derlemelerini yüklemez. Spotify kapak kırpma, son şarkı kayıt biçimi ve bağlantı kurtarma korunur. Sistem tepsisi ve Windows bildirim balonları yerel Windows yardımcılarıyla çalışır.

## Geliştirme

`./winui/dev.ps1 Build|Run|Inspect|Test|Benchmark|Publish|Stop`

Derleme için proje içindeki `.tools/dotnet` SDK kullanılır. WinUI, Foundation ve InteractiveExperiences bileşenleri doğrudan seçilmiştir; kullanılmayan AI/ML/arama/widget paketleri sürüme eklenmez. WebView çalıştırılmaz.

Testler gerçek ses/parlaklık düzeyini değiştirmez; sahte medya oturumları ve geçici yardımcı araç verileri kullanır. Raporlar çıktı klasörünün `qa` dizinindedir. Test başarısı gerçek masaüstündeki cam görünümünün görsel onayı yerine geçmez.




Saat ve ses sütunları aynı 180ms ilerleme kaynağını paylaşır; hızlı hover değişiminde ara konumdan devam eder. Çıkış solması bitmeden ses örneklemesi dondurulmaz. Sıfır/çok düşük sinyalde sütunlar sabit tabana döner; duraklatmada örnekleme kapanır, sessiz çalan oturumda düşük sıklıkta sinyal denetlenir.

Arayüz callback tanılaması data/ui-faults.log içinde kaynak iş, HRESULT ve stack tutar; kayıt sınırlıdır. Gözlenen geçersiz parametre ve kapanmış UI nesnesi hataları callback sınırında ele alınır; beklenmeyen hatalar gizlenmez. İzole zorlayıcı test için Veilix.exe --stability-test kullanılır; kasıtlı test hataları yalnız qa klasörüne yazılır.


Bluetooth cihazları: pil halkası, Kontroller bağlantısı, sayfa okları veya tepsi menüsü ile açılır. Kayıtlı cihazlar listelenir; bağlı/bağlanıyor/hata durumları ve varsa pil bilgisi gösterilir. Tek mevcut AEP watcher yeniden kullanılır; klasik cihazların Windows önbelleği arka planda ve sorgulama yapmadan okunur. Panel cihaz listesini bekleyerek arayüzü kilitlemez.

Klasik Bluetooth ses aygıtlarında Veilix, eşleşen ses sürücüsüne Windows'un doğrudan bağlanma/kesme isteğini gönderir. Aygıt eşleşmesi kapsayıcı kimliği veya Bluetooth adresi üzerinden yapılır. Sürücü desteklemiyorsa ya da önceki sürümden geri yüklenecek profiller varsa Win32 profil yöntemi kullanılır. Başarı yalnız Windows bağlantıyı doğruladığında gösterilir. Desteklenen BLE aygıtlarında bağlantı isteği GATT oturumu açar; normal izleme aygıtlara bağlanmaz.

Simgeler nötr beyaz/koyu kontrol simgeleri ve sınırlı etkinlik renkleri kullanır: Bluetooth/su mavi, bağlantı/şarj yeşil, zamanlayıcı turuncu, güneş sarı. Renkler seçili paletten bağımsızdır; açık temalarda okunabilir koyu karşılıkları kullanılır. Yeni Bluetooth vektörü mevcut Fluent System Icons sürümündendir.

Sistem bildiriminin ayrıntısı boşsa tepsi balonunda başlık kullanılır; hem başlık hem ayrıntı boşsa Veilix bildirimi yazılır. Böylece NotifyIcon.ShowBalloonTip boş metin ArgumentException hatası üretmez.


2026-10-30: Cam penceresi artık içerik penceresinin native owner'ıdır. Böylece Windows içerik penceresini camın üzerinde tutar; arka planın bağımsız TOPMOST kalıp yazıları örtmesi engellenir. Cam input hit-test sonucu HTTRANSPARENT'tır; WS_DISABLED kullanılmaz. Var olan 500ms örtüşme kontrolü, yalnız gerektiğinde kaybolan TOPMOST/katman bağını onarır. Malzeme değişiminde eski owner geri yüklenir ve WinUI penceresi korunur.

Bluetooth'ta bağlı cihazda Bağlantıyı kes, diğerinde Bağlan görünür. Sürücü isteğini desteklemeyen klasik cihazda bağlantı kesilirken yalnız seçili cihazın önceden kurulmuş servis profilleri kapatılır; eşleştirme silinmez. Bu profiller işlemden önce data/bluetooth-profiles.json içine yazılır ve sonraki Bağlan işleminde (uygulama yeniden başlamış olsa da) geri açılır. Bu yöntem sürücünün servis etkinleştirme API'sini kullanır; profiller Veilix'ten tekrar bağlanana kadar kapalı kalır. İşlem ve yeniden bağlanma başka cihazları etkilemez. BLE için Veilix'in açtığı GATT oturumu bırakılır; bağlantıyı Windows/başka uygulama tutuyorsa zorla kesildi denmez, Windows ayarlarına yönlendirilir. Başarı gerçek bağlantı durumu doğrulanınca gösterilir; donanım/sürücü hataları kodlarıyla yerel günlüğe yazılır. Donanımdaki kes-bağla testi kullanıcının isteğiyle yapılmamıştır.

Caps Lock bildirimi tamamen kaldırılmıştır. Eski system-caps kayıtları bildirim geçmişinde filtrelenir; klavye dili tercihi bağımsız biçimde korunur.
