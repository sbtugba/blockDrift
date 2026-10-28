# Block Rift — bağımsız Level 1 tasarım challenge raporu

> Durum (2026-10-28): Bu belge uygulanmış eski `rulesVersion=1` bölümünü ve o tarihteki testleri kaydeder. Yeni şekle bağlı hareket kurallarına geçiş henüz yapılmadı; buradaki çözüm güncel GDD kuralları için kanıt değildir.

27 Ekim 2026. Önceki orta-duvar düzeni kaldırıldı; yeni Level 1 **Hedef Cebi** olarak bağımsız tasarlandı. Block-rift-level-design skill'i tasarım değerlendirmesinde, level-generator skill'i teknik kanıt sözleşmesinde kullanıldı. GDD §§3–4, 5, 7, 10–11 ve başlangıç kararlılığı §13.1 korundu. Tahta boyutu, blok sayısı veya hamle sayısı için önceden bir zorluk kotası konulmadı.

## 1. Referansla ilişki ve bağımsızlık

Referans yalnız alan kullanımı, farklı şekiller ve planlama kalitesi için incelendi. Referanstaki beş hareketli engel kombinasyonu, dikey uzun bloklar, yıldız (0,5), hedef (5,4), taşlar (1,5)/(5,3) veya çözüm sırası alınmadı. Görseldeki “Min. 9” etiketi doğrulanmış bir Solver sonucu veya tasarım hedefi sayılmadı.

Seçilen tasarımda yıldız (5,5), görünür hedef (2,0), 2×2 ve 3×1 engeller ile dört farklı işlevli taş var. Referanstaki engeller arasından geçme düzeni yerine, **bir hedef cebinin girişini açma ve büyük bloğa yeterli park alanı sağlama** problemi kuruldu. Önceki Level 1'in tam genişlikteki orta duvarı, 4×1+1×1 ilişkisi ve üst sağ hedefi de korunmadı. Bu, referansın veya önceki bölümün döndürülmüş/aynalanmış sürümü değildir.

## 2. Elle tasarlanan üç aday

Koordinatlar sol alt (0,0); +x sağa, +y yukarı/Unity +Z. Dikdörtgen origin'i sol alt hücredir. Tüm adaylar 6×6, tek 1×1 yıldızlı, tetikleyicisiz ve BoardValidator açısından geçerlidir.

| Aday | Yıldız → hedef | Hareketli engeller (boyut, origin) | Sabit taşlar |
|---|---|---|---|
| A — Alan açma zinciri | (4,0) → (1,4) | gate 2×2 (0,4); blue 3×1 (2,4); bridge 2×1 (2,3); support 1×1 (3,2) | (0,3), (3,1) |
| B — Hedef Cebi, seçilen | (5,5) → (2,0) | gate 2×2 (2,1); blue 3×1 (1,3) | (1,0), (3,0), (1,1), (4,1) |
| C — Yıldızı geçici park et | (3,3) → (0,3) | gate 3×1 (0,3); blue 2×1 (4,3) | (0,2), (0,4), (2,2), (2,4), (5,2) |

| Aday | Mevcut BFS minimumu | İlk kanıtta ziyaret | Örnek minimum çözüm | Tasarım kararı |
|---|---:|---:|---|---|
| A | 6 | 50688 | star ←3; bridge →2, ↓1; blue ↓1; gate →2; star ↑4 | REVISE/REJECT: iki eksenli bridge manevrası daha dolaylı; support hareket etmeden optimum çözüm var ve hedef başlangıçta örtülü |
| B | 5 | 1948 | star ↓3; blue ↑2; gate ↑2; star ←3, ↓2 | ACCEPT: görünür hedef, okunabilir iki engel bağımlılığı, alt/orta/üst alan kullanımı |
| C | 6 | 2034 | star ↓1; blue ↑2; gate →3, ↑1; star ↑1, ←3 | REJECT for Level 1: gerçek karşılıklı yol açma fikri var; ancak yıldızın önce uzaklaşıp geri gelmesi daha dolaylı ve bazı optimumlar iki satıra sıkışıyor |

Bütün bu minimumlar mevcut BoardSolver'ın tamamlanan BFS sonuçlarıdır; örnek yollar aynı MovementEngine üzerinden WIN olarak tekrarlandı. Seçim “en az hamleli aday kazanır” puanlamasıyla yapılmadı.

### A'nın kalite incelemesi ve sınırları

Amaçlanan support→bridge→blue→gate zinciri tam gerçekleşmedi. Solver, support'u hiç oynatmadan bridge'i önce sağa, sonra aşağı park ederek çözüm buldu. Support dondurulduğunda da kazanılabiliyor (5375 ziyaret); onu hareketli olarak sunmak için yeterli öğretici gerekçe yok. Blue dondurulunca tam tarama 10200 durumda, bridge dondurulunca 3412 durumda UNSOLVABLE döndü. Gate dondurma taraması 100000 durum limitinde **INCOMPLETE** kaldı; bu taramaya dayanarak çözümsüzlük iddiası yapılmıyor. A'nın bütün minimum yolları ve tam grafiği çıkarılmadı; kabul edilmedi. Hareketli support'un destek işlevi ile hareketinin zorunluluğu birbirine karıştırılmadı.

### C'nin kalite incelemesi

C'de hem yıldızın hem mavinin yer değiştirmesi gate'in manevrasını açıyor. Böylece yıldız sadece teslim edilecek parça değil, geçici olarak park edilmesi gereken bir engel de oluyor. İki engel ayrı ayrı dondurulduğunda tam taramalar UNSOLVABLE (gate 520, blue 51 durum). Tam grafik 4577 durum/56542 geçiş; 66 optimum komut dizisinin tamamı replay edildi. Yıldızın optimumdaki farklı satır kullanımı 2–4, sütun kullanımı 4; süpürülen hücre 12–17. 66 dizi, 66 ayrı strateji demek değildir.

Taş çıkarma deneylerinde (0,2)/(0,4) kaldırılınca yalnız star+gate ile 4 hamlelik yan giriş; (2,2)/(2,4) kaldırılınca maviyi gerektirmeyen 5 hamlelik gate kaçışı oluştu. (5,2) çıkarılınca başlangıç UnstableStart oldu. Taşlar işlevsiz görünmüyor; ancak hedefin başlangıçta örtülü olması, karşılıklı park fikri ve bazı dar optimum rotalar ilk öğretici için B kadar okunabilir bulunmadı. Bu yorum yalnız taş veya blok sayısına dayanmaz.

## 3. Seçilen Level 1 — koordinatlı yerleşim

```text
       x=0 1 2 3 4 5
y=5      . . . . . S
y=4      . . . . . .
y=3      . B B B . .
y=2      . . G G . .
y=1      . # G G # .
y=0      . # E # . .
```

S=star; G=tek 2×2 gate; B=tek 3×1 blue; #=sabit taş; E=görünür hedef. Birbirine bitişik G/B hücreleri ayrı parçalar değildir.

| ID / nesne | Başlangıç | Mevcut prefab / materyal |
|---|---|---|
| star, 1×1 | (5,5) | Special_1x1_Star, mevcut krem/altın |
| gate, 2×2 | (2,1) | Block_2x2, mevcut BR_Plastic_Green |
| blue, 3×1 | (1,3) | Block_3x1, mevcut BR_Plastic_Blue |
| sabit taşlar | (1,0), (3,0), (1,1), (4,1) | Obstacle_1x1_Stone, mevcut stone materyali |
| hedef | (2,0) | mevcut Exit_Cell prefab'ı, mevcut turkuaz materyal |

İlk B denemesinde gate hedefi örtüyordu. Görünür hedef kalite testi bu sürümü reddetti; gate bir satır yukarı alınıp hedef açık bırakıldı ve cebin yan girişleri yeniden tasarlandı. Bu revizyonun Solver hesabı yeniden yapıldı. Materyal, UI veya hedef prefab'ı değiştirilerek sorun gizlenmedi.

### Neden bu parçalar var?

- **Gate:** Hedef cebinin üst girişini kapatan büyük blok. Bir satır yükselince yakındaki hücre boş görünür, fakat cebin üst ağzı hâlâ kapalıdır; yeterli park alanı gerekir.
- **Blue:** Gate'in yukarı çıkmasını engeller. Birinci planlama kararı blue'ya üstte yer açmaktır. Daha uzun 3×1 ayakizi, yatayda bir hücre itmenin büyük bloğa yeterli boşluk yaratmadığını görünür kılar.
- **Taş (1,0):** Hedefe soldan doğrudan giriş kısa yolunu kapatır.
- **Taş (3,0):** Sağ alt kısa yolu kapatır, gate'e başlangıç desteği verir ve aşağı kaçışını engeller.
- **Taş (1,1):** Gate'in başlangıçta sola park ederek üst alan problemini atlamasını önler.
- **Taş (4,1):** Aynı kısıtı sağ tarafa uygular.

İki engel ve dört taş bu işlevlerden çıktı. Ek renkli bloklar doluluk uğruna eklenmedi; bir bloğu tek başına kenara itip kazanmak mümkün değildir.

## 4. Teknik kanıtlar ve gerçek bağımlılıklar

| Seçilen B ölçümü | Sonuç |
|---|---|
| Başlangıç | BoardValidator geçerli; hedef açıkta; destek/çakışma/sınırlar geçerli |
| Solver | SOLVED; minimum 5; replay WIN |
| Tam grafik | COMPLETE; 4188 durum, 51439 yönlü geçiş |
| Tüm minimum komut dizileri | COMPLETE; 3 dizi, hepsi Core üzerinde WIN |
| Her optimumda hareket eden engeller | gate ve blue, ikisi de |
| Her optimumda yıldız rotası | 6 farklı satır, 4 farklı sütun |
| Süpürülen farklı hücreler | 19; ara hücreler ve hareket eden dikdörtgenlerin ayakizleri dahil |
| Erişilebilir çıkmazlar | 0 |

Gate başlangıçta hiçbir yönde hareket edemiyor: aşağı ve yanlar taşlarla, yukarısı blue ile kapalı. Dolayısıyla **blue → gate → yıldızın hedef cebine girişi** gerçek bir bağımlılıktır. Yıldızın sağ kenardan ilk yaklaşma hareketi bağımsız bir hazırlık olabilir; tek bir ezberlenmiş sıra dayatılmaz.

Gate dondurulduğunda 288, blue dondurulduğunda 24 durumluk tamamlanan filtreli grafiklerde kazanma yok. Böylece her ikisinin hareketi sadece bir örnek çözüm için değil, bütün kazanma yolları için gereklidir.

**Araç ayrımı:** BoardSolver minimum yol ve Solved/Unsolvable/Incomplete sonucu sağlar. Dondurma, tüm optimum yollar, alan kullanımı ve ters geçiş denetimi onun hazır API özellikleri değildir. Mevcut Editor kalite aracı bunları, aynı Core EnumerateLegalMoves/Apply üzerinde grafik incelemesi yaparak hesapladı. Yeni çarpışma, fizik veya runtime Solver yazılmadı. Dondurma sırasında hareketli parçayı FixedCell'e dönüştürmek yerine ilgili ID'nin kenarları filtrelendi; özgün geçerli level ve hedef işgali kuralları korundu.

0 çıkmaz kanıtı: tam grafikte terminal olmayan her geçişin ters komutu Core'da önceki snapshot'a döndü. Bu yüzden her erişilebilir terminal olmayan durum, kazanma yolu bilinen başlangıca geri dönebiliyor. Bu kanıt tetikleyicili gelecekteki bölümlere genellenmedi.

### Minimum çözüm

1. star aşağı 3: (5,5) → (5,2).
2. blue yukarı 2: (1,3) → (1,5).
3. gate yukarı 2: (2,1) → (2,3).
4. star sola 3: (5,2) → (2,2).
5. star aşağı 2: (2,2) → (2,0), bırakma beklenmeden WIN.

Üç minimum komut dizisi, üç ayrı strateji sayılmadı. 5'ten kısa çözüm olmadığı BFS ile doğrulandı; bütün minimumlar iki engeli de hareket ettiriyor.

### Farklı bir park stratejisi

Soldaki boş alan da çalışır: blue ↑1, →2; gate ↑1, ←2; star ←3, ↓5. Bu 6 hamlelik yol Core replay testiyle doğrulanır ve minimum diye sunulmaz. Oyuncu gate'i üstte park etmek yerine sola çekip yıldız için düz bir iniş hattı hazırlayabilir. Başka uzun çözümler olabilir; bütün uzun yollar veya strateji sınıfları sayılmadı.

### Kısıt kaldırma deneyleri

| Kaldırılan parça/taş | BFS sonucu | Anlam |
|---|---|---|
| blue | 4 hamle | Büyük blok için ön hazırlık problemi azalıyor |
| gate | Karşı-olgusal 3 hamle; başlangıç UnstableStart | Gate ana kısıt ve blue'nun desteği |
| (1,0) | 3 hamle, yalnız star | Sol alt bypass açılıyor |
| (3,0) | Karşı-olgusal 2 hamle, yalnız star; UnstableStart | Sağ alt bypass ve başlangıç desteği |
| (1,1) | 4 hamle | Gate sola erken kaçabiliyor |
| (4,1) | 4 hamle | Gate sağa erken kaçabiliyor |

UnstableStart veren deneyler kabul edilebilir yeni bölüm değildir; yalnız ilgili kısıtın etkisini anlamak için yapılan snapshot karşılaştırmalarıdır. Hiçbir taş sırf görsel dolulukla gerekçelendirilmedi.

## 5. Alan kullanımı ve Çok Kolay değerlendirmesi — tasarım yorumu

Üst sıra blue'nun park alanı; hemen altındaki iki satır gate'in alanıdır. Sağ kenar yıldızın üstten orta bölgeye yaklaşma koridorudur. Orta şerit yatay geçişi, alt cep son inişi sağlar. Sol boşluklar farklı park çözümünde kullanılır. Yıldızın en alta erken inmesi hedefe otomatik erişim sağlamaz; oyuncu dönüşünü doğru satırda yapmayı düşünür. Bu deneme geri çevrilebilir.

Her boş hücrenin tek tek zorunlu olduğu veya her hücreden geçildiği iddia edilmiyor. Alan kalitesi doluluk oranıyla puanlanmadı. Önceki tam genişlikte orta duvar yerine, üstte manevra alanı oluşturup alt hedef cebini erişilebilir kılmak gerekiyor.

Çok Kolay seçimi öznel uzman değerlendirmesidir: hedef görünür, iki engel ilişkisi okunabilir, tüm hatalar normal hareketlerle geri çevrilebilir, trigger/süre baskısı yok ve kısa neden-sonuç zinciri var. A'daki iki eksenli dolaylı zincir ve C'deki yıldızı önce park edip geri getirme kararı daha fazla açıklama gerektiriyor. Karar sadece 5/6 hamle veya blok sayısına dayanmaz. İnsan oyuncu testi yapılmadı; bilimsel zorluk iddiası yoktur.

## 6. Unity entegrasyonu ve koruma

Mevcut BlockRift_Gameplay sahnesi, BoardRoot/PiecesRoot ve 1 hücre=1 birim adaptörü kullanıldı. Sadece seçilen B sahneye bağlandı. A/C sahneleri oluşturulmadı. Palette'te gate→Block_2x2, blue→Block_3x1 eşlemesi değişti; materyal referansları aynı kaldı. Material üreten Build çağrılmadı; mevcut RefreshPuzzle yerleşim girişi kullanıldı.

Hazır hareketli prefab ayakizleri 1×1, 2×1, 3×1, 4×1, 2×2'dir. GDD'deki 1×2, 1×3, 1×4 ve 3×3 için katalogda uygun gerçek görsel yok. Bunlar ölçeklenerek taklit edilmedi. Seçilen bölümün gerekli assetlerinde eksik yoktur.

Hareket motoru, BoardSolver, Core kuralları, GameSession, input, kamera, arka plan, URP materyal sistemi, tahta geometrisi ve Blender assetleri değiştirilmedi. Önceden onaylı perspective kamera korundu. Otomatik generator, yeni ana sahne veya yeni paket eklenmedi.

![Gerçek 1080×1920 Unity Game View](C:/Users/sahba/BlockRift/docs/Gameplay_6x6_GameView.png)

## 7. Test ve teslim kanıtları

TDD: yeni hedef cebi testi önce eski düzeni reddetti (4 testte 1 başarısız). Ardından hedef görünürlüğü testi, hedefi örten ilk B denemesini de reddetti (4 testte 1 başarısız). Son revizyon ve alternatif sol park yolu mevcut Core üzerinden doğrulandı.

Son EditMode/PlayMode sonuçları teslim kanıtlarıyla raporun sonunda belirtilir. Orijinal 69 Core testi ile kamera, grid, prefab ve mevcut input regresyonları korunur. Fiziksel mobil cihaz testi yapılmadı; PlayMode cihaz olayları InputTestFixture ile çalıştırıldı. Gerçek Game View ayrıca incelendi.

Çalıştırılan komutlarda Unity yolu `C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe`, doğrulama proje yolu `C:/Users/sahba/BlockRift/Temp/GameplayFoundationValidation` idi. Ana çıktı klasörü `C:/Users/sahba/BlockRift/TestResults/Level1Independent`:

- `-batchmode -nographics -executeMethod BlockRift.Editor.LevelOneQualityAudit.Run -quit` (quality-final.log, ardından görünür hedef revizyonu için visible-quality.log).
- `-batchmode -nographics -executeMethod BlockRift.Editor.LevelOneSetup.RefreshPuzzle -quit` (visible-setup.log).
- `-batchmode -nographics -runTests -testPlatform EditMode -testFilter BlockRift.Tests -testResults .../editmode-delivered.xml -logFile .../editmode-delivered.log`.
- `-batchmode -runTests -testPlatform PlayMode -testFilter BlockRift.Tests.LevelOnePlayTests -testResults .../playmode.xml -logFile .../playmode.log`.
- `-batchmode -executeMethod BlockRift.Editor.GameplayGameViewCapture.CaptureBatch` (gameview.log).

Bu komutların her birinde yukarıdaki `-projectPath` kullanıldı. Grafik gerektiren PlayMode/capture komutlarında `-nographics` kullanılmadı. Ham aday raporu `TestResults/Level1Independent/quality.md`; ilk, hedefi örten B denemesi quality-initial.md içinde ayrı korunur. A'nın dondurma INCOMPLETE sonucu gizlenmedi.

## 8. Dosyalar ve AGENTS.md yönlendirmesi

Yeni: `Editor/IndependentLevelOneCandidates.cs` — yalnız üç manuel aday, generator değil.

Güncellenen: `Core/Board/LevelOne.cs` (sabit bölüm verisi), `Editor/LevelOneQualityAudit.cs` (mevcut analiz aracında aday karşılaştırması/hamle filtresi), `Editor/LevelOneSetup.cs` (şekil eşlemeleri), `Content/LevelOnePalette.asset`, `BlockRift_Gameplay.unity`, `LevelOneQualityTests.cs`, `LevelOnePlayTests.cs`, `GameplaySceneTests.cs`, mevcut LEVEL1_SPEC.md, bu rapor ve Game View görüntüsü. Oyun kuralları veya runtime input kodu değişmedi.

AGENTS.md'deki mevcut madde 5 korunarak tasarım kalitesi, bölüm değerlendirmesi, zorluk dengesi, bağımlılıklar ve alan kullanımında **block-rift-level-design zorunlu** yönlendirmesi kullanıldı. Madde 4'teki **block-rift-level-generator** algoritmik üretim, Solver, replay ve teknik kanıt sorumluluğunu koruyor. Madde 6 artık `level-design`, `puzzle`, `procedural-gen` isimlerini açıkça yalnız yardımcı kaynak olarak belirtiyor. Implementation contract'taki mevcut anlamlı bulmaca ilkesi korunup, minimum hamle sayısının da tek başına zorluk belirleyemeyeceği mevcut cümleye eklendi; tekrarlanan yeni talimat paragrafları oluşturulmadı. AGENTS_SNIPPET.md kopyalanmadı veya değiştirilmedi.

Commit/push yapılmadı. Undo, kayıt ve generator geliştirme bu göreve eklenmedi.

### Son teslim sonuçları

- **EditMode: 161/161 geçti, 0 başarısız, 0 atlanan.** Kanıt: `TestResults/Level1Independent/editmode-delivered.xml`.
- **PlayMode: 6/6 geçti, 0 başarısız, 0 atlanan.** Kanıt: `TestResults/Level1Independent/playmode.xml`.
- Altı hamlelik sol park alternatifi de Core replay testinden geçti. Bir satırlık eksik kaldırmada, diğer engeller o konumda tutulunca hedef cebine erişilemediği ayrı Solver testiyle doğrulandı.
- Ana ve izole projede **44 C#/asmdef dosyası aynı**, kaynak farkı yok. Sahne SHA256 değerleri eşit: `DAA4B3B233F9B829C887D24D78B9F7E72BDA735036B7BEC10388210AD5DAA4C9`.
- Ana editör ve izole gerçek 1080×1920 Game View görüntüleri eşit: `6040759023AA4B7C358F4D6C69867CAE159F7AECC25986194E56FA78BD9BF3BF`.
- **90 mevcut sanat dosyasında 0 hash değişikliği**. Ortak mevcut sahne YAML nesnelerinde yalnız PiecesRoot çocuk listesi değişti; kamera, arka plan, board/alt tabaka ve UI verileri aynı.
- GUID denetimi 218 dosyada 0 uyarı ve önceden bilinen DefaultVolumeProfile.asset içindeki aynı 4 missing-script kaydını raporladı (78,119,264,832; exit 1). Yeni gameplay/kalite aracı bağlantısında hata yok.
- Ana editör ilk capture isteğini henüz eski Core assembly'si yüklüyken mesh sayısı korumasıyla reddetti. Derleme tamamlandıktan sonra tekrar denendi ve gerçek Game View başarıyla alındı; sahneyi veya korumayı kaldırarak geçilmedi.

Kalite kanıtları: `quality.md`, `red.xml`, `visible-goal-red.xml`, `scene-changes.json`, `art-preservation.json`, `delivery-hashes.json`, `gameview.json`, `gameview-main.json`, `meta-check.json`. A'nın kabul edilmeyen tasarımına ait tamamlanmamış alt analiz, seçilen B'nin tamamlanmış minimum/grafik kanıtıyla karıştırılmadı.
