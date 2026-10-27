# BLOCK RIFT — Oyun Tasarım ve Oynanış Dokümanı

**Sürüm:** 1.2 — 23 Ekim 2026 (kullanıcı onaylı kazanma, eş zamanlı düşüş ve yardım tüketimi açıklamaları)  
**Durum:** Onaylanmış tasarım referansı; Aşama 1 veri modeli ve doğrulama kapsamı  
**Kapsam:** Oyun amacı, oynanış, mekanikler, seviyeler, çözüm doğrulama, ödüller, kayıt ve teknik kabul ölçütleri.  
**Kapsam dışı:** Görsel tasarım, renk, sanat stili, ekran maketleri ve UI yerleşimleri.

> **Önemli:** Bu doküman, görüşmelerde en son onaylanan kararları esas alır. Eski taslaklarda bulunan sürekli yer çekimi, çapraz kayma, L/T/Z/U şekiller ve dışarıdan çıkış gibi fikirler **geçerli değildir**. Karar verilmemiş ayrıntılar ayrı olarak belirtilmiştir; geliştirici veya Codex bunları kendiliğinden kesinleştirmemelidir.

## 1. Oyun fikri ve hedef

**Block Rift**, 5×5 ve 6×6 kareli alanlarda geçen, tek bir **1×1 ana parçayı** oyun alanının **içindeki** çıkış hücresine ulaştırmaya odaklanan bir kaydırmalı bulmaca oyunudur. Oyuncu farklı boyutlardaki engel parçalarını uygun sırada sağa, sola, yukarı ve aşağı kaydırarak hedefe giden yolu açar.

Ayırt edici ek mekanik, **tek kullanımlık yer çekimi tetikleyicileridir**. Normalde parçalar otomatik düşmez. Oyuncu bir hareketli engeli tetikleyicinin üzerinde bırakırsa, hareketli engellerin tümüne yalnızca **dikey aşağı** yer çekimi uygulanır. Ortaya çıkan zincirleme hareketler, oyuncunun açtığı yolu kapatabilir; bu nedenle tetikleyiciler öncelikle **kaçınılması gereken risk** olarak tasarlanır.

Oyun eş nesneleri birleştirme veya blokları dış çerçeveden çıkarma oyunu değildir. **Kazanma koşulu ana parçanın kapı hücresine ulaşmasıdır.**

## 2. Bir bölüm nasıl oynanır?

1. Oyuncu ana parçanın, kapının, hareketli engellerin, sabit gri engellerin ve varsa tetikleyicilerin konumlarını inceler.
2. Herhangi bir hareketli engeli veya ana parçayı seçip yatay **ya da** dikey eksende sürükler.
3. Parça engellere veya tahta sınırına çarpmadan, seçilen eksende boşluğun izin verdiği herhangi bir mesafeye kayabilir. Parçalar dönmez.
4. Oyuncu parmağını bırakana kadar aynı eksende ileri-geri düzeltme yapabilir. **Kapıya ulaşma istisnası dışında sadece bırakıldığı son geçerli konum** kaydedilir. Başlangıç konumuna dönerse hamle oluşmaz.
5. Gerçek bir yer değiştirme **1 hamledir**; birkaç hücre kaymak ekstra hamle sayılmaz.
6. Bırakılan parça bir **hareketli engelse** ve hamle sonunda aktif tetikleyici hücrelerinden en az birini örtüyorsa, ilgili tetikleyiciler tüketilir ve yer çekimi başlar. Sadece üzerinden geçmek tetiklemez.
7. Yer çekimi sırasında aşağı doğru yer değiştirebilen engeller eş zamanlı, hücre hücre düşer. Daha fazla düşüş mümkün kalmayana kadar zincirleme devam eder. Bu otomatik hareketler ek hamle sayılmaz.
8. Oyun son durumu kontrol eder; hedefe ulaşıldıysa bölüm kazanılır. Hedefe ulaşma imkânı kalmadıysa oyuncuya Undo (hakkı varsa) veya ücretsiz yeniden başlatma sunulur.
9. Ana parça kapı hücresine **ulaştığı anda durur** ve bölüm tamamlanır. Tahta dışına çıkmak gerekmez.

### Kısa örnek senaryo

Oyuncu çıkışa uzanan koridoru kapatan 2×1 engeli yana kaydırır. Bu engel hamle sonunda tetikleyici üzerinde kalmadığından yer çekimi çalışmaz. Sonraki hamlede 1×3 engeli kaldırmak için aşağıya çektiğinde bir tetikleyiciyi kapatırsa, tüm hareketli engellerin aşağı düşüşü hesaplanır. Düşen bir blok çıkış koridorunu kapatabilir. Oyuncu bunun yerine başka bir engel sıralaması bularak tetikleyiciden kaçınmaya çalışır. Bu anlatım **oynanış örneğidir**, önceden doğrulanmış bir bölüm veya çözüm değildir.

## 3. Oyun alanı ve nesneler

### 3.1 Oyun alanı

- Kullanılan tahta boyutları: **5×5 ve 6×6**.
- İlk öğretici bölümler 5×5; daha sonraki bölümler zorluğa göre iki boyutu karma kullanır.
- Her hücrenin bir koordinatı vardır. Parçalar aynı hücreyi aynı anda işgal edemez ve tahta dışına taşamaz.
- Kapı, tahtanın dış kenarında ayrı bir geçit değil, **tahta içindeki tek hedef hücresidir**; herhangi bir hücrede olabilir.
- Bir bölümde **0, 1 veya 2** yer çekimi tetikleyicisi bulunabilir.

### 3.2 Ana parça

- Her zaman **1×1** boyutundadır.
- Oyuncu tarafından dört yöne kaydırılır; **yer çekiminden etkilenmez**.
- Hareketli engellere fiziksel destek olabilir; engeller ana parçanın içinden geçemez veya üstüne düşemez.
- Kapı hücresine ulaştığında bölüm hemen tamamlanır.
- Yer çekimi tetikleyicisini **çalıştırmaz**.

### 3.3 Hareketli engeller: kesin şekil kataloğu

Yalnızca aşağıdaki **dokuz dikdörtgen** şekil kullanılabilir:

| Şekil | Hücre sayısı | Şekil | Hücre sayısı |
|---|---:|---|---:|
| 1×1 | 1 | 2×1 | 2 |
| 1×2 | 2 | 1×3 | 3 |
| 3×1 | 3 | 2×2 | 4 |
| 3×3 | 9 | 1×4 | 4 |
| 4×1 | 4 | | |

- `1×3` ve `3×1` farklı, **önceden tanımlanmış** yönelimlerdir.
- **Parça döndürme yoktur.** L, T, Z, U gibi şekiller kullanılmaz.
- Aynı şekilden bir bölümde birden fazla bulunabilir; kesin adet bölümün kalite ve çözülebilirlik analizine bağlıdır.
- Hareketli engellerin tamamı dört yöne manuel kaydırılabilir ve etkin yer çekiminden etkilenir.

### 3.4 Sabit gri engeller

- Hiç hareket etmez, düşmez, otomatik tetikleme yapmaz.
- Tek hücre veya birleşik bloklar hâlinde yerleştirilebilir.
- Hareketli parçaları destekleyebilir, yol kapatabilir, düşüşleri durdurabilir.
- **Kapı hücresinin üzerine yerleştirilemez**; diğer geçerli yerleşimler çözülebilirlik testinden geçer.

### 3.5 Kapı ve özel hücreler

- Kapı bir **hedef konumdur**, fiziksel engel değildir.
- Hareketli engeller kapı hücresinin üzerinde bulunabilir veya üzerinden geçebilir; kapı düşüşü engellemez.
- Sabit engel kapının üzerine yerleştirilemez.
- **Kapı ve tetikleyici aynı hücrede bulunamaz.**

## 4. Manuel hareketin kesin kuralları

1. Bütün hareketli parçalar sağa, sola, yukarı ve aşağı hareket edebilir.
2. Bir parmak sürüklemesinde yalnızca **tek eksen** kullanılabilir. Aynı eksende yön tersine çevrilebilir; dikeyden yataya veya yataydan dikeye geçiş için yeni hamle gerekir.
3. Hareket seçilen eksende, herhangi bir sayıda **tam hücre** mesafesi olabilir.
4. Yol boyunca parça hacmi her ara adımda kontrol edilir; parçalar birbirinin içinden geçemez.
5. Engel veya sınır varsa **son geçerli hücrede** durur.
6. Başlangıçla bitiş konumu aynıysa hamle sayılmaz, tetikleme yapılmaz.
7. Gerçek yer değişimi tek hamledir; hareket boyunca geçilen ara hücreler tek başına tetikleyici çalıştırmaz.
8. Kapıya ulaşma istisnası dışında hamle oyuncu parçayı bıraktığında kesinleşir; sonrasında varsa yer çekimi çözülür.
9. Ana parça, hareket yolu üzerinde kapı hücresine ulaşırsa orada **hemen durur ve kazanır**; sonuna kadar sürüklenmez. Parmağın kaldırılması beklenmez; kazanma anında hareket kesinleşir ve bu sürükleme bir kez sayılır.

## 5. Yer çekimi mekaniği

### 5.1 Genel ilke

**Yer çekimi her hamlede çalışmaz.** Yalnızca özel bir tetikleyicinin başarılı biçimde etkinleşmesiyle çalışır. Bu karar önceki “her hamle sonrası yer çekimi” taslağının yerine geçer.

### 5.2 Tetiklenme koşulları

- Hamle yapan nesne **hareketli engel** olmalıdır; ana parça tetikleyemez.
- Hamle sonunda hareketli engelin kapladığı hücrelerden en az biri **aktif** tetikleyicinin hücresiyle örtüşmelidir.
- Ara konumdan geçmek yeterli değildir.
- Başlangıçta tetikleyici hücrelerinin üzerinde hiçbir parça bulunmaz.
- Her tetikleyici **tek kullanımlıktır** ve oyuncu onu kendi iradesiyle kapatıp etkisizleştiremez; sadece çalıştırmaktan kaçınabilir.
- İki tetikleyici aynı hamlede örtülürse **ikisi de tüketilir**, yer çekimi yalnızca **bir** zincirleme süreç olarak başlar.
- Düşen parçaların tüketilmiş tetikleyiciler üzerine gelmesi yeniden tetiklemez. Cascade sırasında kullanılmamış tetikleyiciler de etkinleşmez.
- Tetikleyicilerden kaçınılarak kazanılabilen **en az bir geçerli çözüm** her bölümde mevcut olmalıdır (tetikleyici içermeyen bölümlerde koşul zaten sağlanır).

### 5.3 Düşüş kuralları

- Yalnızca **hareketli engeller** etkilenir; ana parça ve sabit engeller yerinde kalır.
- Otomatik hareket **yalnızca dikey aşağı** olur. Çapraz veya yatay otomatik boşluk arama yoktur.
- Bir engel ancak mevcut konumundan **bir satır aşağı taşındığında tüm hücreleri** geçerli ve çakışmasız kalıyorsa düşebilir.
- Parçanın bir kısmının altında destek kalması, aşağı taşınacak tam şekil için engelse düşmesini engeller. Devrilme, dönme veya şekil değiştirme olmaz.
- Birlikte düşebilen üst üste hareketli bloklar aynı simülasyon adımında ilerler. Üst blok, o adımda aşağı kayan alt bloğun boşalttığı hücreyi kullanabilir; hareket etmeyen blokların hücreleri kullanılamaz ve sonuçta çakışma oluşamaz.
- Yer çekimi birden fazla parçayı aynı anda etkileyebilir: uygun engeller **hücre hücre eş zamanlı** ilerler; her aşamada çakışma kontrolü yapılır.
- Düşüşler sonlanana kadar **zincirleme** devam eder. Yer çekimi fizik animasyonu değil, ızgara kurallarıyla hesaplanan deterministik bir geçiştir.
- Yer çekimi yeni oyuncu hamlesi başlamadan **tamamen bitmelidir**.
- Oyuncunun o hamlede sürüklediği hareketli engel de tetiklenme sonrasında düşebilir.
- Ana parça diğer engelleri destekler ve düşen engeller tarafından ezilmez; ana parçanın etrafında çıkmaz oluşabilir.

### 5.4 Mekaniğin tasarım amacı

Yer çekimi, oyuncunun işini otomatik kolaylaştıran düzenli bir temizleme sistemi değildir; **yanlış yerde tetiklenmesi çözümü zorlaştırabilecek bir risk** olarak kurgulanır. Bununla birlikte her etkinleşme otomatik kayıp değildir. Tetiklemeyi önceden görebilmek ve doğru hamle sıralamasını planlamak oyunun düşünsel yükünün parçasıdır.

## 6. Hamle işleme sırası — referans akış

```text
Oyuncu sürüklemeye başlar
    ↓
Tek eksende geçerli konumlar önizlenir
    ├─ Ana parça kapıya ulaştı → Bırakmayı beklemeden hareketi kesinleştir, 1 hamle say → WIN
    └─ Ulaşmadı → Önizlemeye devam et
    ↓
Oyuncu bırakır
    ↓
Bitiş = başlangıç mı? ── Evet → İşlem yok, hamle sayılmaz
    │ Hayır
    ↓
Son geçerli hareket uygulanır, 1 hamle sayılır
    ↓
Ana parça kapıya ulaştı mı? ── Evet → Bölüm kazanılır
    │ Hayır
    ↓
Hamle sonunda aktif tetikleyiciyi örten HAREKETLİ ENGEL var mı?
    │ Hayır → Son durumu doğrula → Sıradaki hamle
    │ Evet
    ↓
Örtülen tetikleyicileri tüket
    ↓
Tüm hareketli engeller için eş zamanlı bir hücre aşağı hareket kontrolü
    ↓
Uygun engelleri birlikte düşür, tekrar değerlendir
    ↓
Artık düşebilen engel yok → Tahta sabitlendi
    ↓
Çözülebilirlik / çıkmaz kontrolü → Devam veya kurtarma seçenekleri
```

> **Uygulama notu:** Bu akış kararlaştırılan mantıksal sırayı açıklar. Oyun motoru ve bölüm çözücü **aynı** geçiş işlevini kullanmalıdır; animasyon gerçek oyun durumunun tek kaynağı olmamalıdır. Ortak motor ve katmanlı mimari kullanıcı tarafından mühendislik yaklaşımı olarak onaylanmıştır; ayrı bir oyun mekaniği veya Kural 62 değildir.

## 7. Kazanma, kaybetme ve kurtarma

### 7.1 Kazanma

Ana 1×1 parçanın kapı hücresine girmesi **anında kazanma** anlamına gelir. Başka eşleştirme, kenar geçişi veya dışarı çıkış gerekmez.

### 7.2 Başarısızlık

- **Süre sınırı yoktur.**
- **Hamle sınırı yoktur.**
- Ancak güncel durumdan oyunun kurallarıyla kapıya ulaşan hiçbir çözüm kalmamışsa bölüm **çözümsüz** durumdadır.
- Çözümsüzlük oyuncunun son hamlesinin tahminine göre değil, çözüm algoritması tarafından doğrulanır.
- Oyuncuya uyarı verilir; envanterinde Undo varsa kullanabilir veya **ücretsiz yeniden başlatabilir**.
- Fiziksel olarak çevresi kapanmış bir ana parça mutlaka çözümsüz sayılmaz: hareketli engeller başka yollardan açılabiliyorsa çözüm hâlâ bulunabilir.

### 7.3 Undo

- Undo, kazanılan/satın alınan **envanter hakkıyla** kullanılır; hak yoksa kullanılamaz.
- Tek Undo **son manuel hamle öncesindeki eksiksiz tahta durumuna** döndürür: ana parça, tüm engeller, düşüşlerin sonucu, tetikleyici tüketim durumları ve hamle geçmişi.
- Undo, kullanılmış yardım hakkını (Undo/Hint) veya harcanmış coin'i geri vermez. Profil ekonomisi tahta geçmişinden ayrıdır.
- Tetikleyiciler Undo ile önceki durumuna dönebilir; bu, tek kullanımlı olmalarıyla çelişmez.
- Ücretsiz Restart her zaman mümkündür; envanterdeki haklar Restart nedeniyle silinmez.

### 7.4 Hint

- Envanter hakkıyla açılır.
- Güncel tahta durumundan doğrulanmış çözümün **bir sonraki hamlesini** gösterir: doğru parça, yön ve kaç hücre.
- Hamleyi otomatik yapmaz; oyuncu gerçekleştirir.
- Hint hakkı yalnızca **güncel durum için doğrulanmış ipucu başarıyla sunulduğunda** tüketilir. Arama başarısızsa, kesilirse veya zaman/bellek sınırında tamamlanamazsa hak tüketilmez. Mevcut durum çözümsüzse de **Hint hakkı tüketilmez**. Sonuç sunulmadan tahta değişmişse eski durumun ipucu tüketim gerekçesi olamaz.
- Oyuncu zorlanınca Hint seçeneği kısa süreli vurgulanabilir; oyun durdurulmaz ve ücretsiz Hint verilmez.

## 8. Puanlama, yıldızlar ve oyun ekonomisi

### 8.1 Performans

- Bölüm performansı **minimum çözüm hamlesi** üzerinden değerlendirilir; süre baskısı yoktur.
- **1, 2 veya 3 yıldız** verilir. Kesin yıldız sınırları (minimum hamlenin yüzde/ek hamle toleransı) oyuncu testleriyle ayarlanacaktır.
- 1 yıldız ile tamamlamak bile **sonraki bölümü açmaya yeter**.

### 8.2 Coin ekonomisi — başlangıç konfigürasyonu

| Kazanım / harcama | Coin |
|---|---:|
| 1 yıldızlı tamamlama ödülü | +10 |
| 2 yıldızlı tamamlama ödülü | +20 |
| 3 yıldızlı tamamlama ödülü | +30 |
| 1 Undo hakkı satın alma | −50 |
| 1 Hint hakkı satın alma | −100 |
| Yeni oyuncu başlangıç hediyesi | +100 |

- Coin ödülleri ve yardım fiyatları **yapılandırılabilir**; belirtilen rakamlar ilk test değerleridir.
- Oyuncu coin ile Undo/Hint **satın alır, envanterine ekler, dilediği bölümde kullanır**.
- Aynı bölüm sınırsız farm edilememelidir: önceki en yüksek yıldız ödülüyle yeni ödül arasındaki **pozitif fark** kadar coin verilir (önerilmiş ekonomik koruma; konuşmada ayrıca A/B onayı alınmamıştır, uygulamadan önce nihai kabul kriterine işlenmelidir).
- Hint/Undo kullanımının yıldızlara ilave ceza getirip getirmeyeceği **kesinleşmemiştir**. Önceki öneri cezalandırmamaktı.

## 9. İlerleme ve kayıt sistemi

- Bölümler **sıralı** açılır.
- İlk öğretici bölümler 5×5, devamında 5×5 ile 6×6 zorluğa göre karışık kullanılabilir.
- Bütün oyuncular için **önceden üretilmiş, doğrulanmış, saklanmış aynı bölümler** kullanılır.
- Oyuncu uygulamayı kapatırsa, açık bölümün bütün parça/tetikleyici konumları, hamle sayısı, Undo geçmişi, bölüm ilerlemesi, yıldızlar, coin ve envanter **kaydedilir**.
- Tekrar açılışta aynı tutarlı durumdan devam edilir. Yarıda kalmış animasyon, oyun durumunu değiştirmez.

## 10. Bölüm üretimi ve kalite doğrulama

### 10.1 Üretim prensibi

Algoritma sadece izinli dokuz hareketli parça tipini, 5×5 / 6×6 tahtaları, sabit engelleri, hedefi ve 0–2 tetikleyiciyi kullanır. Üretilen aday bölüm **önce çözülür ve analiz edilir**, sonrasında kalite sınırlarını karşılıyorsa yayınlanabilir bölümler havuzuna alınır.

### 10.2 Her bölüm için zorunlu analiz başlıkları

1. **Geçerlilik:** Taşma, hücre çakışması, yasak şekil, sabit engel-kapı veya kapı-tetikleyici çakışması var mı?
2. **Başlangıç:** Tetikleyicilerin üstü boş mu, başlangıç yerleşimi yer çekimi kararlılığı kuralıyla tutarlı mı?
3. **Çözülebilirlik:** En az bir gerçek kazanma dizisi bulunuyor mu?
4. **En kısa çözüm:** Minimum manuel hamle sayısı kaç?
5. **Çözüm çeşitliliği:** Benzersiz çözüm yolları ve anlamlı stratejik alternatifler neler? Sayı tek başına kabul/ret nedeni değil.
6. **Durum uzayı:** Kaç benzersiz erişilebilir tahta durumu var?
7. **Çıkmazlar:** Kaç durumda kapıya ulaşan yol kalmıyor?
8. **Geri dönüşlülük:** Hangi hatalı durumlardan normal hamlelerle kurtulunabiliyor; hangileri Undo/Restart gerektiriyor?
9. **Tetikleyici analizi:** Tetikleyicisiz en az bir çözüm var mı; tetiklenirse durum nasıl değişiyor?
10. **Yer çekimi analizi:** Düşen parça sayısı, toplam düşüş adımı, zincir uzunluğu, strateji üzerindeki etki ne?
11. **Çok faktörlü zorluk:** Minimum hamle, bağımlılıklar, çıkmaz riski, yer çekimi ve stratejik kalite birlikte nasıl puanlanıyor?
12. **Benzerlik:** Başlangıç dizilişi ve çözüm stratejisi başka kabul edilmiş bölümlere aşırı benziyor mu?
13. **Sonuç:** Kabul / ret ve açık gerekçesi.

### 10.3 Teknik kavramların net tanımları

- **Tahta durumu:** Her parçanın koordinatı, kullanılan tetikleyici bayrakları ve gerekli diğer oyun durumu bilgileri. Eşdeğerliğin nasıl normalize edileceği tanımlanmalı.
- **Durum grafiği:** Düğümler benzersiz tahta durumları, kenarlar geçerli **bir manuel hamle** ve onun bütün otomatik sonuçlarıdır.
- **Çözüm:** Başlangıç durumundan kazanma durumuna ulaşan geçerli hamle dizisidir.
- **Minimum hamle:** Durum grafiğinde başlangıçtan herhangi bir kazanma durumuna en kısa kenar sayısıdır.
- **Çıkmaz:** İlgili durumdan kazanma düğümüne ulaşamayan durumdur.
- **Geri dönüşsüz hata:** Bir hamlenin ulaştırdığı durumdan **normal hamlelerle** kazanan duruma ulaşılamaması; Undo/Restart başka bir kurtarma yoludur.
- **Çözüm sayımı:** Döngüler sonsuz dizi doğurabileceğinden tüm olası hamle dizilerinin sınırsız sayılması yapılmaz. Basit yol veya belirli hamle sınırına kadar çözüm sayımı gibi **tanımlı ve raporlanabilir** ölçütler kullanılmalıdır.
- **Anlamlı alternatif:** Başka çözümden yalnızca gereksiz gidip-gelme adımlarıyla ayrışmayan, stratejik olarak farklı çözüm.

### 10.4 Zorluk profili

| Seviye | Hedeflenen davranış |
|---|---|
| Kolay | Öğretici; hatalar çoğunlukla normal hareketlerle düzeltilebilir |
| Orta | Daha fazla parça bağımlılığı ve planlama |
| Zor | Tetikleyici riski ve geri dönüşsüz çıkmazlar belirgin olabilir |
| Uzman | Yüksek stratejik bağımlılık; daha fazla geri dönüşsüz hata riski |

- Genel zorluk **yükselir**, ancak zor bölümlerin ardından rahatlatıcı bölümler gelebilir.
- Bir bölümün büyük tahtada veya çok parçalı olması tek başına zor olduğu anlamına gelmez.
- Bir bölümün çok fazla çözümünün bulunması tek başına reddetme sebebi değildir.
- Kolay bölümlerde geri dönüşsüz cezalandırıcı durumlar az tutulur; ileri düzeyde artabilir.
- **Özel tehlike kuralı:** Tetikleyici bulunan her bölüm **tetiklenmeden** de çözülebilmelidir.
- Her aday, zorluğu düşük olması dışında, stratejik anlam ve çeşitlilik yönünden de incelenir.

### 10.5 Saklanan bölüm paketi (önerilen teknik veri)

```text
LevelDefinition
  id, version, gridWidth, gridHeight
  mainPieceStart, exitCell
  movablePieces: [id, shapeType, startCell]
  fixedCells: [...]
  gravityTriggers: [cell, initiallyUnused]

LevelAnalysis
  solverVersion, rulesVersion
  isSolvable, minimumMoveCount
  exampleOptimalSolution
  reachableStateCount, deadEndStateCount
  reversibleFailureMetrics
  gravityRiskMetrics, noTriggerSolutionExists
  strategicDiversity, similarityMetrics
  difficultyScore, difficultyClass
  accepted, rejectionReasons
```

Bu veri şeması **uygulama önerisidir**, kesin dosya formatı değildir.

## 11. Teknik uygulama ve test ilkeleri

- Hareket, tetikleme, düşüş ve kazanma hesapları, ekran animasyonundan bağımsız **tek, deterministik kural motoru** üzerinden uygulanmalıdır.
- Bölüm üretici, çözücü ve Hint sistemi oyunun kullandığı **aynı geçiş kurallarını** kullanmalıdır (onaylanmış mühendislik yaklaşımı).
- Testler en az şu durumları kapsamalıdır: dört yön; eksen kilidi; çok hücreli sürükleme; ara hücre çarpışmaları; aynı eksende geri dönme; hareket etmeyen hamle; kapıdan geçerken anında kazanma; kapı üzerinde hareketli engel; tetikleyiciden geçip üzerinde bitirmeme; iki tetikleyiciyi birlikte örtme; tetikleyici tüketimi; Undo ile geri yükleme; ana parça desteği; zincirleme/eş zamanlı düşme; tekrar yüklenen kayıt; çözümsüzlük ve Hint davranışı.
- Algoritmanın ürettiği **çözüm hamleleri**, gerçek oyun motorunda baştan sona yeniden yürütülerek doğrulanmalıdır.
- Aynı başlangıç durumu ve aynı hamle dizisi **aynı sonuç** üretmelidir.

## 12. Önceki taslaktan değiştirilen veya kaldırılan fikirler

| Eski taslak / öneri | Güncel karar |
|---|---|
| Her hamleden sonra yer çekimi | **Kaldırıldı**; yalnızca tek kullanımlık tetikleyici etkinleşince |
| Otomatik sağa/sola veya çapraz kayma | **Kaldırıldı**; yalnızca dikey aşağı düşme |
| L, T, U, Z ve serbest şekil üretimi | **Kaldırıldı**; kesin dokuz dikdörtgen şekil |
| Ana parçanın değişken boyutu | **Kaldırıldı**; her zaman 1×1 |
| Ana parçanın yer çekimiyle düşmesi | **Kaldırıldı**; düşmez |
| Kapının tahta dışında veya yalnızca üstte olması | **Kaldırıldı**; oyun alanının herhangi bir iç hücresinde |
| Otomatik dinamik oyuncu bölümleri | **Ana mod için kaldırıldı**; önceden doğrulanmış sabit bölümler |
| Hamle/süre limitli kayıp | **Kaldırıldı**; sadece çözümsüzlük |
| Geri almanın sınırsız ve ücretsiz olması | **Kaldırıldı**; envanter hakkı gerekli |

## 13. Kodlamadan önce yalnızca kritik açık noktalar

Bunlar **onaylanmış kurallara ek yeni mekanikler değildir**; belirsiz durumları yanlış varsaymamak için kaydedilmiştir.

1. **Başlangıç kararlılığı:** Önce “bölüm başında hiçbir parçanın düşebileceği boşluğu bulunmasın” kabul edildi. Yer çekimi daha sonra tetikleyiciye bağlandığı için başlangıçta desteklenmeyen parçaların da havada bulunabilmesine izin verilip verilmeyeceği yeniden karara bağlanmadı. **Güvenli geçici ilke:** Eski onay korunsun; bölümler yer çekimi bakımından kararlı başlasın. Bunu değiştirmeyin.
2. **Yıldız eşikleri:** 1/2/3 yıldız için kesin minimum-hamle toleransı belirlenmedi; yapılandırılabilir olmalı.
3. **Tekrar ödül koruması:** Önceki öneri yalnızca iyileştirilen yıldız rekorunun coin farkını vermekti; kesin ayrı onay kaydı yok. Ekonomi sömürüsünü engelleme ihtiyacı kesin, yöntemi teknik kabul aşamasında belirlenmeli.
4. **Undo / Hint yıldız etkisi:** Ek yıldız cezası kesin kararlaştırılmadı; yokmuş gibi kodlamaya kilitlemeyin.
5. **Durum uzayı maliyeti:** Tüm durumların eksiksiz incelenmesi aday bazı bölümlerde pahalı olabilir. Analiz tamamlanmadan veya hesaplama sınırına takıldığında bölümü **kanıtlanmış gibi kabul etmeyin**; ret/inceleme durumuna alın.
6. **Yer çekimi sırasında eş zamanlılık — karara bağlandı:** Birlikte düşebilen üst ve alt blok aynı adımda hareket eder; boşalan hücre kullanılabilir (§5.3). Deterministik ve çakışmasız algoritmanın uygulanması/test edilmesi teknik görevdir.
7. **Tetikleyiciye dokunulmadan çözüm:** Bu gereklilik, yer çekimi **oyunun ana farkı** olmasına rağmen tetikleyicinin bazı bölümlerde hiç kullanılmadan geçilebilmesi anlamına gelir. Karar kasıtlıdır; değiştirmeyin.

## 14. Önerilen geliştirme sırası (karar değil, plan)

1. Dokümanı mevcut oyun kuralları için tek kaynak ilan et.
2. 5×5 ızgara, ana parça, kapı, dokuz şekil ve dört yönlü manuel kaydırma çekirdeğini kur.
3. Çarpışma, hareket sayımı, kazanma ve sabit engelleri test et.
4. Tetikleyici durum makinesini ve tek kullanımlık etkinleşmeyi ekle.
5. Eş zamanlı dikey düşüş ve zincirleme hesaplamasını bağımsız testlerle doğrula.
6. Undo / Restart ve tam durum kaydını ekle.
7. Aynı motoru kullanan durum uzayı çözücüsünü kur; Hint ile çözümsüzlük uyarısını bağla.
8. Önce örnek doğrulanmış bölümler üzerinde kalite raporu üret; ardından algoritmik bölüm üreticisini oluştur.
9. Zorluk sıralaması, çeşitlilik filtresi, yıldız/coin/enventeri ekle.
10. Oyun hissi ve ödül dengelerini test ederek sayısal eşikleri ayarla.

---

**Tek cümlelik oyun tanımı:** Block Rift, oyuncunun 5×5 veya 6×6 tahtada engelleri dört yöne kaydırarak 1×1 ana parçayı içerideki kapıya ulaştırdığı; tek kullanımlık tetikleyicilerin bütün hareketli engelleri aşağı düşürerek yanlış hamleleri stratejik bir tehlikeye dönüştürdüğü, çözülebilirliği kanıtlanan bölümlerden oluşan bir mobil bulmaca oyunudur.



