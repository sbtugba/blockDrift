# Block Rift

Unity 6 URP üzerinde geliştirilen, portre ekranlı bir mobil bulmaca oyunu. 2.5D sunumun altında Unity fiziğinden bağımsız, deterministik bir hücre simülasyonu hedeflenir. GitHub deposunun adı `blockDrift`, oyunun adı **Block Rift**'tir.

## Projeyi açma

1. Git ve Git LFS kurulu olmalıdır. İlk kullanımda `git lfs install` çalıştırın.
2. Depoyu klonlayın:

   ```sh
   git clone https://github.com/sbtugba/blockDrift.git
   cd blockDrift
   git lfs pull
   ```

3. Unity Hub'dan bu klasörü **Unity 6000.3.23f1** ile açın. Kesin sürüm `ProjectSettings/ProjectVersion.txt` içindedir.
4. Unity paketleri `Packages/manifest.json` ve `packages-lock.json` üzerinden yükler. `Library` ve `Temp` otomatik oluşturulur.
5. Oynanabilir ana sahne: `Assets/Scenes/BlockRift_Gameplay.unity`. Sahneyi açıp Play ile **Küçük Aktarma** bölümünü çalıştırın. Dokümantasyon dizini: [docs/README.md](docs/README.md).

## Mevcut kapsam — 2026-10-28

- Saf C# Core, şekle bağlı manuel hareket **rulesVersion 2**, sekiz şekil ve ortak geçişi kullanan exact BFS/replay.
- Application GameSession, mouse/touch input ve gerçek Blender/URP assetleriyle oynanabilir 6×6 **Küçük Aktarma** Level 1; Core ile doğrulanmış minimum **8 hamle**.
- 5×5/6×6 grid ve kamera testleri korunur; gerçek 5×5 board modeli katalogda henüz yoktur.
- Bu migration'da 135 saf Core/Application, 214 Unity EditMode ve 8 PlayMode testi geçti. Saf testler EditMode toplamının alt kümesidir.
- Gravity/cascade, Undo/Hint, save/ekonomi ve Level Generator henüz uygulanmadı; tamamlanmış oyun sürümü iddiası yoktur.

Ayrıntılar ve gerçek Game View: [Level 1 teslim raporu](docs/LEVEL1_REPORT.md). Eski kural içerikleri otomatik dönüştürülmez; sürüm uyuşmazlığında yeniden doğrulama gerekir.

## Klasörler

| Klasör | İçerik |
|---|---|
| `Assets/_BlockRift/Core` | Unity bağımlılığı olmayan veri, doğrulama, hareket ve Solver |
| `Assets/_BlockRift/Tests/EditMode` | Core testleri |
| `Assets/_BlockRift/Art` | FBX, materyal ve doku assetleri |
| `Assets/Scenes` | Unity sahneleri |
| `ArtProduction` | Güncel Blender kaynakları ve üretim scriptleri |
| `docs` | Oyun tasarımı ve tarihsel inceleme raporları |
| `.agents/skills` | Projeye özel ve destekleyici geliştirme yönergeleri |

## Kurallar ve testler

Onaylı mekaniklerin kaynağı `docs/GAME_DESIGN.md`; geliştirme sözleşmesi `AGENTS.md` dosyasıdır. Oyun ve Solver aynı Core geçişini kullanır; gelecekteki Hint/Generator da bunu kullanmalıdır. Tarihsel raporlar mevcut sahnelerin güncel doğrulaması olarak yorumlanmamalıdır.

Unity'de **Window → General → Test Runner** üzerinden EditMode ve PlayMode testleri çalıştırılabilir. Gerçekte yürütülen komutlar, TDD red/green ve ortam sınırları [teslim raporunda](docs/LEVEL1_REPORT.md) kayıtlıdır.

## Depo hijyeni

- Unity `.meta` dosyaları assetleriyle birlikte saklanır.
- `.fbx` ve `.blend` dosyaları Git LFS kullanır. LFS olmadan yalnız küçük işaretçi dosyaları alınır.
- Önbellekler, yerel IDE ayarları, test çıktıları, Blender yedekleri, kaldırılmış asset arşivleri ve sır dosyaları Git'e eklenmez; yerel diskte korunur.
- Üretim scriptlerinin proje kökü çalışma makinesine bağlı sabit bir kullanıcı yolu içermez. Blender'da script dosyası üzerinden veya proje içindeki `.blend` açıkken çalıştırın.
- `ArtProduction` tarihsel üretim adımlarını da içerir; bunları topluca otomatik çalıştırmayın. `Obstacle_1x1_Stone/Final/refine_final.py` içinde önceden mevcut bir girinti hatası vardır; depo hazırlığı kapsamında model üretimi çalıştırılmadı veya bu hata onarılmadı.
- Unity Cloud proje/organizasyon bağlantısı kişiye özeldir; bu ilk snapshot'ta paylaşılmaz. Kendi hizmet bağlantılarınızı yerelde kurun ve kimlik bilgilerini commit etmeyin.
