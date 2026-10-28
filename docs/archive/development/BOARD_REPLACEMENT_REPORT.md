# Block Rift — yeni board ve perspektif kamera

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Tarih: 27 Ekim 2026. Kapsam: mevcut Gameplay sahnesindeki tahta görselinin değiştirilmesi ve kamera/arka plan kadrajı.

## Sonuç

`BlockRift_Gameplay` sahnesinde ve `GameplayVisualCatalog` içindeki mevcut 6×6 eşlemesinde artık **Board_6x6_Frameless** prefab'ı bağlıdır. Önceki çerçeveli FBX/prefab arşiv/reference olarak korunur; aktif sahnede yalnız bir board bulunur. Mevcut yedi parça prefab'ı ve konum/dönüş/ölçekleri korunmuştur.

`BoardRoot`, `BoardVisualsRoot`, `PiecesRoot` ve UI hiyerarşisi korunur. BoardRoot/PiecesRoot nötr dönüş ve birim ölçektedir. 1 hücre = 1 Unity birimi, Core'un sol-alt başlangıçlı koordinatları ve 5×5/6×6 desteği değişmedi. Core, gameplay, solver, level generator veya GDD düzenlenmedi.

GDD §§3.1–3.3 ve §11 esas alınır. Mimari skill'indeki önceki orthographic kamera tercihi, kullanıcının bu görevde açıkça istediği perspective kamera için sahne düzeyinde geçersiz kılındı; bu bir oyun kuralı değişikliği değildir. Mevcut kamera bileşenine iki projeksiyon desteği eklendi, yeni paralel kamera sistemi kurulmadı.

## Asset ve ölçüler

Blender'da yeni, tamamlanmış `Board_6x6_Frameless_Root`, `Board_6x6_Frameless_Cells`, `Board_6x6_Frameless_Support` nesneleri bulundu. Kullanıcının yeni asseti bağlama talebiyle bu varyant kullanıldı; yeni geometri üretilmedi. Önizleme root offseti (8,10,0) export edilmedi.

| Özellik | Yeni tahta |
|---|---|
| Kaynak | `ArtProduction/AssetPreview/AssetPreview.blend`, Frameless koleksiyonu |
| FBX | `Assets/_BlockRift/Art/Models/Gameplay/Board_6x6_Frameless.fbx` |
| Prefab | `Assets/_BlockRift/Art/Prefabs/Gameplay/Board_6x6_Frameless.prefab` |
| Yerel dış ölçü X/Z | 5.980 × 5.980 |
| Toplam Y aralığı | -0.067 … -0.001 |
| İnce destek Y aralığı | -0.067 … -0.061 |
| Hücre merkezleri | -2.5, -1.5, -0.5, 0.5, 1.5, 2.5 |
| Hücre görsel genişliği | 0.970 |
| Grid pitch | 1.000 |
| Mantıksal yüzey / parça tabanı | 0 / 0 |
| Görsel hücre tepesi / taban açıklığı | -0.001 / 0.001 |
| Mesh / üçgen sayısı | 2 / 16.672 |

Dış görsel ölçünün 6 yerine 5.98 olması, hücre aralığının küçültülmesi değildir. Çerçevesiz 0.97 genişlikli hücreler en dışta 0.015'er birim görsel pay bırakır. Mevcut board bounds denetiminin minimum span kontrolü bu onaylı görsel yapıya uyarlandı; grid hesapları değiştirilmedi, model ölçeklenmedi.

Blender `verify_frameless.py` yeniden çalıştırıldı: **7 gerçek fit, 205 konum, 124 dış kenar, 0 kesişim, PASS**. Hücre mesh'i önceki doğrulanmış hücrelerle aynı datablock'tur. `export_frameless.py` yalnız bu iki mesh'i geçici kopyalarla export etti; önceki exporter'ın parmak izi yordamını kullandı. Kaynak `.blend` hash'i ve bütün mevcut mesh verilerinin parmak izleri aynı kaldı. Rapor: `ArtProduction/UnityIntegration/frameless_export.json`.

FBX birim ölçekle ve bir kez eksen dönüşümüyle import edildi. Mevcut `MAT_Board_Cream` ve `MAT_Board_Beige` URP materyalleri yeniden kullanıldı. Custom normal importu korunur. Kaynak hücreler ve bazı bloklar için önceden raporlanan alt bevel custom normal yönü bulguları bu çalışmada düzeltilmedi; yeni bir shading kalite onayı iddia edilmez.

## Kamera ve yerleşim

- Camera projection: **Perspective**.
- Rotation: **X 50°, Y 0°, Z 0°**.
- Vertical FOV: **32°**, bileşendeki ayarlanabilir aralık 30–35°.
- Near/far: **0.1 / 100**.
- 1080×1920, safe area tam ekran başlangıcında konum yaklaşık **(0, 19.17121, -16.82414)**. Bu konum ekran/safe area değişince yeniden hesaplanır.
- BoardViewport safe-area anchor'ları: **(0.04,0.30)–(0.96,0.80)**. Merkezi 0.55 ile ekran merkezinin üstündedir. TopHUD 0.82–0.98 ve BottomControls 0.03–0.20 alanlarıyla örtüşmez.
- Kadraj payı **1.04**. Mevcut 5×5/6×6 sunum zarfının sekiz köşesi gerçek perspektif derinlikleriyle fit edilir; ön/arka kenarların farklı büyümesi hesaba katılır. BoardRoot veya parçalar ekrana sığdırılmak için ölçeklenmez.
- `GameplayViewportLayout` eski orthographic yolunu da destekler. İki projeksiyon test edildi; aktif sahne perspective kullanır.
- `BackgroundSpriteFitter` perspektifte kendi uzaklığındaki frustum kesitini kaplar. Küçük %0.2 overscan, filtreleme/matris hassasiyetinin kenarda boş çizgi bırakmasını önler. Mevcut kum arka planı ve materyali korundu.

Gerçek ana Unity Game View/Play Mode görüntüsü **1080×1920** olarak alındı ve görsel olarak incelendi: `docs/Gameplay_6x6_Perspective.png`. Mevcut `docs/Gameplay_6x6_GameView.png` de güncel sahneyi gösterir. Görüntü Blender renderı veya maket değildir. Parçalar yeniden renklendirilmedi; referans yalnız istenen kamera/tahta düzenlemesi için kullanıldı. UI butonları veya yeni gameplay görselleri üretilmedi.

## Doğrulama

İzole `Temp/GameplayFoundationValidation` projesinde mevcut yerel paketlerle yürütülen gerçek komutlar:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayBoardReplacement.Apply -quit -logFile 'C:/Users/sahba/BlockRift/TestResults/BoardPerspective/replace.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/BoardPerspective/final137.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/BoardPerspective/final137.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplayGameViewCapture.CaptureBatch -logFile 'C:/Users/sahba/BlockRift/TestResults/BoardPerspective/preview.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/BoardPerspective/delivered137.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/BoardPerspective/delivered137.log'
```

- Board bağlantısı/derleme: **PASS**, çıkış kodu 0.
- Nihai çalışma ve ana projeye aktarılan dosyalarla tekrar: **137 geçti / 0 başarısız / 0 atlanan**, çıkış kodu 0. Mevcut 115 teste 22 yeni test vakası eklendi.
- İlk yeni test çalışmasında 3 perspektif arka plan köşe kontrolü sayısal olarak 0.50001 sınırını 0.000004–0.000009 aşıyordu; toleransı gevşetmek yerine gerçek arka plan kapsamasına küçük pay eklendi. Son testlerde bu kontroller geçti.
- Mevcut yedi parça prefab'ının dosya hash'leri değişmedi. Scene bakım yordamı parça konumlarını işlem öncesi/sonrası karşılaştırdı. Her parçanın aynı hücre/ayakizi/taban pivotunda, birim ölçek ve nötr dönüşte kaldığı ayrıca test edildi.
- Yeni hücre mesh'inin bütün import edilmiş vertex konumları önceki doğrulanmış hücrelerle aynı yerel grid koordinatlarında karşılaştırıldı; **PASS**. 0.00001 ölçüm hassasiyetinde vertex kümeleri eşittir.
- Yeni prefab'ın iki gerçek mesh'i, 16.672 üçgeni, 5.98 dış ölçüsü, -0.067 alt/−0.001 üst sınırı ve katalog/sahne bağlantısı doğrulandı.
- Perspective/orthographic, 5×5/6×6, 30°/35° FOV, 1080×1920, 1080×2520, 1536×2048 ve 1920×1080 senaryolarında safe-area kadrajı, near/far, 1 birim grid ve arka plan kaplaması test edildi. Önceki gerçek renderer kadraj testleri de geçti.
- Ana projeye aktarımda mevcut GUID'ler korundu. Scene/catalog başlangıç dosyaları eşzamanlı kullanıcı değişikliği için kontrol edildi. Son test edilen ve ana sahne SHA-256 aynıdır: `E3679880D0A551FDBECE77F36ADE1E4A5E77DEE418249ED987DC9A75264D0338`.
- `TestResults/BoardPerspective/source-hashes.json`: 27 kaynak/asmdef dosyası ana ve izole test projesinde birebir aynı. XML/loglar, önceki görsel ve parça hash'leri aynı klasördedir.

Core, eski Core testleri, GDD ve ana paket manifestinde diff olmadığı kontrol edildi. Unity kaynak/meta denetimi ayrıca yürütüldü:

```powershell
& 'C:/Users/sahba/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
```

164 dosyada **4 mevcut hata / 0 uyarı**, denetim çıkış kodu 1: yalnız değiştirilmeyen `Assets/Settings/DefaultVolumeProfile.asset` satır 78,119,264,832 içindeki eski script referanslarıdır. Yeni dosyalarda eksik/çakışan GUID bulgusu yoktur; genel proje denetiminin tamamen yeşil olduğu iddia edilmez.

Gerçek cihaz performans testi yapılmadı. Grafik destekli izole editörün SearchDatabase başlangıcında editöre ait bir index istisnası görüldü; Game View yakalama tamamlandı. Ana editörde yeni görüntü ayrıca alınıp doğrulandı.

## Değişen dosyalar

- Yeni FBX ve prefab: `Board_6x6_Frameless` ve `.meta` dosyaları.
- `Assets/Scenes/BlockRift_Gameplay.unity`: board görseli, kamera 50°/perspective/FOV 32 ve BoardViewport anchor'ları.
- `Assets/_BlockRift/Content/GameplayVisualCatalog.asset`: yalnız 6×6 board prefab referansı.
- `GameplayViewportLayout.cs`: mevcut bileşende perspective fit ve korunmuş orthographic yol.
- `BackgroundSpriteFitter.cs`: iki projeksiyonda kapsama.
- `VisualModelChecks.cs`: çerçevesiz .97 hücre sınırı desteği; grid değişmedi.
- `GameplayGameViewCapture.cs`: sabit mesh sayısı yerine mevcut katalogdaki gerçek mesh sayısını doğrulama.
- `GameplayBoardReplacement.cs`: izole doğrulama için açık, tek amaçlı sahne bakım yordamı.
- `GameplaySceneTests.cs`, `GameplayArtIntegrationTests.cs`: yeni tahta/projeksiyonun gerçek beklentileri; collider/Rigidbody/Core kontrolleri korunur.
- `BoardPerspectiveTests.cs`: 22 yeni test vakası.
- `ArtProduction/UnityIntegration/export_frameless.py`, `deliver_board_replacement.py`, bu rapor ve güncel Game View görüntüleri.

Önceki tahta/oyun taşı geometrileri, materyalleri ve Core değiştirilmedi. Commit/deploy/publish yapılmadı.
