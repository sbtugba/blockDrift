# Block Rift — Board/Piece görsel entegrasyon altyapısı

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Tarih: 27 Ekim 2026. Gerçek model importu ve gameplay mantığı kapsam dışındadır.

## İnceleme ve kararlar

`AGENTS.md`, mimari ve Unity C# skill'leri, GDD v1.2, Core veri modeli/validator, mevcut testler, Gameplay sahnesi ve sanat ölçü raporları incelendi. `Assets/_BlockRift` içinde grid/world dönüşümü arandı: mevcut kod yalnız `GameplayViewportLayout` kamera kadrajını içeriyordu. Bu sistem korundu; yeni kamera veya kural motoru yazılmadı.

İlgili GDD kuralları: §3.1 yalnız 5×5/6×6; §§3.2–3.3 ana parça 1×1, dokuz sabit dikdörtgen yönelimi; §4 dönüşsüz hücre hareketi; §11 sunumdan bağımsız Core. Bu çalışma yalnız koordinat sunar, hareket/çarpışma/tetikleme/kazanma kararı vermez. `PieceDefinition.Start` sol-alt ayakizi başlangıcıdır; Core aşağı yönü y-1 korunur.

## Görsel koordinat ve pivot sözleşmesi

- `BoardRoot`: tahta mantıksal yüzeyinin merkezi. Yerel XZ düzlemi tahta; Unity Y yükseklik.
- Grid X → yerel +X; grid Y → yerel +Z. Dolayısıyla Core aşağı yönü y-1, sunumda -Z olur.
- Hücre aralığı sabit **1.000 Unity birimi**. Kamera ekrana sığdırmak için model ölçeğini değiştirmez.
- Hücre merkezi: `(x - (N-1)/2, surfaceHeight, y - (N-1)/2)`.
- Çok hücreli parçanın taban merkezi: `(origin.X + width/2 - N/2, surfaceHeight, origin.Y + height/2 - N/2)`.
- 5×5 ilk/son hücre merkezleri `(-2,0,-2)` / `(2,0,2)`; 6×6 için `(-2.5,0,-2.5)` / `(2.5,0,2.5)`.
- Örnek 6×6, 2×1 parça, origin `(0,0)`: taban merkezi `(-2,0,-2.5)`. Hücre merkezine doğrudan koymak yerine ayakizi merkezinin kullanılması yarım hücre kaymasını önler.
- Parça wrapper pivotu XZ ayakizi merkezinde ve tabanda Y=0; tahta wrapper pivotu XZ merkezinde/mantıksal yüzeyde Y=0 olmalı. Varsayılan `surfaceHeight=0`, ilave blok yükseltmesi yoktur.
- `BoardRoot` ve bütün üst köklerin ölçeği `(1,1,1)` olmalı. Dünya konumu ve Y etrafında sahne yönelimi desteklenir; tahtayı X/Z etrafında eğmek veya ölçeklemek reddedilir. Bu sahne yönelimi parçaların oyun içinde dönmesi değildir.
- `BoardVisualsRoot` ve `PiecesRoot`, BoardRoot'un ayrı doğrudan çocukları; yerel konum/dönüş sıfır, ölçek bir olmalı.
- Hazır model wrapper'ı da dönüş sıfır/ölçek bir olmalıdır. İlerideki import eksen/pivot düzeltmeleri model çocuğunda açıkça yapılmalı; adaptör mesh'i otomatik ölçeklemez veya yeniden merkezlemez.

Blender'daki +Z yükseklik Unity +Y'ye, hücre sıralaması bu +X/+Z sözleşmesine uymalıdır. FBX eksen/handedness dönüşümünün bunu otomatik yaptığı varsayılmadı; gerçek importta işaretli köşeler ve panel merkezleriyle kontrol edilmelidir.

## Eklenen altyapı ve kullanım

`BoardGridLayout`: yan etkisiz ve MonoBehaviour olmayan koordinat hesabı. Core `Cell` değerini okur, Unity `Vector3` döndürür. Görsel ayakizi sınır denetimi içerir; Core collision, ShapeCatalog veya move transition kodunu çoğaltmaz.

`BoardVisualLayout`: mevcut BoardRoot'a bağlıdır. Mevcut BoardVisualsRoot/PiecesRoot ve kamera adaptörünü açık referanslarla kullanır. Otomatik level yükleme, nesne üretimi, polling veya Core state mutasyonu yapmaz.

```csharp
// Gelecekte kompozisyon katmanı, onaylı level boyutunu ve hazır prefab örneğini sağlar.
layout.SetBoardSize(level.Width); // Yalnız sunum boyutu; level/state değişmez.
Vector3 cellCenter = layout.CellToWorld(cell);
Vector3 pieceBase = layout.PieceToWorld(definition, currentOrigin);
layout.PlaceBoardView(existingBoardWrapper);
layout.PlacePieceView(existingPieceWrapper, definition, currentOrigin);
```

Bu çağrılar hazır Transform üzerinde çalışır; prefab instantiate etmez, animasyon/hamle veya kimlik kayıt sistemi kurmaz. Parçanın Core tanımındaki başlangıcı değiştirilmez. Başarısız yerleşim girdileri hiyerarşi/Transform değiştirilmeden reddedilir. Aynı hazır görsel tekrar yerleştirilebilir.

Mevcut `GameplayViewportLayout`, isteğe bağlı `BoardVisualLayout` referansından sunum sınırlarını okur. Önceki kamera yolu ve fallback sınırları korunur. Tahta boyutuyla zarf X/Z ölçüsü 5.5 veya 6.5 olur; varsayılan yükseklik aralığı -0.2…0.7 ve kadraj payı 1.08 korunur. Bu değerler görsel kadraj rezervidir, yeni bir oyun mekaniği değildir. Geçersiz Inspector boyutunda kontrol hata bildirir, kamera fallback ile yenilenebilir.

## Mevcut sahne ve kontroller

Hiyerarşi değiştirilmedi; yeni model/nesne eklenmedi. `BoardRoot` üzerine `BoardVisualLayout`, kamera bileşenine aynı adaptörün referansı eklendi. Sahne boyutu varsayılan 6; 5×5 aynı adaptörle seçilebilir. BoardRoot/PiecesRoot ve diğer model kökleri boş kaldı. Işık, UI, kamera açısı, materyaller ve proje ekran ayarları bu görevde değiştirilmedi.

Unity'de Play kapalıyken:

1. `Assets/Scenes/BlockRift_Gameplay` sahnesini açın.
2. Hierarchy → `GameplayRoot/BoardRoot` seçin. Inspector kök/referans/ölçek hatalarını gösterir.
3. Scene sekmesinde Gizmos'u açın. Grid çizgileri ve X/Y/Z eksenleri editör kılavuzudur; mesh değildir, Game görünümüne çizilmez.
4. İleride model geldikten sonra aynı Inspector'daki **Import sonrası model/pivot kontrolü** alanında model wrapper'ını seçin; tahta/parça türünü ve parça boyutunu girerek doğrulayın. Bu alan bir prefab katalog bağlantısı değildir, editör denetim aracıdır.
5. Alternatif olarak `Block Rift > Visuals > Inspect Selected Model Bounds`, seçili wrapper altındaki statik MeshFilter sınırlarını wrapper yerel uzayında raporlar.

`VisualModelChecks` parçada taban-merkez pivotunu, X/Z ayakizini ve 0…0.7 yükseklik zarfını; tahtada merkez pivotunu, mantıksal yüzeyi ve mevcut çerçeve/yükseklik zarfını sınar. Ölçüler için tolerans 0.001 birimdir. Dönmüş model çocuklarının mesh bounds köşeleri wrapper uzayına dönüştürülür. Skinned mesh/deforme geometri kapsam dışıdır. Bounds kontrolü gerçek panel merkezleri, mesh hücre fit'i veya tüm sanat kabul ölçütlerinin kanıtı değildir.

**Gerçek model/pivot/fit doğrulaması: INCOMPLETE.** Bu görevde hiçbir model import edilmedi ve boş kökler başarılı model kontrolü olarak sunulmadı.

## Değişen dosyalar

Yeni:

- `Assets/_BlockRift/Presentation/Board/BoardGridLayout.cs`
- `Assets/_BlockRift/Presentation/Board/BoardVisualLayout.cs`
- `Assets/_BlockRift/Presentation/Board/VisualModelChecks.cs`
- `Assets/_BlockRift/Editor/BoardVisualLayoutEditor.cs`
- `Assets/_BlockRift/Editor/GameplayVisualIntegrationSetup.cs`
- `Assets/_BlockRift/Tests/Presentation/BoardVisualLayoutTests.cs`
- Yeni kaynak/Board klasörü `.meta` dosyaları ve bu rapor.

Güncellenen:

- `Assets/Scenes/BlockRift_Gameplay.unity`: bir BoardVisualLayout bileşeni ve kamera referansı.
- `Presentation/Camera/GameplayViewportLayout.cs`: mevcut kadrajda adaptör sınırları ve bağlantı kontrolü.
- `Presentation/BlockRift.Presentation.asmdef`: yalnız aşağı yönlü Core referansı.
- `Tests/Presentation/BlockRift.Presentation.EditModeTests.asmdef`: Core test referansı.
- `Editor/GameplaySceneSetup.cs`: ileride boş sahne üretildiğinde de aynı bağlantı yordamını kullanır; mevcut sahneyi yeniden oluşturmaz.

Core, mevcut test kaynakları, GDD, paket manifesti, var olan FBX/material/Blender dosyaları değiştirilmedi. Önceki görevden gelen dosya/değişiklikler korundu. Save/level veri şeması değişmedi. Commit/deploy/publish yoktur.

## Gerçekte yürütülen doğrulama

Açık kullanıcı Unity oturumuna müdahale edilmeden, mevcut yerel paketleri kullanan `Temp/GameplayFoundationValidation` izole projesi kullanıldı. Ana manifest değişmedi. Yollar aşağıdaki komutlarda çalıştırıldıkları mutlak biçimleriyle gösterilir.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayVisualIntegrationSetup.ConfigureExistingScene -quit -logFile 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/configure.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests.BoardValidatorTests' -testResults 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/core.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/core.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -assemblyNames 'BlockRift.Presentation.EditModeTests' -testResults 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/presentation.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/presentation.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/final-editmode.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/VisualIntegration/final-editmode.log'
```

Sonuçlar:

- Mevcut sahnenin Unity ile bağlanması ve C# derlemesi **PASS**, çıkış kodu 0.
- Önce Core: **69/69 PASS**. Sonra ilk sunum çalışması: **30/30 PASS**.
- Geçersiz Inspector boyutu için fallback testi eklendikten sonra nihai çalışma: **100 geçti / 0 başarısız / 0 atlanan**, çıkış kodu 0. Dağılım: 69 Core + 13 mevcut sahne/kadraj + 18 yeni adaptör testi.
- 5×5/6×6 tüm hücre merkezleri, tüm dokuz şeklin her olası görsel yerleşimi, taban merkezi, hücre adımı, sınır/int taşması, taşınmış/Y yönelimi değişmiş BoardRoot, üst kök ölçeği, kaymış PiecesRoot, tekrar yerleştirme ve hatada değişmeme test edildi. Bunlar görsel testlerdir; yasal gameplay hamleleri veya çözülebilirlik kanıtı değildir.
- Yeni kamera entegrasyonu her iki boyutta 1080×1920, 1080×2520, 1536×2048 ve asimetrik safe area ile tüm sunum zarfı köşelerinde test edildi. Mevcut altı çözünürlüklü kadraj testleri de geçti.
- Pivot/ölçek/eksen/yükseklik bounds kontrolleri sayısal Bounds girdileriyle sınandı; test için taş mesh'i oluşturulmadı. Yerleşim testleri yalnız test sırasında oluşturulan, kaydedilmeyen boş Transform'ları kullanır.
- Test edilen ve ana projeye aktarılan sahne SHA-256 aynıdır: `BD7CD22A497AC9A0EFB2771566A84058A9EE091046C0F9C271EB5C7BE0DC6FB3`. Sahne aktarımından önce başlangıç hash'i kontrol edilerek eşzamanlı kullanıcı değişikliğinin üzerine yazılması önlendi. Mevcut `.meta` GUID'si korundu.
- `TestResults/VisualIntegration/source-hashes.json`: ana/izole projede 20 kaynak/asmdef dosyası birebir aynı. Sonuç XML/logları aynı klasördedir.

```powershell
& 'C:/Users/sahba/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
git diff --check -- Assets/_BlockRift ProjectSettings/ProjectSettings.asset
```

Meta/GUID denetimi: **4 mevcut hata / 0 uyarı**; yalnız değiştirilmeyen `Assets/Settings/DefaultVolumeProfile.asset` satır 78,119,264,832 (önceki raporlarla aynı). Yeni dosyalarda eksik/çakışan GUID hatası yoktur. Genel proje denetimi tamamen yeşil değildir. `git diff --check` geçti. Bu görevde interaktif Play Mode/Device Simulator görsel QA veya cihaz performans testi yürütülmedi.

## Gerçek modellerin entegrasyonu için kalanlar

1. Onaylı Blender dosyalarını ileride export/import etmek; 1 birim pitch'i, FBX birim dönüşümünü, yönleri, normalleri ve URP materyallerini doğrulamak. Bu görev mevcut FBX'leri de sahneye bağlamadı.
2. 5×5 tahta assetini hazırlamak/teslim almak; 6×6'yı ölçekleyerek 5×5'e dönüştürmemek. Sanat raporunda ayrı 5×5 tahta henüz üretilmemişti.
3. Birim ölçekli, nötr dönüşlü Board/Piece prefab wrapper'ları; dokuz onaylı yönelimin doğru model eşlemelerini oluşturmak. Mevcut kökleri kullanmak, modelleri ayakizine göre sessizce ölçeklememek.
4. Gerçek bounds/pivot kontrolünü çalıştırmak; hücre/panel merkezlerini, çok hücreli hizalamayı, çerçeve açıklığını ve yüzey toleransını gerçek mesh'lerle yeniden doğrulamak. Bounds testi tek başına yeterli değildir.
5. İleride Application/level kompozisyonundan boyut, PieceId ve güncel Core konumlarını sağlayarak hazır görselleri adaptöre bağlamak. Otomatik spawn/katalog, durum bildirimleri, input ve animasyon ayrı aşamadır; burada yeniden oluşturulmadı.
6. Gerçek modellerle 5×5/6×6, çentikli cihaz safe area, farklı ekran oranları ve mobil URP performansını görsel olarak denetlemek. Mevcut -0.2…0.7 kadraj yükseklik zarfını aşan bir sanat asseti gelirse oyun mantığını/model ölçeğini değiştirerek gizlemek yerine kamera rezervini açıkça yeniden değerlendirmek.
