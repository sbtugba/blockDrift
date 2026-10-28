# Block Rift — Blender → Unity gerçek asset entegrasyonu

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Tarih: 27 Ekim 2026. Sahne: `Assets/Scenes/BlockRift_Gameplay.unity`.

## Sonuç ve açık kalite bulgusu

Sekiz ayrı FBX, sekiz URP Lit materyali, sekiz wrapper prefab ve bir görsel katalog oluşturuldu. Mevcut `BoardVisualLayout`, `BoardGridLayout`, `VisualModelChecks` ve `GameplayViewportLayout` kullanılarak yalnız bir 6×6 tahta ve yedi gerçek görsel yerleştirildi. Core, hamle/yer çekimi, solver veya otomatik bölüm üretimi yazılmadı/değiştirilmedi.

İlgili GDD kararları: §§3.1–3.3 (5×5/6×6, 1×1 hedef ve sabit şekil yönelimleri), §11 (sunumdan bağımsız Core). Geometrik hücre aralığı 1 Unity birimi olarak korunur; hiçbir oyun kuralı sanat assetlerine uydurulmadı.

**Geometri ve yerleşim: PASS. Kaynak custom normal yönleri: REVIEW REQUIRED.** Son teknik Blender denetiminin bulgusu yeniden doğrulandı: tahta tabanı/hücreleri ve altı parçada alt bevel bölgesindeki bazı custom shading normalleri geometrik yüz normalinin ters yarımküresine bakıyor. Toplam 5.706 köşe normali bu denetimde işaretleniyor. Taş ve tahta çerçevesinde bu bulgu yok. Geometri, UV veya normallere düzeltme uygulanmadı; Unity `Import Normals` ile mevcut normaller korundu. Bu entegrasyon tam shading/normal kalite onayı değildir. Sessiz alternatif model veya normal yeniden hesaplama yapılmadı.

## Kaynakların tespiti ve Blender doğrulaması

Canlı kaynak `ArtProduction/AssetPreview/AssetPreview.blend` içindeki onaylı meshlerdir. Ayrı üretim klasörleri ve raporları incelendi; eski Unity önizleme FBX'leri kaynak kabul edilmedi.

| Unity FBX/prefab kimliği | Blender kaynak nesnesi | Ayakizi / yükseklik |
|---|---|---|
| Block_1x1 | Block_1x1_Export | 1×1 / 0.30 |
| Block_2x1 | Block_2x1_Export | 2×1 / 0.30 |
| Block_3x1 | Block_3x1 | 3×1 / 0.30 |
| Block_4x1 | Block_4x1_Export | 4×1 / 0.30 |
| Block_2x2 | Block_2x2_Export | 2×2 / 0.30 |
| Special_1x1_Star | Special_1x1_Star_Export | 1×1 / 0.30 |
| Obstacle_1x1_Stone | Obstacle_1x1_Stone_Export | 1×1 / 0.30 |
| Board_6x6 | Board_6x6_Base + Frame + Cells | 6.24×6.24 / -0.180…-0.001 |

Blender MCP üzerinden mevcut `ArtProduction/Board_6x6/TechnicalAudit/audit_current_scene.py` yeniden yürütüldü:

- `PASS_GEOMETRIC_FIT`; 36 hücre, pitch 1.000, görsel hücre 0.970, hücre boşluğu 0.030.
- 6×6 için 205 gerçek mesh yerleşimi (124 dış kenar konumu); tahta parçalarıyla BVH yüzey kesişimi 0.
- 5×5 için aynı ölçekle 136 sayısal yerleşim kontrolü geçti. Ayrı bir 5×5 tahta asseti mevcut değildir ve üretilmedi.
- 10 mesh kapalı manifold; dejenere UV üçgeni 0. Minimum blok–hücre düşey açıklığı yaklaşık 0.001; çerçeveyle blok hareket zarfı açıklığı 0.006. Bunlar kaynak ölçüleridir, Unity gameplay fiziği değildir.
- Ölçek/pivot, UV, materyal bölgeleri, vertex/topoloji/custom normal verileri ve `.blend` dosya hash'i işlem öncesi/sonrası aynı.

Export aracı: `ArtProduction/UnityIntegration/export_approved.py`. Her kaynak geçici sahnede aynı mesh verisini paylaşan bir kopyayla yerel origin'e alınır; sergileme offsetleri export edilmez. Kaynak nesneler taşınmaz, transform uygulanmaz, `.blend` kaydedilmez. FBX eksen/birim dönüşümü yalnız export/import yolunda yapılır. Manifest: `ArtProduction/UnityIntegration/export_manifest.json` (makineye özel üretim kanıtı; Git ignore kapsamındadır).

## Unity materyal ve prefab bağlantıları

- FBX: `Assets/_BlockRift/Art/Models/Gameplay/`.
- Wrapper prefab: `Assets/_BlockRift/Art/Prefabs/Gameplay/`.
- URP Lit materyal: `Assets/_BlockRift/Art/Materials/Gameplay/`.
- Katalog: `Assets/_BlockRift/Content/GameplayVisualCatalog.asset`.

Wrapper'lar yerel origin, birim ölçek ve nötr dönüş kullanır. İçteki gerçek FBX nested prefab'dır; primitive/placeholder mesh oluşturulmadı. FBX importer globalScale=1, file units açık, axis conversion bake açık, mesh compression kapalı, collider/animation/camera/light import kapalı. Custom normal import edilir; tangentler MikkTSpace ile hesaplanır. Mesh read/write, bu aşamadaki gerçek vertex denetimleri için açıktır; mobil bellek optimizasyonu ayrıca değerlendirilebilir.

Blender Principled lineer Base Color değerleri Unity materyal renk gösterimine dönüştürüldü; Metallic korundu, Smoothness=1−Roughness eşlendi. Nötr blokların rengi kaynak Blender materyalindeki gridir; yeni renk tasarımı uygulanmadı. Yıldızın ivory/gold materyal bölgeleri ve tahtanın cream/beige bölgeleri korunur.

Taşın mevcut onaylı 512×512 PNG haritaları kullanıldı. İlk importta bu dosyalar Cubemap olarak algılandığından test normal-map bağlantısını reddetti. Ayarlar açıkça Texture2D/Default (surface), Texture2D/NormalMap (normal), sRGB kapalı olarak düzeltildi; PNG piksel verisi değiştirilmedi. Normal green flip kapalı, normal scale 0.85, smoothness 0.13. Sessiz null texture bağlanması artık hata üretir.

`VisualPrefabCatalog` yalnız kimlik→gerçek prefab eşler. Eksik kimlikte alternatif üretmez; 5×5 tahta sorgusu false döner. 5×5 Core/koordinat/kamera desteği korunmuştur. Dokuz gameplay şeklinin bütün sanat assetleri bu görevde üretilmedi; yalnız istenen yedi görsel eşlendi.

## Mevcut sahnedeki tek test dizilimi

`GameplayRoot/BoardRoot/BoardVisualsRoot/Board_6x6`: bir gerçek tahta, üç kaynak mesh.

`GameplayRoot/BoardRoot/PiecesRoot`: yedi gerçek prefab. Blender'da denetlenmiş fit dizilimi kullanıldı:

| Görsel | Sol-alt hücre |
|---|---|
| 4×1 | (0,0) |
| Taş | (4,0) |
| Yıldız | (5,0) |
| 3×1 | (0,2) |
| 2×1 | (3,2) |
| 1×1 | (5,2) |
| 2×2 | (0,4) |

Toplam 16 hücre işgal edilir; çakışma yoktur. Parça tabanları Y=0, gerçek tahta görsel tepesi Y=-0.001. Bu, sanat entegrasyonu için statik bir test yerleşimidir; oynanabilir/çözülebilir bir bölüm veya taşın authoritative gameplay türü için kayıt oluşturmaz. Core PieceDefinition yalnız mevcut adaptöre ölçü/konum sağlamak için okunur.

Kamera sistemi yeniden yazılmadı. Gerçek renderer sınırları safe-area içindeki BoardViewport'a dört ekran boyutunda test edildi. Ana editörde Game View 1080×1920/Play Mode açıldı ve Unity `ScreenCapture` ile `docs/Gameplay_6x6_GameView.png` alındı; bu Blender renderı veya mockup değildir. Kullanıcının mevcut `Background`/`BackgroundSpriteFitter` ve arka plan materyali korundu; görüntüdeki arka plan bu görevde üretilmedi. Arka plan değişikliği son test kopyasına da dahil edildi.

## Testler ve gerçekleşen komutlar

Açık ana Unity oturumu kapatılmadı. İzole `Temp/GameplayFoundationValidation` projesinde mevcut yerel paketler kullanıldı; ana paket manifesti değişmedi.

```powershell
# Entegrasyon öncesi mevcut 100 test (değişmeden): 100/100 PASS
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/baseline100.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/baseline100.log'

# Gerçek FBX/material/prefab/sahne bağlantısı
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayArtIntegration.Build -quit -logFile 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/build.log'

# Texture2D import düzeltmesi
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayArtIntegration.FinishMaterials -quit -logFile 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/materials-fixed.log'

# Teslim edilen dosyalar ve son capture aracıyla test
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/final-delivered115.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/final-delivered115.log'

# Mevcut arka plan dahil güncel ana sahneyle son tekrar
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/current-scene115.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/ArtIntegration/current-scene115.log'
```

Entegrasyon öncesi **100/100 PASS**. Entegre kaynaklarla **115/115 PASS** (`final115.xml`, `delivered115.xml`, `final-delivered115.xml`); ilk entegrasyon denemesinde taş normal-map bağlantısı nedeniyle 114/115 görülmüş, yukarıdaki Texture2D düzeltmesiyle kapanmıştır. Güncel arka plan dahil son tekrar da **115 geçti / 0 başarısız / 0 atlanan**, Unity çıkış kodu 0 (`current-scene115.xml`). Dağılım: 69 Core + 13 mevcut sahne/kadraj + 18 görsel adaptör + 15 gerçek asset entegrasyon testi.

Son ana/izole sahne SHA-256 eşittir: `E63B8B0AD162A7E2CA068429D7B3AF1B842292E54E33F79D58B434749BA3C01B`. `source-hashes.json` ana ve test kopyasında 25 kaynak/asmdef dosyasının aynı olduğunu doğrular. Gerçek ana Game View PNG SHA-256: `1DD35DF084401D7131FD99C8C5011CC52A596684D7CFB2F37831E364F2AF3477`; metadata 1080×1920 ve Play Mode kaynağını bildirir.

Meta/GUID denetimi de yürütüldü:

```powershell
& 'C:/Users/sahba/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
git diff --name-only -- Assets/_BlockRift/Core Assets/_BlockRift/Tests/EditMode docs/GAME_DESIGN.md Packages/manifest.json
git diff --check -- Assets/_BlockRift/Core Assets/_BlockRift/Tests/EditMode
```

156 dosyada **4 mevcut hata / 0 uyarı**: yalnız değiştirilmeyen `Assets/Settings/DefaultVolumeProfile.asset` satır 78,119,264,832 içindeki önceki script referansları. Yeni asset/script/prefab referanslarında hata yoktur; genel projenin tamamen yeşil olduğu iddia edilmez. Uzun süren bir tekrar aynı script'i `runpy.run_path` ile çalıştırarak da bu sonucu verdi. Core/test/GDD/manifest diff kontrolü boş; belirtilen diff whitespace kontrolü geçti.

Yeni 15 test: sekiz gerçek prefab'ın mesh/pivot/ölçü/normal/tangent/UV/URP materyal kontrolleri; katalog ve eksik 5×5 asset davranışı; hücre işgali/pozitif hacim çakışmaması; taş texture ayarları; gerçek renderer köşelerinin 1080×1920, 1080×2400, 1080×2520, 1536×2048 safe-area kadrajı. Mesh AABB'lerinin en az bir yatay eksende ayrılması, bu test diziliminde hacimsel parça çakışmasını dışlar; komşu yüzey teması toleransı 0.00001'dir.

Önceki boş-sahne testinin boş kök beklentisi, gerçek entegrasyon gereği bir tahta/yedi parça ve BoardRoot altında tam 10 gerçek mesh beklentisiyle güncellendi; collider/Rigidbody yasağı ve UI/kamera kontrolleri korundu. Core testleri değiştirilmedi.

Ana proje GUID'leri korundu; izole çıktılardaki 22 farklı GUID, prefab/catalog/scene/importer referanslarında ana GUID'lere eşlendi. Sonra teslim edilen dosyalar izole projede tekrar test edildi. Kaynak veri kopyalanırken mevcut sahne hash'i kontrol edildi; daha sonra eklenmiş arka planın üzerine yazılmadı. `guid-remapping.json`, XML/loglar ve Game View metadata'sı `TestResults/ArtIntegration/` altındadır.

Windows pencere yakalama iki denemede zaman aşımına uğradı. Unity'nin kendi Game View/Play Mode ve ScreenCapture API'siyle gerçek görüntü alındı. Açık editörde yeni prefab'lar AssetDatabase'e alınmadan ilk görüntü boş kalmıştı; capture aracı artık senkron importu, sekiz katalog prefab'ını ve BoardRoot altındaki 10 gerçek mesh'i kontrol eder. Yeni görüntü oluşmadan önceki PNG'yi başarılı saymaz. Arka planda kare üretimi yalnız capture süresince açılır, önceki ayara geri döner.

## Dosyalar ve kalan sınırlar

Yeni kaynaklar: `VisualPrefabCatalog.cs`, `GameplayArtIntegration.cs`, `GameplayGameViewCapture.cs`, `GameplayArtIntegrationTests.cs`; Blender export/delivery scriptleri ve bu rapor. Değişenler: `BoardVisualLayout` katalog referansı, Editor/test asmdef bağımlılıkları, gerçek modele uyarlanan mevcut sahne testi, Gameplay sahnesindeki görsel prefab bağlantıları ve iki taş texture importer meta'sı. FBX/prefab/material/catalog dosyaları ve `.meta` dosyaları yukarıdaki klasörlerdedir.

Core/GDD, hareket veya solver kodu değiştirilmedi. Kaynak Blender geometrisi/topolojisi/UV/custom normals korundu. Mevcut arka plan çalışması ve diğer kullanıcı değişiklikleri geri alınmadı. Commit/deploy/publish yapılmadı.

Kalan kalite işleri: kaynak alt bevel normal yönleri için açık karar/düzeltme ve yeniden shading denetimi; ayrı 5×5 tahta asseti; istenmeyen ek şekiller için onaylı sanat eşlemeleri; mobil cihaz performans ölçümü. Bu görevde normal sorunu çözülmüş, bütün bölümler çözülebilir veya cihaz performansı doğrulanmış gibi raporlanmaz.
