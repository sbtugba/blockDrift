---
name: block-rift-gameplay
description: Block Rift'in kesinleşmiş oyun mekaniklerini uygularken, hata ayıklarken veya test ederken kullan. 5x5/6x6 grid, parça kaydırma, hedef, tek kullanımlık tetikleyiciler, deterministik dikey cascade, Undo, Hint ve oyun durumuna ilişkin kabul kriterleri.
---

# Block Rift — Gameplay Rules Skill

## Yetki ve kapsam

Oyun için bağlayıcı kaynak `docs/GAME_DESIGN.md`'dir (eski isim Gravity Gate olan aynı oyun). Bu skill uygulanabilir özet ve test sözleşmesidir; kuralları **genişletmez/değiştirmez**. Eski taslaklardaki "her hamlede gravity", parça rotasyonu, dış kapı veya farklı çokgen şekiller geçerli değildir. GDD belirsizliklerini onaylı karar gibi kodlama.

## Sabit kurallar / invariantlar

| Alan | Kesin kural |
|---|---|
| Tahta | **5×5 veya 6×6**, her şey tamsayı hücrede |
| Hedef parça | **Tek 1×1 ana parça**; 4 yön manuel hareket; gravity etkilemez |
| Kapı | Tahta içindeki **herhangi bir tek hücre**; ana parça değince anında kazanır |
| Hareketli parça tipleri | Tam **9** boyut: `1×1`, `2×1`, `1×2`, `1×3`, `3×1`, `2×2`, `3×3`, `1×4`, `4×1` |
| Rotasyon | **Yok**; genişlik/yükseklik korunur; aynı şekil birden çok olabilir |
| Sabit engel | Tek/bağlı gri hücreler; hareket/düşüş yok; kapı üzerinde olamaz |
| Kapı işgali | Hareketli engel kapıdan geçebilir/üzerinde kalabilir; kapı fiziksel duvar değil |
| Sürükleme | Bir hareket **tek eksen**; o eksende ileri/geri önizleme serbest; kapıya ulaşma istisnası dışında yalnız bırakılan son geçerli pozisyon hesaplanır |
| Çarpışma | Tahta sınırı ve tüm diğer parça ayakizleri; tüm ara hücre adımlarında kontrol; ilk engelden önceki son geçerli konum |
| Hamle sayımı | Gerçek manuel konum değişikliği **1 hamle** (mesafeden bağımsız); başlangıca dönüş **0** |
| Trigger | Bölümde **0–2**, kapıdan farklı ve başlangıçta boş; tek kullanımlık, bağımsız, disarm yok |
| Etkinleşme | Yalnız **manuel sürüklenen hareketli engelin** bırakıldığı son pozisyon aktif trigger hücresini örterse; üzerinden geçmek yetmez |
| Gravity | **Yalnız** tetikleyici sonrası; her hareketli engel için sadece **dikey aşağı**, zincirleme ve eş zamanlı hücre adımları |
| Trigger ve düşüş | Düşen blok gravity esnasında yeni trigger çalıştırmaz; aynı hamlede iki aktif trigger kapanırsa ikisi tüketilir, **tek cascade** |
| Hedef ve gravity | Ana parça düşmez/ezilmez; engellerin altında **destek** veya çarpışma engelidir |
| Win | Ana parça kapı yoluna **girdiği anda** işlem biter; sonrasına sürükleme yok |
| Restart | **Ücretsiz**; bölüm başlangıç durumuna döner |
| Undo | Envanter hakkıyla **son manuel hamle öncesi tahta state'i** (trigger consumed ve otomatik düşüş dâhil) geri gelir; coin/yardım hakkı geri verilmez |
| Hint | Hakkı varsa doğrulanmış çözümden **parça + yön + hücre sayısı**; hamleyi otomatik yapmaz; yalnız doğrulanmış güncel ipucu başarıyla sunulunca tüketir |
| Deadlock | Gerçek state'den çözüm yoksa doğrulanmış uyarı; Restart ve varsa Undo seçenekleri; zaman/hamle limiti yok |
| İlerleme | Bölümler sırayla açılır; yıldız engellemez; progress/autosave |
| Ekonomi | Başlangıç **100 coin**; 1/2/3 yıldız **10/20/30**; Undo **50**, Hint **100** coin, ilk parametreler, config'den |

## Transactional move algoritması

```text
Input preview: parça ve eksen seç; yalnız hücrelerde doğrula; ana parça kapıya ulaşırsa bırakmayı beklemeden 1 hamle ve WIN kesinleştir
Pointer release: hedef son geçerli hücreye snap
  ├─ Başlangıç = bitiş → NO_OP (hamle yok, trigger yok, history yok)
  └─ Farklı → PRE_MOVE_STATE history snapshot
      ├─ Hareketi uygula; moveCount += 1
      ├─ Ana parça yol üzerinde kapı hücresine girdi mi?
      │    └─ EVET: kapıda dur → WIN, cascade yok
      ├─ Manuel hareketi yapan MOVABLE obstacle mı?
      │    ├─ HAYIR → trigger yok
      │    └─ EVET → bırakıldığı footprint aktif trigger(lar)ı örter mi?
      │         ├─ HAYIR → cascade yok
      │         └─ EVET → örtülen tüm trigger'ları tüket
      │                     → tüm movable obstacle'lara aşağı gravity cascade
      └─ Son state'i doğrula → çözüm/deadlock ve autosave → sıra oyuncuda
```

**Önemli:** Hamle bitişinde *başka bir engelin* bir tetikleyici üzerinde kalması, bu hamleyi otomatik trigger yapmaz. Yalnızca sürüklenen hareketli engel kontrol edilir. Düşüş süresince yeni trigger aranmaz. `NO_OP` economy, Undo tüketimi ve moveCount'u etkilemez.

## Gravity hesaplama sözleşmesi

1. Grid mantığı sabit coordinate convention ile çalışır (ör. y=0 en alt satır, `Down=(0,-1)`).
2. Cascade başlangıcında hareketli bütün bloklar değerlendirilir; sürüklenen de dâhildir.
3. Her gravity tick'i adayların bir hücre aşağı kayıp kayamayacağını **bütün ayakizleriyle** kontrol eder. Sabit engel, ana parça ve tahta alt sınırı sert engeldir.
4. Birlikte hareket eden üst/alt engellerin eski/yeni footprint çakışması için **deterministik, sıra bağımsız** hareket kümesi hesabı kullan. Aynı tick'te birlikte aşağı kayan bloğun boşalttığı hücre kullanılabilmeli; fakat hareket etmeyen bloğun hücresine giremez. Düşüş kümesini sabit-nokta bağımlılık filtresiyle veya buna matematiksel olarak eşdeğer yöntemle hesapla; sonuçta asla çakışma olmaz. Üst blok alt blokla birlikte düşebilmeli.
5. Aynı tick'te geçerli tüm adayları atomik uygula; hareket kalmayana kadar tekrar et. Parçalar dönmez veya yan kaymaz.
6. Cascade tek bir manuel hamlenin parçasıdır; başkasının hamlesi/gravity re-entry kabul edilmez.
7. Animasyon yalnız sonuç/tick olaylarını oynatır; game state'i tween completion ile hesaplama.

## Yükleme, başlangıç ve tutarlılık

- İlk yerleşim valid, triggers empty; başlangıç board'u hipotetik gravity çalıştırıldığında da kararlı olsun (GDD'deki güvenli geçici kural).
- Trigger bulunan **her bölümde** hiçbir tetikleyiciyi çalıştırmadan kazanılabilecek en az bir yol vardır. Mekaniği 'zorunlu tetik' diye tekrar tasarlama.
- Persist: tüm mevcut parça konumları, tüketilmiş trigger bayrakları, hamle geçmişi, moveCount, aktif bölüm, yıldızlar, coin, envanter. Animasyon yarıda kesilse dahi atomik karar verilmiş state'e yükle.
- Kazanma anında Undo davranışı gibi GDD'de tanımlanmamış sınır durumlarında oyunun temel kuralını bozmayan basit tercih öner; bunu ayrı bir tasarım kararı olarak raporla.

## Açık kararlar: değer uydurma

- 1/2/3 yıldız için kesin minimum hamle toleransları kilitlenmedi: **config/TODO**.
- Undo veya Hint kullanmanın yıldız cezası kesinleşmedi: **cezayı varsayılan kesin kural olarak ekleme**.
- Aynı bölümde yeniden coin farm'ını engelleme zorunluluğu var; 'sadece yeni yıldız rekoru farkını öde' GDD'de **öneri** durumunda, ayrı onay olmadan bağlayıcı kabul etme.
- Görsel stil, renkler, sahne dekoru skill kapsamı dışında.

## Zorunlu kabul testleri

1. Dört yön ve tüm dokuz parça footprint'i; 5×5/6×6 sınırları.
2. Her ara hücreyi kontrol eden çok-hücre drag; engelden önce dur; rotasyon yok.
3. Aynı eksende ileri/geri bırakma; başlangıca dönüş `NO_OP` ve tetik yok.
4. Ana parça yolun ortasında kapıya girdiyse orada dur/instant win; hareketli engel kapıda durabilsin.
5. Ana parça üzerinde trigger bırakınca tetik yok; hareketli engel üzerinden geçip ayrılırsa tetik yok.
6. Manuel bırakılan obstacle tek trigger'ı örterse trigger tüketilir, tek gravity cascade; iki trigger örterse ikisi tüketilir, **tek** cascade.
7. Gravity esnasında düşen obstacle aktif trigger üstüne inse bile trigger çalışmaz.
8. Aynı tick'te birbirine destek olan üst-alt iki movable blok birlikte düşer; hiçbir tick'te overlap yok.
9. Ana parça movable'a destek olur; target düşmez; fixed hiç kıpırdamaz; trigger yoksa havaya kaldırılan blok kendiliğinden düşmez.
10. Undo: graviteden önceki **bütün** parça konumları + trigger'lar + moveCount geri gelir.
11. Save/Load aynı state ve Undo history'yi kurtarır; Restart ücretsiz; hint unsolvable ise hak tüketilmez.
12. Aynı input state + aynı komut byte-for-byte eşdeğer canonical state üretir; EditMode testleri yeşil.

## Tamamlanma raporu

Değiştirilen mantık, kapsanan GDD maddeleri, yeni testler, koşulan test komutları ve sonuçlarını bildir. Test koşulamadıysa nedenini net söyle; 'tüm kurallar çalışıyor' şeklinde kanıtsız rapor verme.

## Kullanıcı onaylı açıklamalar — 23 Ekim 2026
- GDD v1.2 esas alınır. Ana parça sürükleme sırasında kapıya ulaştığı anda kazanır; parmağın kaldırılması beklenmez ve aynı sürükleme ikinci kez sayılmaz.
- Birlikte düşebilen üst/alt hareketli bloklar aynı adımda ilerler; o adımda boşalan hücre kullanılabilir. Hareket etmeyen blokların hücreleri kullanılamaz.
- Undo tahta, tetikleyici ve hamle geçmişini geri yükler; harcanan coin veya kullanılan yardım hakkını geri vermez. Tahta snapshot'ı profil ekonomisinden ayrıdır.
- Hint yalnız güncel durum için doğrulanmış ipucu başarıyla sunulduğunda tüketilir. Başarısız/kesik arama, başarısız sunum veya eskimiş sonuç hak tüketmez.
- Aşama 1 BoardValidator yapısal geçerlilik ve geçici başlangıç kararlılığını denetler; çözülebilirlik, tetikleyicisiz yol veya yayın kabulü kanıtı değildir.

