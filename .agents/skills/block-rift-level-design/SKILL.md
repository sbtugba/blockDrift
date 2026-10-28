---
name: block-rift-level-design
description: Block Rift için yeni bölüm tasarlarken veya mevcut bölümlerin bulmaca kalitesini, oyun alanı kullanımını, hamle bağımlılıklarını, öğreticiliğini, zorluk dengesini ve çeşitliliğini değerlendirirken uygula. Teknik çözülebilirlik ve üretim algoritması için mevcut block-rift-level-generator skill'ini kullan.
---

# Block Rift — Level Design & Puzzle Quality Skill

## 1. Görev ve kaynak önceliği

Bu skill'in amacı, teknik olarak çözülebilen seviyelerin **iyi bulmacalar** olmasını sağlamaktır. Kod ve mekanik otoritesi değildir.

Her görevde, ilgiliyse şu dosyaları incele:

1. `AGENTS.md`
2. `docs/GAME_DESIGN.md` — onaylı kuralların tek kaynağı
3. `.agents/skills/block-rift-gameplay/SKILL.md` — hareket/çarpışma/trigger kuralları
4. `.agents/skills/block-rift-level-generator/SKILL.md` — offline üretim, Solver, ispat ve kabul sözleşmesi
5. İlgili eski spec/level analizleri; aynı tasarım sorununu tekrar çözme

**Sorumluluk ayrımı:** Bu skill bölümün neden iyi veya zayıf olduğunu değerlendirir. Solver yazmaz, Core kurallarını yeniden tanımlamaz, Level Generator'ı kopyalamaz. GDD ile çelişirse GDD geçerlidir. Kod değişikliği gerekiyorsa mimari ve ilgili skill'lere göre ayrı uygulanır.

## 2. Temel tasarım standardı

> Block Rift bir bulmaca oyunudur; blok kaydırma demosu değildir. Her bölüm, "Çok Kolay" olsa bile oyuncuya anlaşılır ama gerçek bir problem çözdürmelidir.

- Zorluk **blok sayısına, dolu hücre oranına, tahta boyutuna ya da yalnızca minimum hamleye** indirgenmez.
- `5×5` ve `6×6` zorluk etiketi değildir; ikisi de beş zorluk kategorisinde kullanılabilir.
- Blok adedi önceden dayatılmaz. Tasarım ve bağımlılıkların gerektirdiği sayı seçilir.
- Fazla boşluk otomatik kusur değildir; **işlevsiz geniş alan** kusurdur. Dolu tahta da otomatik kalite değildir.
- Boş hücreler hareket, yer açma, geçiş veya alternatif konumlandırma için anlamlı kullanılmalıdır.
- Oyuncu, ana parçayı çıkışa götürmek için en az bir anlamlı engeli çözmeli; sadece dekoratif engel serpiştirilmemelidir.
- Aynı şeklin ve rengin tekrarına izin verilir; renkler mekanikleri veya hamle kurallarını belirlemez.
- Onaylı mekaniklere aykırı kural ekleme: manuel yönler şekle bağlıdır (`2×1/3×1/4×1` yatay; `1×2/1×3/1×4` dikey; `1×1/2×2` dört yön); tek sürükleme tek izinli eksendir, rotasyon yoktur. `3×3` geçersizdir. Hedef tahta içindedir.

## 3. Beş zorluk seviyesi — tasarım kılavuzu

Bunlar **sezgisel tasarım hedefleridir, sert hamle/blok eşikleri değildir**:

| Seviye | Beklenen deneyim |
|---|---|
| Çok Kolay | Tek bakışta anlaşılır hedef; kısa ama gerçek yol açma veya sıralama kararı; düşük ceza/çıkmaz riski |
| Kolay | Birkaç bağlantılı engel; basit alan açma, öngörülebilir geri bildirim |
| Orta | Birden fazla bağımlılık ve geçici konumlandırma; uygun hamle sırasını planlama |
| Zor | Dar alanlar, alternatiflerin karşılaştırılması, yanlış sıralamanın maliyeti |
| Çok Zor | Çok aşamalı bağımlılıklar, stratejik alan yönetimi, derin planlama ve olası çıkmazlar |

Zorluk atamasında birlikte değerlendir: ispatlanmış en kısa çözüm, zorunlu parça etkileşimleri, bağımlılık derinliği, kritik hamleler, geri alınamaz durumlar, alternatif çözüm genişliği, çözüm sırasında gerekli yeniden konumlandırmalar ve tetikleyici riskleri. Ölçümlerin hiçbiri tek başına etiket belirlemez. **5×5 veya 6×6 olmasına doğrudan puan verme.** Zorluk etiketlerini oyuncu testleriyle kalibre et; test olmadan 'objektif olarak zor' iddia etme.

## 4. Tahta kullanım kalitesi

Tahtanın tamamını doldurmak veya her hücreyi kullandırmak hedef değildir. Değerlendirme şu sorular üzerinden yapılır:

1. Parçalar tek köşeye veya yalnız alt iki sıraya yığılmış mı? Öyleyse tasarım açısından gerekçesi var mı?
2. Çözüm sırasında farklı satır/sütun veya bölgeler **işlevsel olarak** kullanılıyor mu?
3. Boş alanlar manevra, bekleme, yol açma veya yeniden konumlandırma sağlıyor mu?
4. Dolu hücrelerden hangileri gerçek kısıt oluşturuyor, hangileri sadece süs?
5. Bir bölge tamamen çıkarılsa aynı bulmaca kalır mı? Kalıyorsa düzeni sadeleştirmek veya yeniden tasarlamak gerekir mi?
6. İlk bakışta yıldız, hedef ve temel engel okunabilir mi?

İncelenecek ölçüler (mevcut analiz altyapısına uygunsa): başlangıç doluluk oranı, çözümde değişen hücreler, **farklı satır/sütun kullanımı**, taşların dolaştığı bölgeler, hareket alanı yaratma olayları, işlevsiz parçalar. Bunlar **kalite göstergesi**, tek başına kabul eşiği değildir. Sırf tüm satırlar kullanılsın diye yapay hareket ekleme.

## 5. Blok bağımlılığı ve 'Aha' anı

Güçlü bulmacalar için aday tasarımlarda şunları araştır:

- **Kapı kesme:** Ana parçanın doğrudan yolu en az bir gerçek kısıtla kapalıdır.
- **Alan açma:** A bloğu B'nin hareket edebilmesi için yer açar.
- **Sıra bağımlılığı:** B'yi erken hareket ettirmek yolu kapatabilir; A→B sıralaması anlamlıdır.
- **Geçici park:** Bir parça son konumuna değil, başka parçayı geçirmek için geçici konuma taşınır.
- **Alternatifli karar:** Oyuncu birden fazla olası yaklaşım görür, ancak bunlar aynı ölçüde verimli olmayabilir.

Her bölümün **her** örüntüyü içermesi zorunlu değildir. Çok Kolay bölümde az sayıda açık ilişki; zor seviyelerde daha yoğun ilişkiler uygundur. Görsel çeşitlilik uğruna gerçek çözüm yoluyla ilgisiz taş ekleme.

## 6. Tasarım ve doğrulama iş akışı

1. **Kısıtları oku:** GDD, mevcut Core, hazır 5×5/6×6 görsel tahta, kullanılabilir prefabs ve ilgili geçmiş spec.
2. **Hedef deneyimi tanımla:** Zorluk seviyesi, öğretilecek karar, varsa trigger. Kullanıcının belirlemediği blok sayısını kendin katı bir eşik yapma.
3. **Aday düzen oluştur:** Manuel tasarım veya mevcut offline generator. Sadece fiziksel doluluğa göre optimize etme.
4. **Teknik kapı:** Mevcut Solver/Core ile doğrula; çözüm, minimum hamle, yol replay, çarpışma, kazanma ve varsa trigger'sız kazanan yol kontrolü. `INCOMPLETE` kanıt değildir.
5. **Kalite kapısı:** Bağımlılıkların gerçekliği, alanın işlevsel kullanımı, bypass/alternatif çözümler, dekoratif parçalar ve öğreticilik. Yalnız bir çözümü gözlemleyip tüm çözümler için zorunluluk iddia etme.
6. **İyileştir:** Zayıf düzeni gerekçesiyle reddet; tekrar doğrula. Blok sayısını şişirme veya rastgele manevra zorlaması yapma.
7. **Sürümle:** Onaylanan bölümü offline, sabit veri olarak sakla. Runtime rastgele üretim yok.
8. **Raporla:** Teknik ispat ile insani tasarım değerlendirmesini ayrı başlıklarda sun.

**Önemli:** Eğer Solver/Generator henüz mevcut değilse eksikliği açıkça belirt. Görsel sezgiye dayanarak 'minimum N hamle', 'tüm çözümlerde zorunlu' veya 'kanıtlandı' deme. Kod yazma işi ayrı kapsamdır ve ilgili skill'lerle TDD gerektirir.

## 7. Alternatif çözümler ve anlamsız bypass

- Birden fazla geçerli çözüm kabul edilir; matematiksel teklik şart değildir.
- Çok kısa bir alternatif, amaçlanan bağımlılığı atlıyorsa bölümü yeniden değerlendir.
- En kısa yolların incelenmesi **tüm kazanma yollarının** aynı parçalara ihtiyaç duyduğunu ispatlamaz.
- 'Bu taş zorunlu' gibi kesin ifadeler ancak ilgili kısıtı test eden tamamlanmış durum-uzayı analizi veya eşdeğer kanıtla kullanılır.
- İşlevsiz parça analizi için aday parça çıkarıldığında teknik çözüm ve tasarım etkisini yeniden değerlendir; yeni düzeni tekrar Solver ile kontrol et.

## 8. İlk bölüm (Level 1) için özel rehber

Level 1 öğreticidir ama otomatik kazandıran bir düz koridor olmamalıdır:

- O an **hazır ve kullanılabilir** board varyantını kullan; görseli olmayan 5×5'i varmış gibi davranma.
- İlk hedef: kaydırma, yolu açma, yıldızı tahta içi hedefe taşıma.
- Karmaşık gravity/trigger mekaniklerini tanıtmak gerekmiyorsa kullanma.
- Başlangıç yerleşiminde farklı bölgeler anlamlı kullanılabilir; özellikle 6×6'nın çoğunu tamamen işlevsiz bırakmaktan kaçın.
- Hareketli ve sabit engellerin sayısı **tasarım sonucudur**; ne 'en az dört blok' ne de 'en fazla üç blok' gibi evrensel kural koyma.
- Hamle uzunluğu kanıtlanan çözümün sonucudur; yapay 4–6 veya 5–8 eşikleri koyma.
- Oyuncu bir engeli, bir geçişi ya da kısa bir neden-sonuç zincirini fark etsin.
- Referans ekran resmi **tasarım ipucu** olabilir, fakat hücre koordinatlarını ve geçerliliği gerçek bölüm verisi belirler.

## 9. Üretim kalite kapıları

### Teknik kapı — mevcut Solver/Generator sorumluluğu

- Geçerli LevelDefinition ve parça yerleşimi
- Kazanma yolunun Core üzerinde replay edilmesi
- En kısa yol iddiası için **tamamlanmış** uygun arama
- Tetikleyici varsa en az bir **tetikleyici kullanmadan kazanan çözüm** bulunması
- Kanıt yoksa `INCOMPLETE` veya `UNVERIFIED`, kabul yok

### Tasarım kapısı — bu skill'in sorumluluğu

- Gerçek engel ve amaçlanan karar var mı?
- En az bir işlevsel bağımlılık/alan açma var mı? (çok basit öğretici istisnası gerekçelendirilmeli)
- Oyun alanı anlamlı mı, yoksa tek köşe dışında ölü alan mı?
- Çözümlerin kısayolları tasarımı geçersiz kılıyor mu?
- Her dekoratif veya etkisiz görünen parça sorgulandı mı?
- Öğreticilik, okunabilirlik, algılanan adalet uygun mu?
- Yakın seviyelerle mekanik/strateji benzerliği gereksiz tekrara yol açıyor mu?

Tasarım kapısı **gerekçeli uzman değerlendirmesidir**, insan oyuncu verisi olmadan bilimsel zorluk kanıtı değildir. Yalnızca yüksek bir sayısal skora bakarak otomatik onay verme.

## 10. Standart bölüm inceleme raporu

Her yeni veya revize edilmiş bölüm için kısa ama somut rapor üret:

```text
Bölüm: <ID> | Tahta: <5x5/6x6> | Hedef zorluk: <etiket>
Koordinat: (x,y), origin ve yönler açıkça tanımlanmış
Başlangıç: yıldız, çıkış, taşlar ve hareketli parçalar (ID/shape/konum)
Teknik: solver sonucu, tamamlanma durumu, minimum hamle (kanıtlıysa), çözüm replay
Bulmaca fikri: oyuncunun çözmesi gereken problem
Bağımlılıklar: parça A hangi parça B'nin hangi hareketini açıyor?
Alan kullanımı: çözümde işlevsel olarak kullanılan bölgeler ve boşluklar
Alternatifler/bypass: doğrulanan veya henüz doğrulanmayan durum
Zorluk gerekçesi: hamle sayısı dışındaki faktörler; oyuncu testi eksikliği
Çeşitlilik: komşu seviyelere göre tekrarlanan veya yeni fikirler
Karar: ACCEPT / REVISE / REJECT / INCOMPLETE
```

## 11. Anti-pattern'ler

- "6×6 ise Zor" veya "5×5 ise Kolay" yaklaşımı
- "Daha çok blok = daha zor" varsayımı
- Tahtanın çoğu kullanılmazken sırf görüntü için blok serpiştirmek
- Tek engeli kaydırıp direkt hedefe varılan bütün giriş seviyelerini yeterli bulmak
- Sahte minimum hamle, test edilmemiş çözülebilirlik veya Solver raporu
- Sadece bir optimal yolu analiz ederek bütün çözümler hakkında konuşmak
- Üreticide farklı adlarla aynı mantıksal seviyeyi tekrar tekrar kabul etmek
- Tasarım standardını uygulamak için kamera, board modeli veya GDD mekaniklerini izinsiz değiştirmek

## 12. Kabul tanımı

**Done** için: (a) mevcut kurallara uygun ve teknik olarak doğrulanmış bölüm, (b) amaçlanan zorluk için gerekçelendirilmiş bulmaca kalitesi, (c) anlamlı tahta/boşluk kullanımı, (d) bilinen bypass ve eksikliklerin dürüst raporu, (e) korunmuş Core/scene/asset mimarisi. Test edilmemiş iddiayı kanıt gibi yazma.

## 2026-10-27 — Şekle bağlı bulmaca tasarımı

### Şekle bağlı manuel hareket yönleri (2026-10-27)

**Yönelim başlangıçta sabittir; oyuncu blok döndüremez.** Şekiller `genişlik×yükseklik` olarak tanımlanır.

| Hareketli engel | İzin verilen manuel yönler |
|---|---|
| `1×1` | Sol, sağ, yukarı, aşağı |
| `2×1`, `3×1`, `4×1` | **Yalnızca sol ve sağ** |
| `1×2`, `1×3`, `1×4` | **Yalnızca yukarı ve aşağı** |
| `2×2` | Sol, sağ, yukarı, aşağı |

Ana yıldızlı `1×1` hedef parça dört yöne kayabilir; yer çekiminden etkilenmez. `3×3` **katalogdan çıkarılmıştır**, yeni bölümde, level validator'da ve solver hamlelerinde geçersizdir.

**Bu hareket sınırları yalnızca oyuncunun manuel sürüklemesi içindir.** Tetikleyiciyle oluşan otomatik yer çekiminde tüm hareketli engeller, yatay şekiller dâhil, mevcut çarpışma/cascade kurallarıyla **dikey aşağı** düşebilir. Manuel dikey yasağı otomatik düşüşü engellemez. Mevcut çıkış, Undo, kazanma ve tetikleyici kuralları değişmez.

- Bağımlılık tasarımında yön kısıtlarını kullan; yönleri yalnızca zor görünmek için keyfî değiştirme.
- `2×2` ve `1×1` dört yönlü manevra parçalarıdır; yatay/dikey uzun bloklar kendi eksenlerindeki boşluğu gerektirir.
- Şekillerin yön sınırlaması kısa yolları azaltabilir; her adayda solver ile minimum çözümü ve bağımlılıkları yeniden değerlendir.
- Şekil, blok sayısı, doluluk oranı ve tahta boyutu tek başına zorluk puanı değildir.
