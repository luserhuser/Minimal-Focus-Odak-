# Odak Widget

`Odak.exe` dosyasına çift tıkla. Kurulum, tarayıcı veya Node.js gerekmez. Windows ve .NET Framework 4.8 kullanır. Klasörü kalıcı bir yere taşıdıktan sonra otomatik başlatmayı açabilirsin.

## Günlük kullanım

- Uygulama doğrudan çerçevesiz widget olarak açılır. İki kart tek grup olarak taşınır. Kart başlıklarından sürükle; konum otomatik kaydedilir. Sağ tık veya ··· menüsünden konumu kilitleyebilirsin. İsteğe bağlı tam ekran Odak kartı menüden açılır; Escape widget’a döner.
- Kronometre 00:00:00’dan ileri sayar. Başlat, Duraklat / Devam et ve Sıfırla düğmeleri vardır. Sıfırla mevcut oturumu temizler; bugünkü toplam çalışma süresi korunur. Mola döngüsü ve Pomodoro hedefi yoktur. Tepside ve oyun modunda saymaya devam eder. Uygulamadan Çıkış ile ayrılınca durur; yeniden açıldığında kaldığı sürede duraklatılmıştır. Süre 30 saniyede bir ve işlemlerde kaydedilir; zorla kapatmada son 30 saniye kaybolabilir.
- Günlük liste en fazla üç görev alır. Metni yazıp Enter veya + ile ekle. Checkbox tamamlar, × siler, oklar sıralar. Tamamlanma hedefi üçtür.
- Gün değişince görevler ve günlük çalışma toplamı sıfırlanır. Çalışan kronometre kesilmez; gece yarısından sonraki süre yeni günün toplamına eklenir.
- Widget çerçevesizdir; Windows başlık çubuğu, küçült/büyüt/kapat düğmeleri ve görev çubuğu düğmesi yoktur. Kart araları saydamdır. Diğer uygulamaların üzerine sabitlenmez. Tepsi simgesine çift tıklama widget’ı gösterir; sağ tık → Çıkış tamamen kapatır. Konumu sıfırla seçeneği widget’ı ekranın sağ kenarına geri getirir.
- Oyun modu pencereyi gizler, zaman kontrolünü 15 saniyeye düşürür. Kronometre gerçek geçen süreye göre çalışmayı sürdürür. Ekran güncellemesi ve kayıt kontrolü 15 saniyede bir yapılır. Tepsiden Widget’ı göster ile oyun modunu kapat. Ses, bildirim ve oyun üzerine bindirme yoktur.
- Ayarlar → Windows oturum açılışında başlat seçeneği isteğe bağlıdır; varsayılan kapalıdır. Otomatik başlatmayı açtıktan sonra klasörü taşırsan bu seçeneği yeniden kaydet.

## Kaynak tüketimi ve sınırlar

Yerel WPF arayüz, yazılımla çizim, animasyonsuz ekran, tek zamanlayıcı  kullanır. [Microsoft RenderOptions belgesi](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.renderoptions.processrendermode) yazılım çizimi seçeneğini açıklar. Ölçülen kısa boşta çalışma sonucu `resource-check.txt` dosyasındadır; oyun FPS etkisi veya tüm Windows sürümleri için garanti değildir. Gerçek oyun testi yapılmadı.


## Kaynak kodu

`source/build.ps1` Windows’un .NET Framework C# derleyicisiyle EXE’yi üretir. `Odak.exe --self-test` temel davranış testlerini çalıştırır; `--preview` ekran görüntüsü, `--measure` 10 saniyelik boşta çalışma ölçümü üretir. Test ve ölçüm modları normal ayarları kaydetmez.

Kaldırmak için önce otomatik başlatmayı kapat, tepsiden Çıkış seç ve uygulama klasörünü sil. İstersen yerel veri klasörünü ayrıca silebilirsin.
