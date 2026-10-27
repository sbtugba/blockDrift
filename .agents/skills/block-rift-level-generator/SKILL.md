---
name: block-rift-level-generator
description: Block Rift 5x5/6x6 bulmaca bölümlerini offline algoritmik üretirken, solver kurarken, zorluk/çıkmaz analizinde, Hint doğrulamasında ve bölüm kabul testlerinde kullan. Gameplay kural motorunu yeniden yazmadan tekrar kullan.
---

# Block Rift — Level Generator & Solver Skill

## Temel taahhüt

Bölümler **çalışma sırasında rasgele üretilemez**: offline üretilir, ispatlanmış sonuçlarıyla **sürümleyerek kaydedilir** ve tüm oyunculara aynı sıralı bölümler dağıtılır. Referans `docs/GAME_DESIGN.md` ve `block-rift-gameplay/SKILL.md`. Solver, Hint, live game ve generator **aynı Core TryApplyMove/EnumerateLegalMoves API'sini** paylaşır; birbirinden farklı gravity implementasyonları yasak.

## Girdi ve çıktı sözleşmesi

```text
LevelDefinition:
  levelId, schemaVersion, rulesVersion, width(5|6), height(5|6)
  targetPieceId, targetStartCell, exitCell
  movablePieces: [{stableId, shape(width,height), originCell}]
  fixedCells: [Cell]
  gravityTriggers: [{id, Cell, initiallyAvailable:true}]

LevelAnalysis:
  rulesVersion, solverVersion, generatorSeed
  validationErrors[], status(ACCEPT|REJECT|INCOMPLETE)
  solvable, shortestMoveCount, optimalProofPath[]
  noTriggerWinningPath[] / noTriggerSolutionExists
  uniqueReachableStateCount, deadEndStateCount
  reversibleTraps, irreversibleDeadEnds, solutionDiversity
  cascadeMetrics, triggerRiskMetrics
  layoutSimilarity, strategySimilarity, difficultyFactors
  rejectionReasons[], analysisLimits, elapsedCompute
```

Bu format **önerilen şema**dır, mevcut oyun dosya biçimi değil. JSON/ScriptableObject veya başka serileştirme formatı seçilirse normalize ederek aynı semantiği koru. `LevelDefinition` asla oyuncunun geçici BoardState/coin/Undo hakkını içermez.

## Aday bölüm doğrulama — erken ret

1. Boyut 5×5/6×6; tam bir 1×1 target ve tek exit; exit grid içi.
2. Yalnız dokuz dikdörtgen shape: `1×1`, `2×1`, `1×2`, `1×3`, `3×1`, `2×2`, `3×3`, `1×4`, `4×1`.
3. Hiçbir parça grid dışına taşmaz/üst üste binmez. Piece ID'ler benzersiz; aynı shape yinelenebilir. Sabit parça exit üzerinde olmaz.
4. Trigger adedi 0–2, hücreleri birbirinden ve exit'ten farklı, ilk başta **her biri boş**; target veya diğer parçayla çakışmaz.
5. Başlangıç düzeni hipotetik düşüş karşısında **kararlı** (mevcut GDD güvenli geçici ilke). Bu koşulu açık config'e dönüştürmeden kaldırma.
6. Exit başlangıçta target üzerindeyse sıfır-hamle bölüm gibi özel durumu sessizce kabul etme: öğretici tasarım/kalite ilkelerine göre incele.
7. Manuel gravity **yok**; gravity ancak obstacle bırakma trigger'ı ile çalışır. Trigger-aktivasyon dışı düşme varsayan generator yanlış bölüm üretir.

## Durum grafiği ve solver

- `StateKey`: tüm hareketli ve hedef parçaların pozisyonları + tetikleyici kullanılma bitleri + kurallara etki eden diğer durumlar. **MoveCount, render, input ve animation** state key'e dahil edilmez. Aynı şekilli parçaları kanonikleştirmek ancak ID semantiği ve undo/hint eşlemesi korunuyorsa ve eşdeğerlik ispatlanabiliyorsa yapılabilir; aksi takdirde stabil ID ile key oluştur.
- `EnumerateLegalMoves` her oynanabilir piece için dört doğrultuda 1..boşluğun izin verdiği kadar hücrelik tek hamle üretir; mesafe kaç olursa olsun maliyet **1 manuel hamle**. Yalnızca gerçekten yer değiştiren hamleler kenar olur.
- `ApplyMove` tam gameplay transaction'ı uygular: yer değiştirme → son konum trigger'ı → tüketim → tüm board gravity cascade → final board. Ana parça kapıya yol üzerinde ulaşırsa kapıda durur ve win.
- Minimum hamle hesabı için **BFS (uniform cost)**; gerekirse memory-bounded exact search eklenebilir ama ispat olmadan minimum deme.
- `Visited` ile tekrarlayan durumları kes; sonsuz back-and-forth hareket dizilerini **sınırsız çözüm sayısı** olarak sayma.
- Geri dönmeyen kararlarda state graph'ın winning reachable altkümesini reverse reachability ile hesapla; `deadEnd = no path to win`. Görsel olarak sıkışık olması tek başına dead end değildir.
- Bölüm içindeki no-trigger yolunu ayrı bir çözüm aramasıyla doğrula: yol boyunca **hiçbir yeni trigger consumed olmasın**. Her trigger'lı aday için bu **sert kabul kapısı**dır.
- Minimum kazanma yolunu gerçek gameplay core'un üzerinde baştan sona **yeniden oynat**; tutarsızlık varsa kabul etme.

## Zorluk & çeşitlilik analizi

Salt parça sayısına, tahta büyüklüğüne ya da en kısa yola bakarak zorluk sınıfı belirleme. Raporla:

- En kısa manuel hamle ve gerekli parça bağımlılıkları.
- Benzersiz erişilebilir state sayısı; yanlış hamleyle girilen geri dönüşsüz dead end oranı.
- Alternatif **stratejiler** (yalnız gereksiz gidip-gelme ile farklılaşan yolları sayma). Değişken limitlerle örnek yol sayısı raporlanabilir; sonsuz diziyi sayma.
- Tetikleyici/yer çekimi tehdidi: tüketilen trigger, taşınan obstacle sayısı, kümülatif düşüş hücresi ve çözüm etkisi.
- Önceki seviyelerle **başlangıç geometrisi + gerekli hamle stratejisi** benzerliği.
- Kolay → orta → zor → uzman profili; genel olarak artan zorluk, arada rahatlatıcı bölümler. Öğretici başta 5×5, sonra 5×5/6×6 karışık.

Eşikler ve katsayılar **test edilebilir konfigürasyondur**. Kullanıcı kesin puan formülü onaylamadı: nihai tek sayı formülü uydurma; analiz faktörleri ve açıklanabilir öneri üret.

## Kabul hattı / pipeline

```text
Generate candidate(seed, constraints)
    ↓
Validate invariants (hard fail if invalid)
    ↓
Solve exact shortest path + no-trigger path + state graph analysis
    ├─ unproven, timeout, memory cap → INCOMPLETE (NEVER ACCEPT)
    ├─ unsolvable or no no-trigger solution → REJECT
    └─ proven → replay winning paths against shared Core
                  ↓
          compute challenge/risks/diversity/similarity
                  ↓
          quality filters → ACCEPT or REJECT with reasons
                  ↓
          save versioned LevelDefinition + analysis/report
                  ↓
          golden replay regression test
```

Her üretimde `seed`, `generatorVersion`, `rulesVersion`, `solverVersion`, kullanılan limitler ve ret gerekçeleri kaydedilir. Yeniden üretilen seviyelerin tüm oyuncularda aynı olması amaçsa yayınlanan **sabit level dosyası** esas alınır; sadece seed saklamak yeterli değildir.

## Hesaplama güvenliği / doğruluk

- Maksimum state, süre veya bellek bütçesine ulaşınca **INCOMPLETE**, `solvable=true` / `minimumMoveCount` gibi kanıtlı iddialar yok. Daha küçük aday seç veya offline kapsamlı değerlendirmeye gönder.
- Bölüm kalite kapılarını başarı varsayımıyla geçme: `true` yerine açıklanabilir kanıt/yeniden oynatılabilir hareket path'i.
- Generator RNG sabit tohumlu; solver ve gameplay saf deterministik.
- Mobil runtime Hint için mevcut state'den çözüm araması ve gerektiğinde cache; cihazda çözüm bulunamaması "kesin çözümsüz" demek değildir. Exhaustive kanıt olmayan deadlock uyarısı verme.
- Devasa brute force yerine durum anahtarı, bounded/pruned search, reverse win reachability, offline batch ve cache kullan; **pruning exact shortest path kanıtını bozmasın**.
- `same-shape` ID normalize etme, simetrik tahta kanonikleştirme veya symmetry reduction gibi optimizasyonlar yalnız invariant eşdeğerliği test edildiyse etkinleşsin.

## Zorunlu test paketi

1. GDD invariant ihlali içeren her adayın validasyonda reddi; exit/trigger overlap, başlangıç dolu trigger, 9 şekil dışı parça.
2. Hiç hareket olmayan hamle kenar olarak üretilmez; çok hücre kayma **tek adım** maliyetlidir.
3. Solver çözüm path'i gameplay Core ile tekrar oynatıldığında aynı sonuç, kazanma ve kullanılan trigger bayrakları.
4. Tek/çift trigger, cascade, üst üste blokların eş zamanlı düşmesi, trigger'dan kaçınan alternatif çözüm.
5. Aynı seed ve aynı kurallar, aynı candidate/proof/accepted level verilerini üretir; saklanan final levels sürümle sabit.
6. Zaman aşımı/kesik arama "çözümsüz" veya "çözülebilir" diye sahte kesinliğe çevrilmez.
7. Basit el yapımı küçük senaryoda BFS minimum mesafe doğru; path replay; Undo state anahtarına karışmaz.
8. Benzer geometrili veya stratejili iki bölüm, belirlenen eşiklere göre raporlanır/reddedilir.
9. Basit çözülen fakat ciddi tek hamle trap riski olan adayın zorluk sınıfı tek faktöre göre belirlenmez.
10. Hint, ancak kanıtlı mevcut çözüm varsa doğru ID/yön/mesafe döndürür; otherwise hakkı harcamaz.

## Çıktı raporunun biçimi

Her batch için toplam aday, geçerli aday, solver tarafından ispatlanan, no-trigger kanıtı, kabul/ret/eksik analiz sayısı, en kısa çözüm aralığı, zorluk dağılımı, çeşitlilik özeti, çalıştırılan testler ve bilinen limitleri yaz. Kanıtlanmamış adayı "bölüm hazır" diye sunma.

## Kullanıcı onaylı açıklamalar — 23 Ekim 2026
- GDD v1.2 esas alınır. Ana parça sürükleme sırasında kapıya ulaştığı anda kazanır; parmağın kaldırılması beklenmez ve aynı sürükleme ikinci kez sayılmaz.
- Birlikte düşebilen üst/alt hareketli bloklar aynı adımda ilerler; o adımda boşalan hücre kullanılabilir. Hareket etmeyen blokların hücreleri kullanılamaz.
- Undo tahta, tetikleyici ve hamle geçmişini geri yükler; harcanan coin veya kullanılan yardım hakkını geri vermez. Tahta snapshot'ı profil ekonomisinden ayrıdır.
- Hint yalnız güncel durum için doğrulanmış ipucu başarıyla sunulduğunda tüketilir. Başarısız/kesik arama, başarısız sunum veya eskimiş sonuç hak tüketmez.
- Aşama 1 BoardValidator yapısal geçerlilik ve geçici başlangıç kararlılığını denetler; çözülebilirlik, tetikleyicisiz yol veya yayın kabulü kanıtı değildir.
