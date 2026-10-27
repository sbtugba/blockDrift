---
name: block-rift-architecture
description: Block Rift Unity 6 C# proje mimarisini kurarken veya değiştirirken kullan. Grid oyun motoru, uygulama servisleri, Unity sunum katmanı, kayıt, test, bağımlılıklar, klasörler ve geliştirilebilirlik ilkelerini uygula.
---

# Block Rift — Architecture Skill

## Amaç ve bağlayıcı kaynaklar

Block Rift, Unity 6 URP üzerinde mobil (iOS/Android), dikey ekran, 2.5D görsel sunumlu; ancak **tamamen hücre-temelli deterministik** mantık kullanan bulmaca oyunudur. Oyun kuralları için proje kökündeki `docs/GAME_DESIGN.md` (bu pakette bulunur) otoritedir. İlgili iş için `block-rift-gameplay`; bölüm çözme/üretme için `block-rift-level-generator` skill'ini de oku. Tasarım dokümanında **karara bağlanmamış** ekonomi veya skor ayrıntılarını onaylı mekanikmiş gibi sabitleme.

Bu skill kullanıcı tarafından kabul edilen katmanlı mimariyi ve uygulama önerilerini içerir; kullanıcı onaylı oyun mekaniğinin yerine geçmez. Yeni bir sınıf veya alt sistem eklemeden önce mevcut projeyi incele, mevcut adlandırma ve konvansiyonları koru, gerekli değişikliği en küçük kapsamda yap.

## Zorunlu bağımlılık yönü

```text
Unity Presentation (MonoBehaviour, GameObject, URP, UI, animasyon)
               ↓
Application (Move/Undo/Hint/Restart/Level orchestration)
               ↓
Core / Domain (pure C#, board state, rules, transitions)
               ↑
Offline Solver & Generator (Core transition API'sini çağırır)

Infrastructure (save files, repositories, config, assets)
  → Application'ın tanımladığı soyut sözleşmeleri uygular
```

- **Core**: `UnityEngine` referansı **yok**. Oyun durumu ve her yasal hamlenin sonucu burada tek kaynaktır. Unity fizik motoru (`Rigidbody`/yer çekimi) oynanış için kullanılmaz.
- **Application**: Kullanıcı komutlarını yönetir, state snapshot/history, seviye tamamlama, yardım tüketimi ve kalıcılık sınırlarını düzenler. UI/animasyon kodu içermez.
- **Presentation**: Grid state'ini sahnede temsil eder. Sürüklemede önizleme yapar; ana parça kapıya ulaşınca bırakmayı beklemeden Core üzerinden kazanmayı kesinleştirir. Diğer hareketlerde bırakılınca Core sonucunu gösterir. Fiziksel transform hiçbir zaman authoritative state değildir.
- **Solver/Generator**: Core'un **aynı** legal move enumeration ve transition yöntemlerini çağırır. İkinci bir fizik/hareket uygulaması yazma.
- **Infrastructure**: JSON/dosya, ayarlar ve derleme-time/asset bağlantıları için adaptör. Dosya yolu ve saklama sürümü platformlardan bağımsız sözleşmeye bağlıdır.

## Önerilen Unity klasör düzeni

```text
Assets/_BlockRift/
  Core/                # Unity-bağımsız C# domain kaynakları
    Board/             # immutable değer nesneleri ve doğrulama
    Rules/             # move legality, trigger, gravity, terminal
  Application/         # commands, orchestration, ports
  Infrastructure/      # save, config, level repository
  Presentation/
    Input/              # touch/pointer -> grid komutu
    Board/              # prefab/mesh/view oluşturma
    Animation/          # state diff -> tween/VFX
    Camera/             # orthographic mobil kadraj
    UI/                 # HUD, menu, dialogs
  Content/              # ScriptableObjects, serialized levels
  Tests/EditMode/       # Core + Application + solver testleri
  Tests/PlayMode/       # Input, rendering coordination, lifecycle
  Scenes/ Prefabs/ Materials/ Audio/
Tools/BlockRift.LevelTools/ # Gerekiyorsa editor/headless offline tool
```

Mevcut projenin fiziksel klasörlerini sırf bu şemaya uymuyor diye taşımak zorunda değilsin. Ayrı `.asmdef` ile (Core → Unity bağımsız, Application → Core, Presentation → Application+Core) döngüsüz bağımlılık doğrulaması yap. EditMode testlerini olabildiğince saf C# düzeyinde çalıştır.

## Domain modeli ve güvenli API

Örnek kavramlar, zorunlu sınıf adları değil:

- `Cell(x,y)` tamsayı koordinat; yatay x, dikey y. **Aşağı yönü** tutarlı tanımla (ör. y-1) ve tüm kod/tests içinde tek standardı kullan.
- `PieceDefinition(id, width, height, kind)` ve `PiecePosition(pieceId, origin)`; sadece önceden tanımlanmış 9 hareketli dikdörtgen boyut.
- `LevelDefinition` değişmez temel tahta/kapı/sabit engel/tetikleyici tanımı.
- `BoardState` parça konumlarını, tüketilmiş tetikleyici bitlerini ve gerekli durum bayraklarını içerir. Render koordinatları içermez.
- `MoveCommand(pieceId, axis, signedCellDistance)` yalnız **tek eksen** üzerinde tamamlanmış hareketi temsil eder.
- `TryApplyMove(level, state, move) -> MoveResult` hiçbir yan etkiye bağlı olmadan sonucu, geçerlilik nedenini, tetikleyici tüketimlerini, düşüş adımlarını ve win durumunu döndürür.
- `EnumerateLegalMoves(level,state)` ve `MakeStateKey(level,state)` solver ile oyun için ortak.

`BoardState` mutasyonunda tek yola bağlı kal: immutable snapshot veya kontrollü copy-on-write. Render/view üzerinde state mutasyonu yapma. Objelerin benzersiz ve kararlı `pieceId`'leri olmalı; **aynı şekilden birden fazla parça** birbirine karıştırılmamalı. Undo için manuel hamle başlamadan **tam** state snapshot al; cascade ve kullanılmış tetikleyiciler dâhil tüm sonuç tek transaction olsun. Animasyon iptalinde veya uygulama kapanışında kısmi state kaydetme.

## Teknik kurallar

1. Core ve solver deterministik olsun: aynı `LevelDefinition + BoardState + MoveCommand` her zaman aynı sonucu üretir; RNG sadece aday bölüm üretiminde seed ile kullanılır.
2. Collider/raycast pointer seçimi için kullanılabilir; gerçek çarpışma **hücre ayakizi** ile hesaplanır. Düşme için frame delta, physics engine veya tween pozisyonu kullanılmaz.
3. Unity lifecycle (`Awake`, `Start`, `Update`) yalnız bağlama, input, animasyon ve sunum için. Domain sınıflarında `MonoBehaviour`, global `GameObject.Find`, statik sahne singleton erişimi yok.
4. Tek bir gameplay state sahibi/controller ve açık dependency injection/composition root oluştur; yeni bağımlılıklar interface üzerinden eklenir. Gereksiz servis katmanı/generic repository üretme.
5. Grid-↔-world conversion tek bileşende; 5×5/6×6 ve portre ekran safe area için test et. Kamera orthographic ve sabit: oyunun kuralları bakış açısından etkilenmez.
6. Level content ile runtime progress'i ayır; seviyeler önceden üretilmiş/sürümle sabitlenmiş dosyalardır, oyuncu cihazında rastgele yeniden üretilmez.
7. Save versiyonlu, atomik/yarım yazılmaya dayanıklı, restore sonrası invariant doğrulamalı; hareket geçmişi ve inventory dâhil. Bozuk save'de veri kurtarma/uygun fallback açıkça raporlanır.
8. Coin/Hint/Undo değerleri konfigürasyonla gelir; sabit kod içi sihirli sayı verme. Kesinleşmeyen yıldız eşikleri ve yeniden ödüllendirme mantığını "TBD"/konfigüre edilebilir tut.
9. C# class/public method, önemli field ve alışılmadık Unity/C# yapıları için kısa, **öğretici Türkçe yorumlar** ekle. Bariz satırları yorumlama; sınırlar/varsayımlar nedenleriyle açık olsun.
10. Asset üretme, görsel tercih belirleme veya GDD mekaniklerini değiştirme bu skill'in işi değildir. Değişiklik mekanik karara dokunursa sessizce yeniden tasarlama; riskleri açıkla.

## Her kod değişikliğinde çalışma sözleşmesi

1. İlgili dosya, bağımlılık ve doküman kurallarını incele.
2. Etkilenen domain invariantlarını ve başarısızlık senaryolarını belirt.
3. Değişikliği küçük, geri alınabilir birimlerde yap; public API'yı nedensiz kırma.
4. Önce domain EditMode testlerini, sonra ilgili PlayMode/integration testlerini koş.
5. Testi **gerçekte** koşamadıysan "çalıştı" deme; nedenini ve manuel doğrulama adımını yaz.
6. Kullanılan varsayımları, kalıcı save/level schema değişimini ve riskleri değişiklik raporunda belirt.

## Kabul kontrolleri

- Core kodu Unity olmadan derlenebilir/test edilebilir.
- Gameplay, Hint, solver ve generator aynı transition logic'e bağlıdır.
- Fiziksel düşüş yalnız trigger sonrasında hesaplanır, animasyon domain durumunu etkilemez.
- Aynı move → aynı state; kaydet/yeniden yükle → aynı state; Undo → önceki state.
- Projeye gelişi güzel singleton, bağımlılık döngüsü veya `Update()` içinde tahta kuralı eklenmez.

## Kullanıcı onaylı açıklamalar — 23 Ekim 2026
- GDD v1.2 esas alınır. Ana parça sürükleme sırasında kapıya ulaştığı anda kazanır; parmağın kaldırılması beklenmez ve aynı sürükleme ikinci kez sayılmaz.
- Birlikte düşebilen üst/alt hareketli bloklar aynı adımda ilerler; o adımda boşalan hücre kullanılabilir. Hareket etmeyen blokların hücreleri kullanılamaz.
- Undo tahta, tetikleyici ve hamle geçmişini geri yükler; harcanan coin veya kullanılan yardım hakkını geri vermez. Tahta snapshot'ı profil ekonomisinden ayrıdır.
- Hint yalnız güncel durum için doğrulanmış ipucu başarıyla sunulduğunda tüketilir. Başarısız/kesik arama, başarısız sunum veya eskimiş sonuç hak tüketmez.
- Aşama 1 BoardValidator yapısal geçerlilik ve geçici başlangıç kararlılığını denetler; çözülebilirlik, tetikleyicisiz yol veya yayın kabulü kanıtı değildir.

