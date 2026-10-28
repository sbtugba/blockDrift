# Block Rift — ilk 3D Gameplay sahne iskeleti

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Tarih: 27 Ekim 2026. Kapsam: kamera, ışık/arka plan, boş model kökleri ve UI yerleşim alanları.

## İnceleme ve kapsam

- `AGENTS.md`, mimari/kamera/UI/C# skill'leri, `docs/GAME_DESIGN.md` v1.2, `STAGE1_REPORT.md`, `docs/CONSISTENCY_AUDIT.md`, bütün Core kaynakları ve mevcut EditMode testleri incelendi.
- Kullanıcının “GDD 3.1” ifadesi §3.1 oyun alanı kararları olarak ele alındı. Depodaki GDD sürümü 1.2'dir: yalnız 5×5/6×6, iç hedef hücresi ve 0–2 tetikleyici. Görsel yerleşim GDD kapsamı dışındadır.
- Mevcut uygulama `Cell`, `PieceDefinition`, `ShapeCatalog`, `LevelDefinition`, `BoardState`, `BoardValidator` ve 69 Core testinden oluşuyor. Application, hamle geçişi, solver veya gameplay sunum katmanı bulunmuyordu; bu görev bunları oluşturmaz.
- Depoda `SampleScene` ve `AssetPreview` vardı. `BlockRift_Gameplay` kayıtlı sahnesi bulunmadığından istenen adla yeni temel sahne oluşturuldu. Mevcut sahneler değiştirilmedi.
- Mevcut Unity 6000.3.23f1 / URP 17.3.0 ve Mobile/PC pipeline assetleri kullanıldı; yeni paket eklenmedi. Blender kaynaklarına, var olan FBX'lere ve ArtProduction değişikliklerine dokunulmadı.

## Sahne hiyerarşisi

```text
GameplayRoot
├── CameraRig                    [GameplayViewportLayout]
│   └── Main Camera              [Camera, AudioListener, URP camera data]
├── EnvironmentRoot
│   └── Key Light                [Directional Light, URP light data]
├── BoardRoot                    [origin, scale 1; XZ düzlemi, Y yükseklik]
│   ├── BoardVisualsRoot
│   ├── PiecesRoot
│   ├── FixedGeometryRoot
│   ├── MarkersRoot
│   └── EffectsRoot
└── UIRoot
    └── GameplayCanvas           [Screen Space Overlay, CanvasScaler]
        └── SafeArea
            ├── TopHUD
            ├── BoardViewport
            ├── BottomControls
            └── OverlayRoot
```

Model kökleri ve UI alanları boştur. Mesh, collider, Rigidbody, level loader, input, buton veya gameplay controller eklenmedi. Bu nedenle Game görünümünde yalnız arka plan görünür. EventSystem/GraphicRaycaster, etkileşimli UI oluşturulacağı aşamaya bırakıldı; şu anda input alan bir öğe yoktur.

## Kamera ve yerleşim

- Sabit orthographic kamera, Euler `(60, 0, 0)`; near/far `0.1 / 100`. Dünya merkezi etrafında 20 birim kamera mesafesi. Takip, titreşim ve perspektif bozulma yok.
- Kayıtlı 1080×1920 başlangıç kadrajı: orthographic size `6.782609`, konum yaklaşık `(0,17.502682,-10.117478)`. Ekran değişince bu değerler sunum bileşenince yeniden hesaplanır.
- 6×6 için muhafazakâr sunum zarfı: merkez `(0, 0.25, 0)`, boyut `(6.5, 0.9, 6.5)`, kadraj payı `1.08`. Aynı zarf 5×5'i de kapsar. Bu zarf, model importunda gerçek renderer sınırlarıyla tekrar kontrol edilmelidir; Core tahta verisi veya yeni bir grid dönüştürücü değildir.
- Kamera boyutu ekran oranına göre genişlik ve yükseklik gereksiniminin büyüğünü alır. Kamera konumu, tahta merkezini safe area içindeki BoardViewport merkezine taşır. Tam ekran kamera arka planı korur; tahta HUD/alt kontrol alanından ayrı kadrajlanır.
- `Screen.safeArea` normalize edilerek SafeArea anchor'larına uygulanır. Runtime'da yalnız ekran boyutu veya safe area değiştiğinde düzen yenilenir. Editör önizlemesinde yeniden hesaplanır; Core okunmaz/değiştirilmez.
- CanvasScaler: `Scale With Screen Size`, `1080×1920`, Match Width/Height `0.5`.
- SafeArea içi anchor'lar: TopHUD `(0.04,0.82)–(0.96,0.98)`, BoardViewport `(0.04,0.22)–(0.96,0.80)`, BottomControls `(0.04,0.03)–(0.96,0.20)`, OverlayRoot tüm güvenli alan. Offsetler sıfırdır.
- PlayerSettings: varsayılan `1080×1920`, Portrait; landscape ve ters portre otomatik dönüşleri kapalı. Fiziksel cihaz çözünürlüğü sabitlenmez.

## Işıklandırma ve arka plan

- Solid Color arka plan: RGB `(0.055, 0.075, 0.11)`, alpha 1; skybox ve fog yok.
- Key Light: Directional, Euler `(50,-30,0)`, intensity `1.2`, hafif sıcak RGB `(1,0.95,0.87)`, soft shadows / strength `0.65`.
- Trilight ambient: sky `(0.45,0.5,0.6)`, equator `(0.25,0.3,0.38)`, ground `(0.16,0.18,0.23)`.
- Yeni volume/profile yok; kamera post-processing ve HDR kapalı. Görsel ayarlar ilk sahne iskeletidir, nihai sanat tasarımı değildir.

## Değiştirilen/eklenen dosyalar

- `Assets/Scenes/BlockRift_Gameplay.unity` ve `.meta`.
- `Assets/_BlockRift/Presentation/Camera/GameplayViewportLayout.cs`, Presentation asmdef ve yeni klasör/dosya `.meta` dosyaları.
- `Assets/_BlockRift/Editor/GameplaySceneSetup.cs`, Editor asmdef ve `.meta` dosyaları. `Block Rift > Scene > Create Gameplay Foundation` komutu mevcut sahneyi ezmez. Bu araç model veya oyun mantığı üretmez.
- `Assets/_BlockRift/Tests/Presentation/GameplaySceneTests.cs`, test asmdef ve `.meta` dosyaları.
- `ProjectSettings/ProjectSettings.asset`: yalnız hedef boyut/yön ayarları.
- Bu rapor.

Core, mevcut test kaynakları, GDD, paket manifesti ve mevcut sahneler korunmuştur. Build Settings başlangıç sahnesi bu görevde değiştirilmedi; gameplay sahnesi Project penceresinden açılır.

## Doğrulama

Kullanıcının açık Unity oturumu kapatılmadı. Kaynakların SHA-256 ile eşitliği kontrol edilen `Temp/GameplayFoundationValidation` projesinde, mevcut yerel PackageCache paketleri `file:` bağımlılıklarıyla kullanıldı. Ana projenin manifesti değişmedi. İlk sandbox denemesi lisans IPC erişiminde bekledi; yalnız bu görevin süreci kapatıldı ve izole doğrulama uygun erişimle tekrar başlatıldı. İlk sahne oluşturma denemesinde Unity'nin kaydedilmemiş boş batch sahnesine additive sahne açma kısıtı görüldü; batch başlangıcı için düzeltildi. Son oluşturma ve test çalışmaları başarılıdır.

Gerçekte yürütülen Unity komutları (proje kökünden, yollar çalıştırılırken mutlak olarak çözüldü):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -executeMethod BlockRift.Editor.GameplaySceneSetup.Create -quit -logFile 'C:/Users/sahba/BlockRift/TestResults/GameplayFoundation/create-final.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests' -testResults 'C:/Users/sahba/BlockRift/TestResults/GameplayFoundation/editmode.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/GameplayFoundation/editmode.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation' -runTests -testPlatform EditMode -testFilter 'BlockRift.Tests.GameplaySceneTests' -testResults 'C:/Users/sahba/BlockRift/TestResults/GameplayFoundation/delivered-scene.xml' -logFile 'C:/Users/sahba/BlockRift/TestResults/GameplayFoundation/delivered-scene.log'
```

- Sahne oluşturma/derleme: **PASS**, Unity çıkış kodu 0.
- Tam EditMode çalışması: **82 geçti, 0 başarısız, 0 atlanan** = değişmeyen 69 Core + 13 yeni sunum testi.
- Ana projeye aktarılırken script GUID'si ana projedeki mevcut `.meta` ile eşlendi. Ardından teslim edilen sahne ve script meta dosyalarının aynısı izole projede yeniden açıldı: **13/13 geçti**. Teslim edilen/test edilen sahne SHA-256: `F959439EE6B344608EBD9E580B2BDDE65CD3F035B5043FA76628BF5D5E004687`.
- Kadraj: hem 5×5 hem 6×6 için 1080×1920, 1080×2400, 1080×2520, 1536×2048, 1920×1080 ve 720×1280. Her durumda asimetrik safe area, çerçeveli tahta köşeleri, -0.18…0.30 yükseklik zarfı ve near/far clipping kontrol edildi. Yatay test, portre kilidine ek dayanıklılık kontrolüdür.
- Sahne testi: boş model kökleri, mesh/collider/Rigidbody bulunmaması, tek AudioListener ve CanvasScaler/yerleşim alanları kontrol edildi.
- `source-hashes.json`: ana ve izole projede test edilen 14 `.cs`/`.asmdef` dosyası birebir aynı.

Meta denetimi, verilen Python çalıştırıcısıyla gerçekten yürütüldü:

```powershell
& 'C:/Users/sahba/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
```

Son denetim aynı script'i `runpy.run_path` üzerinden de çalıştırdı: **88 dosya, 4 mevcut hata, 0 uyarı**, çıkış kodu 1. Dört hata yalnız değiştirilmemiş `Assets/Settings/DefaultVolumeProfile.asset` satırları 78, 119, 264, 832 içindeki eski script GUID referanslarıdır; `STAGE1_REPORT.md` ile aynıdır. Yeni sahne/sunuma ait eksik veya çakışan GUID hatası yoktur. Ana projenin genel denetimi tamamen yeşil diye raporlanmaz. Yeni sahne bu volume profile'ını referanslamaz.

`git diff --check -- ProjectSettings/ProjectSettings.asset` geçti. Core, mevcut EditMode testleri, SampleScene, AssetPreview, GDD ve ana paket manifestinde diff bulunmadığı kontrol edildi. Commit/deploy/publish yapılmadı.

## Kalan sınırlar

- Açık Unity penceresinin görüntüsü iki kez zaman aşımına uğradığı için interaktif Game görünümü/Device Simulator görsel denetimi yapılamadı. Headless kamera projeksiyon testleri cihaz üzerindeki görsel kalite/performance ölçümü değildir.
- Gerçek 5×5/6×6 tahta ve oyun taşı modellerinin importu/yerleşimi bu aşamanın dışındadır. Boş kökler henüz Core'a bağlı değildir.
- GDD §§4–7 hareket, tetikleme, cascade, kazanma ve yardım davranışları uygulanmadı/değiştirilmedi. Bu sahne çalışması onların doğrulandığı iddiasını taşımaz.
