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
5. Mevcut ana sahne: `Assets/Scenes/SampleScene.unity`. Ayrı asset inceleme sahnesi: `Assets/Scenes/AssetPreview.unity`.

## Mevcut kapsam

- Saf C# hücre, parça, bölüm ve tahta modelleri; `BoardValidator`.
- Core için 69 EditMode test vakası ve öğretici Türkçe yorumlar.
- Blender'da hazırlanmış blok, sabit taş ve yıldızlı ana parça kaynakları; Unity FBX assetleri.
- Mevcut sahneler; henüz tamamlanmış oynanabilir bir bulmaca sürümü değildir.

Hareket motoru, tetikleyici/gravity çözümü, Undo/Hint servisleri, solver ve bölüm üreticisi henüz uygulanmadı. Önceki geçici 5×5 görsel tahta çalışması bu ilk commit'e dahil değildir.

## Klasörler

| Klasör | İçerik |
|---|---|
| `Assets/_BlockRift/Core` | Unity bağımlılığı olmayan veri ve doğrulama kodu |
| `Assets/_BlockRift/Tests/EditMode` | Core testleri |
| `Assets/_BlockRift/Art` | FBX, materyal ve doku assetleri |
| `Assets/Scenes` | Unity sahneleri |
| `ArtProduction` | Güncel Blender kaynakları ve üretim scriptleri |
| `docs` | Oyun tasarımı ve tarihsel inceleme raporları |
| `.agents/skills` | Projeye özel ve destekleyici geliştirme yönergeleri |

## Kurallar ve testler

Onaylı mekaniklerin kaynağı `docs/GAME_DESIGN.md`; geliştirme sözleşmesi `AGENTS.md` dosyasıdır. Oyun, Hint, solver ve generator ileride aynı Core geçişini kullanmalıdır. Tarihsel raporlar mevcut sahnelerin güncel doğrulaması olarak yorumlanmamalıdır.

Unity'de **Window → General → Test Runner → EditMode → Run All** ile mevcut testler çalıştırılabilir. 69 testin önceki aşamada geçtiği kaydedilmiştir; depo hazırlığı sırasında yeniden çalıştırılmış bir oyun testi iddiası yoktur.

## Depo hijyeni

- Unity `.meta` dosyaları assetleriyle birlikte saklanır.
- `.fbx` ve `.blend` dosyaları Git LFS kullanır. LFS olmadan yalnız küçük işaretçi dosyaları alınır.
- Önbellekler, yerel IDE ayarları, test çıktıları, Blender yedekleri, kaldırılmış asset arşivleri ve sır dosyaları Git'e eklenmez; yerel diskte korunur.
- Üretim scriptlerinin proje kökü çalışma makinesine bağlı sabit bir kullanıcı yolu içermez. Blender'da script dosyası üzerinden veya proje içindeki `.blend` açıkken çalıştırın.
- `ArtProduction` tarihsel üretim adımlarını da içerir; bunları topluca otomatik çalıştırmayın. `Obstacle_1x1_Stone/Final/refine_final.py` içinde önceden mevcut bir girinti hatası vardır; depo hazırlığı kapsamında model üretimi çalıştırılmadı veya bu hata onarılmadı.
- Unity Cloud proje/organizasyon bağlantısı kişiye özeldir; bu ilk snapshot'ta paylaşılmaz. Kendi hizmet bağlantılarınızı yerelde kurun ve kimlik bilgilerini commit etmeyin.
