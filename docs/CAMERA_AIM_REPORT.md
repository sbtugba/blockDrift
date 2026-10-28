# Block Rift — üstten kamera ve kadraj düzeltmesi

> Durum (2026-10-28): Kamera teknik ölçümleri korunmuştur. Arşivdeki TopDown görseli kamera düzeltme aşamasına aittir; mevcut uygulanmış Level 1 görüntüsü [Gameplay_6x6_GameView.png](Gameplay_6x6_GameView.png) dosyasındadır.

Tarih: 27 Ekim 2026. Yalnız mevcut `BlockRift_Gameplay` sahnesinin kamera/sunum kadrajı düzenlendi.

## Sonuç

Kamera perspective olarak korundu. Gerçek board geometrisinin bounds merkezi hedeflenir; kamera konumu bu merkezin pozitif Y tarafında hesaplanır ve `Transform.LookAt(target, Vector3.up)` uygulanır. BoardRoot, PiecesRoot veya modeller döndürülmedi/ölçeklenmedi. Aynı tahta ve yedi parça, aynı hücre konumlarında kalır.

| 1080×1920, tam ekran safe area | Ölçülen değer |
|---|---|
| Dünya konumu | **(0, 31.61775, −6.72778)** |
| Dünya Euler dönüşü | **(78°, 0°, 0°)** |
| Dünya forward yönü | **(0, −0.978148, 0.207912)** |
| Gerçek board hedef merkezi | **(0, −0.034, 0)** |
| Hedefe uzaklık | **32.35887 birim** |
| Forward ile hedef yönünün dot değeri | **1.000000** |
| Vertical FOV | **22°** |
| Near / far | **0.1 / 100** |
| Gerçek board mesh'inin yatay ekran kaplaması | **%86.003** |
| Tüm tahta/parça görsellerinin yatay kaplaması | **%87.038** |
| Üst kenar / alt kenar projeksiyon genişliği | **0.962140** |

Kameranın Y'si hedef Y'sinden büyüktür ve forward Y negatiftir: tahtanın altından bakış yoktur. Dönüşten bağımsız `LookAt` quaternion karşılaştırması da test edildi. Bu konum, ekran veya tahta boyutu değiştiğinde dinamik olarak yeniden hesaplanır; her cihazda aynı sabit pozisyona zorlanmaz.

## Referansla karşılaştırma

Önceki 50°/32° FOV görünümünde, tahta düzleminin üst kenarı alt kenardan yaklaşık %14 daha dar görünüyordu. Yeni 78°/22° FOV ve daha uzak kamera ile fark **%3.8** oldu. Hücrelerin üst yüzleri daha belirgin, üst/alt sıraların görünür boyutları birbirine daha yakındır; referanstaki neredeyse tepeden, hafif perspektifli görünüme yaklaşılır.

1080×1920 gerçek Unity Game View/Play Mode görüntüsü alındı ve görsel olarak incelendi: `docs/archive/screenshots/Gameplay_6x6_TopDown.png`. Tahta bütünü görünür; kenarlarda kesilme yoktur. Üst sayaçlar ve alt Undo/Restart/Hint alanları için boşluk korunur. Kullanıcı referansındaki farklı parça renkleri/dizilimi bu kamera çalışmasında değiştirilmedi.

BoardViewport safe-area anchor'ları `(0.01,0.26)–(0.99,0.82)`, kadraj payı 1.02'dir. Kamera hedefini merkezden saptırmadan tahtayı ekranda konumlandırmak için off-axis perspective görüntü penceresi kullanılır. Projeksiyon mesafesi bütün sunum zarfı köşelerinin derinliklerine göre hesaplanır. Üst HUD ve alt kontrol alanları ile örtüşme test edildi.

Arka plan PNG'si ve materyali aynı kaldı. Mevcut `BackgroundSpriteFitter`, kaydırılmış kamera görüntü penceresinin merkezini ve boyutunu `ViewportToWorldPoint` ile izler; böylece aynı arka planın ekrandaki kaplaması korunur. Yeni çevre/zemin/mesh veya farklı bir görsel üretilmedi.

## Korunanlar

- Art ve Content altındaki **76 dosyanın işlem öncesi/sonrası SHA-256 hash'i aynı**: board/oyun taşı FBX'leri, prefablar, materyaller, texture'lar, katalog ve mevcut meta dosyaları.
- BoardRoot ve bütün alt Transform'ların dünya matrisleri sahne bakım yordamında ve testte karşılaştırıldı: değişiklik yok.
- Grid koordinat dönüşümü, 1 hücre = 1 Unity birimi, 5×5/6×6 desteği korundu.
- Core, Core testleri, GDD ve ana paket manifestinde diff bulunmadı. Level Generator'a dokunulmadı.
- GDD §§3.1 ve 11 ile mevcut 5×5/6×6 ve sunum/Core ayrımı korunur. Mimari skill'indeki eski orthographic tercih yerine kullanıcının açık perspective talebi uygulanır; oyun kuralı değişmez.

## Değişen dosyalar

- `Assets/_BlockRift/Presentation/Camera/GameplayViewportLayout.cs`: gerçek board merkezi için açık `focusRoot` referansı; merkez hedefli LookAt; FOV/mesafe ile perspektif kadraj; kamera yönünü bozmayan görüntü penceresi kaydırması. Orthographic yol korunur ve custom projection bu moda geçerken sıfırlanır.
- `Assets/_BlockRift/Presentation/Camera/BackgroundSpriteFitter.cs`: aynı görüntünün kaydırılmış frustum'a göre kamera sunumuna oturması. Arka plan asseti/materiali değişmez.
- `Assets/Scenes/BlockRift_Gameplay.unity`: yalnız kamera/projeksiyon, focusRoot, BoardViewport ve kamerayı izleyen arka planın türetilmiş sahne Transform'u.
- `Assets/_BlockRift/Editor/GameplayCameraCorrection.cs`: izole doğrulama için tek amaçlı kamera bakım/ölçüm yordamı.
- `Assets/_BlockRift/Tests/Presentation/CameraAimTests.cs`: sekiz yeni kamera test vakası.
- `Assets/_BlockRift/Tests/Presentation/BoardPerspectiveTests.cs`: önceki 50°/32° sahne tercihi yerine yeni 78°/22° tercihini doğrulayan mevcut sahne beklentisi. Diğer kontroller korundu.
- Yeni kaynakların `.meta` dosyaları, bu rapor ve güncel Game View görüntüleri. Commit/deploy/publish yapılmadı.

## Gerçekte yürütülen testler

Mevcut yerel paketleri kullanan izole `Temp/GameplayFoundationValidation` projesinde:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayCameraCorrection.Apply -quit -logFile 'C:/Users/sahba/BlockRift/TestResults/CameraAim/configure.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/CameraAim/editmode.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/CameraAim/editmode.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayGameViewCapture.CaptureBatch -logFile 'C:/Users/sahba/BlockRift/TestResults/CameraAim/preview.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/CameraAim/delivered145.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/CameraAim/delivered145.log'
```

**Sonuç: 145 geçti / 0 başarısız / 0 atlanan**, Unity çıkış kodu 0. Mevcut 137 testin tamamı korunarak sekiz yeni vaka eklendi. İlk çalışma ve teslim edilen dosyalarla tekrar aynı sonucu verdi.

- 5×5/6×6; 1080×1920, 1080×2520, 1536×2048: pozitif Y, aşağı yön, merkez LookAt, 70–80° açı, Y/Z=0, hafif perspektif oranı ve değişmeyen model matrisleri.
- Gerçek 6×6 mesh vertexleriyle 1080×1920 ekran kaplaması %85–90; üst/alt UI boşluğu.
- Kaydırılmış görüntü penceresinde arka planın dört ekran köşesini kaplaması.
- Önceki iki projeksiyon/safe-area/gerçek mesh/pivot/ölçek/grid ve Core testleri de geçti.

5×5 için kamera ve mantıksal sunum zarfı test edildi; projedeki gerçek sanat tahtası 6×6'dır. Bu görevde yeni 5×5 model oluşturulmadı veya 6×6 ölçeklenmedi.

Ana sahne ve test edilen sahne hash'i aynı: `0869DB52FD7BD6F209E95AD330B28B265E54E8C884DDDE1D6B9065BB721971F5`. `source-hashes.json` 29 kaynak/asmdef dosyasının ana ve test kopyasında aynı olduğunu gösterir. Ana editörde de yeni Game View görüntüsü alındı; PNG hash'i `D87E1922BA76400B335FC2619033A10523F6264FAB85752CF09377AFB0F817ED`.

Ölçümler, XML/loglar, 76 dosyalık koruma listesi ve önceki görüntü `TestResults/CameraAim/` altında saklanır. Gerçek cihaz performans testi yürütülmedi. Grafik destekli izole editörün SearchDatabase başlangıcında editöre ait index istisnası görüldü; Game View yakalaması tamamlandı.

```powershell
& 'C:/Users/sahba/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
```

Meta/GUID denetimi: 168 dosya, **4 mevcut hata / 0 uyarı**, çıkış kodu 1. Hatalar yalnız değiştirilmemiş `Assets/Settings/DefaultVolumeProfile.asset` satır 78,119,264,832 içindeki önceki script referanslarıdır. Yeni dosyalarda eksik/çakışan GUID bulgusu yoktur. Genel proje denetiminin tamamen yeşil olduğu iddia edilmez.
