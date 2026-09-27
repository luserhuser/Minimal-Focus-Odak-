# Odak

Windows için minimal, çerçevesiz bir odak widget'ı. Kronometre ve günlük görevleri sade, dikkat dağıtmayan bir arayüzde bir araya getirir.

## Özellikler

- Başlatma, duraklatma ve sıfırlama destekli odak kronometresi; günlük çalışma toplamı.
- Günlük en fazla üç adım/görev; tamamlama, silme ve sıralama.
- Sürüklenebilir koyu arayüz, sistem tepsisi ve oyun sırasında gizleme.

## Ekran görüntüsü

Kişisel veri içermeyen bir ekran görüntüsü henüz eklenmedi. Görseli `docs/odak-widget.png` konumuna koyup bu bölüme ekleyebilirsiniz. Otomatik üretilen `preview.png` kişisel görevler içerebildiği için Git dışında tutulur.

## Yerelde çalıştırma

Windows ve .NET Framework 4.8 gerekir. PowerShell'de proje klasöründen:

```powershell
.\source\build.ps1
.\Odak.exe
```

Yeniden derlemeden önce çalışan uygulamayı sistem tepsisi → **Çıkış** ile kapatın. Visual Studio, .NET SDK veya paket kurulumu gerekmez.

Mevcut davranış testlerini çalıştırmak için:

```powershell
$test = Start-Process .\Odak.exe -ArgumentList '--self-test' -Wait -PassThru
if ($test.ExitCode -ne 0) { throw 'Test başarısız.' }
Get-Content .\test-results.txt
```

## Teknoloji ve veriler

C#, WPF ve .NET Framework; derleme Windows'un Framework C# derleyicisiyle yapılır. Harici paket veya ağ bağlantısı kullanılmaz. Görevler ve ayarlar `%LOCALAPPDATA%\Odak` altında yerel tutulur; bu klasörü paylaşmayın. Ayrıntılar: [Kullanım](KULLANIM.md).

## Lisans

Bu proje GNU General Public License v3.0 (`GPL-3.0-only`) ile lisanslanmıştır. Tam metin: [LICENSE](LICENSE).
