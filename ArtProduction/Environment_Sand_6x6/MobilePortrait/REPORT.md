# Block Rift — 1080×1920 portre kum revizyonu

Durum: Blender üretimi ve belirtilen sayısal kontroller tamamlandı; kullanıcı görsel onayı bekleniyor. Unity export/import yapılmadı.

## Kadraj ve mevcut Unity kamera referansı

Assets/Scenes/BlockRift_Gameplay.unity salt okunur incelendi. Main Camera: quaternion x=0.62932044, w=0.7771459 (78° eğim), local position (0,30.544376,-6.49963), perspektif ve dikey FOV=22°. GameplayViewportLayout.cs, FitPerspective bölümü UI için off-axis projection kullanır. Blender önizlemesinde aynı eğim ve dikey FOV kullanıldı; Unity scripti, kamera veya UI nesnesi değiştirilmedi. Off-axis UI sistemi Blender içinde yeniden geliştirilmedi.

Kameralar yalnızca yeni Review_MobileSand_6 ve Review_MobileSand_5 sahnelerindedir. Her iki sahnede tam üstten ortografik ve Unity açısına yakın perspektif 1080×1920 PNG hazırdır. UI öğesi üretilmedi; yer ayrıldı.

| Varyant | X ekran zarfı | Y ekran zarfı | Üst boşluk | Alt boşluk |
|---|---|---|---|
| 6×6 | 0.0550 … 0.9450 | 0.2541 … 0.7350 | 508.8px | 487.9px |
| 5×5 | 0.0556 … 0.9444 | 0.2543 … 0.7346 | 509.5px | 488.2px |

Ekran zarfı mevcut hücre vertexlerinin perspektif projeksiyonundan ölçülür. Sol/sağ yaklaşık eşit ve tahta yatayda ortalıdır. Üst ve alt boşluk değerleri hücre sınırına kadardır; nihai Unity UI yerleşimi bu görevde test edilmedi.

## Siyah alan: kesin neden ve düzeltme

Eski 1080×1920 renderın sol üstündeki siyah bölgeyi ayrı kamera/ışık kontrolü ve dünya ışın testiyle inceledik. O noktada kum mesh vardı; ışıkları kamera dışına taşımak tek başına siyah alanı kaldırmadı. Eski Color_BrokenSoil haritasında 125.649 siyah RGB piksel, normal haritasında 126.297 siyah piksel saptandı.

Eski üretim kodu yeniden hesaplandı: çatlak taper hesabındaki sin(pi*u) değeri uç noktalarda yuvarlama nedeniyle çok küçük negatif olabiliyordu. Kesirli üs bu değerlerde NaN üretti; 125.649 geçersiz shade pikseli dosyada siyaha dönüştü. Bu geçersiz piksel maskesi ile eski siyah albedo maskesi **%100 örtüşüyor**. Bulgular invalid_value_reproduction.json ve black_area_diagnosis.json içindedir.

Yeni üreticide u ve sinüs çıktısı güvenli aralığa clamp edilir, sonlu değer assertleri çalışır. Yeni haritalarda siyah blok pikseli yoktur. Her yeni tam ekran çıktıda siyah bölge ve alpha boşluğu piksel kontrolü 0; ayrıca her kamera kadrajında 209 ışın örneğiyle sahne kapsaması 0 kaçırma verir. Yeni kum dış alanı portre kadrajını rahatça örtecek şekilde Y yönünde genişletildi; stüdyo ışıkları kamera dışına alındı. Bu iki önlem kapsama/önizleme güvenliği içindir, eski siyah alanın kök nedeni doku üretim hatasıdır.

## Hücre aralıkları: tek kum meshinin gerçek yüzeyi

Yeni varyantlarda ayrı bej destek/panel nesnesi bulunmaz. Orijinal destek referans sürümde ve yedeklerde korunur; yeni sahnelere bağlanmaz. Dış kum halkası iç tabana aynı vertex/face ağı üzerinden bağlanır; hücre aralıklarının altında ayrı bir dolgu nesnesi yoktur. İç yüzey de MAT_Mobile_Sand materyalini ve aynı BaseColor/Normal haritalarını kullanır.

Kum iç tabanı Z=-0.062, hücre altı Z=-0.061 ve hücre tepesi Z=-0.001. Dolayısıyla görünür aralıklarda doğrudan kum vardır; hücre tabanıyla 0.001 birim geometrik tolerans korunur. Tam fiziksel temas iddia edilmez. Hücreler yeniden ölçeklenmedi veya yeniden şekillendirilmedi. 6×6 yeni kopya bütün onaylı hücre verilerini korur. 5×5 için aynı hücreler 25 adet olarak merkezlenir; özgün 6×6 verisine yazılmaz.

6×6’da 85, 5×5’te 56 yatay/dikey aralık ve kavşak ray-cast örneği aynı kum nesnesini Z=-0.062’de buldu. GapClose renderları bunu yakından gösterir. Kumun en yüksek noktası yaklaşık Z=-0.0145; hücrelerin üst düzlemi ve Z>=0 hareketli blok hacmi açıktır. Alt yan hücre kenarında gömülme dikişi için 0.0002 kontrollü örtüşme korunur; hücre geometrisi değişmez.

## Dengeli, asimetrik çatlak dağılımı

Her boyut için bağımsız seed: 6×6=28417, 5×5=28416. Üst, alt, sağ ve sol bölgelerin her birinde bir uzun ana patika ve değişken sayıda orta/mikro patika vardır. Konum, yön, dönüşler, genişlik, taper ve derinlik bağımsızdır; döndürülmüş aynı geometri kullanılmaz. Ana hedef uzunluk 2–4, orta 0.5–1.5 birimdir. Tahta ve UI koruma sınırında bazı dallar kırpılabilir. Uçlar ve bazı hat parçaları kadraj dışına uzanabilir. Üst/alt UI bölgelerine denk gelen en dış bantlar sade bırakılır. Çatlaklar height/normal + albedo haritasıdır; gerçek oyuk meshleri olarak raporlanmaz.

| Boyut | Bölge | Ana uzun hat | Dallar dahil toplam patika |
|---|---|---|---|
| 6 | top | 1 | 6 |
| 6 | bottom | 1 | 5 |
| 6 | left | 1 | 7 |
| 6 | right | 1 | 4 |
| 5 | top | 1 | 5 |
| 5 | bottom | 1 | 7 |
| 5 | left | 1 | 4 |
| 5 | right | 1 | 4 |

## Teknik bütçe ve korunma

Her varyantta kum 1.873 vertex / 3.600 triangle; bir materyal ve iki adet 1024² texture. Dış çevre ve iç taban tek bağlı kum meshidir, yalnızca dış sınırdaki 144 açık kenar bilinçlidir. Beklenmeyen manifold dışı kenar ve dejenere yüzey 0. UV sonlu, normal uzunluğu hatası 1.2e-7 altında. Portre dış alanında UV 0–1 dışına uzanabilir; Blender texture EXTEND, gelecekte Unity için Wrap Mode Clamp gerekir. Unity import ayarı henüz yapılmadı. Cihaz performansı ölçülmedi.

Yedek Before_Mobile_*.blend dosyasıdır. Kaynak nesnelerin mesh, UV, normal, materyal, Transform ve Collection üyeliği parmak izleri üretim sonrası ve son kontrolde aynıdır. Mevcut Core, Unity kamera, UI, prefab ve hareketli blok assetleri yazılmadı. Scene değişiklikleri sadece Blender’daki yeni önizleme varyantlarındadır. Önceki onaylı çerçeveli/çerçevesiz tasarımlar korunur.

## Gerçekleştirilen işlemler

prepare_mobile.py paketli Python/Pillow/NumPy ile çalıştırıldı; yeni haritalar sonlu değer kontrolünden geçti. build_mobile_revision.py Blender MCP ile çalıştırıldı ve 5×5/6×6 sahneler ile sekiz render tamamlandı (araç bağlantısı son yanıtı alamasa da dosyalar ve qa.json tamamlandı). finish_mobile.py ile UV/normal, gap rayları, kaynak veri koruması ve ekran zarfı ölçüldü. Eski siyah doku maskesi eski üretim formülüyle yeniden üretildi; tam maske örtüşmesi doğrulandı. compose_report.py ile önce/sonra paftası, yakın çekim paftası ve bu rapor üretildi. Çıktılar görsel olarak incelendi.

Teslim: 6/ ve 5/ klasörlerinde Top_1080x1920.png, UnityPerspective_1080x1920.png, GapClose.png ve LowEdge.png; BeforeAfter.png ve GapAndEdge.png. qa.json ve pixel_checks.json sayısal sonuçlardır. Unity’ye export yapılmadı; kullanıcı onayı bekleniyor.