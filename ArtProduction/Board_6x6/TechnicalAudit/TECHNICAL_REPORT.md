# Block Rift — onaylı 6×6 tahta teknik denetimi

**Geometrik uyum: PASS. Custom shading normal yönleri: REVIEW REQUIRED.**

Denetim salt okunur olarak yürütüldü. Hiçbir modele, materyale, UV verisine, normale, pivot veya Transform değerine düzeltme uygulanmadı. AssetPreview.blend kaydedilmedi ve export yapılmadı. Sahne seçimi de korundu.

## Ölçüm sistemi

Tolerans: 0.00001 Blender birimi. Gerçek mesh vertexleri board root koordinatlarına dönüştürüldü. 36 hücre bağlı mesh bileşenleriyle yeniden ölçüldü. Merkez eksenleri: -2.5, -1.5, -0.5, 0.5, 1.5, 2.5. Ölçülen pitch 1.0; hücrelerin görsel X/Y ölçüsü 0.97 ve gövde seviyesinde aralık 0.03. Üst yuvarlatma, görünür üst açıklığı bu minimumun üzerinde artırabilir.

Grid merkezi maksimum hata: 9.878167619740789e-09. Pitch maksimum hata: 0.0. Mevcut modellerin boyutları, ölçekleri ve pivotları korunarak kontrol yapıldı.

## Yedi asset — gerçek yerleşim ve açıklıklar

| Asset | Grid merkez hatası | Hücre merkezi dikey açıklığı | Alt yüzey örneklerinde açıklık aralığı | Hücreler arası kanal altında | Board mesh kesişimi |
|---|---:|---:|---:|---:|---:|
| Block_4x1_Export | 0.00000000 | 0.001000 | 0.001000 … 0.024385 | 0.061000 | 0 |
| Obstacle_1x1_Stone_Export | 0.00000000 | 0.001000 | 0.001000 … 0.027865 | — | 0 |
| Special_1x1_Star_Export | 0.00000000 | 0.001000 | 0.001000 … 0.017556 | — | 0 |
| Block_3x1 | 0.00000000 | 0.001000 | 0.001000 … 0.024384 | 0.061000 | 0 |
| Block_2x1_Export | 0.00000000 | 0.001000 | 0.001000 … 0.024384 | 0.061000 | 0 |
| Block_1x1_Export | 0.00000000 | 0.001000 | 0.001000 … 0.024385 | — | 0 |
| Block_2x2_Export | 0.00000000 | 0.001000 | 0.001000 … 0.024384 | 0.061000 | 0 |

Blokların yerleşim tabanı Z=0, görsel hücre tepeleri Z=-0.001. Dolayısıyla **blok–hücre fiziksel teması yoktur**; minimum dikey açıklık 0.001 birimdir. Önceden onaylanan bu küçük görsel tolerans eş düzlemli yüzeyleri ve Z-fighting riskini önler. Hücrelerin bütün vertexleri -0.001 veya daha aşağıda, blokların bütün vertexleri 0 veya daha yukarıdadır: pozitif ayırıcı düzlem açıklığı hacimsel gömülmeyi de dışlar.

Hücre omuzlarına denk gelen alt yüzey örneklerinde açıklık yaklaşık 0.017556–0.027865 seviyelerine kadar artar. Uzun blokların ve 2×2 bloğun hücreler arası boşlukları köprülediği noktalar ayrıca gerçek ray-cast ile kontrol edildi: o noktalarda taşıyıcı platforma kadar 0.061 açıklık vardır. Bu değer hücrenin düz orta yüzeyindeki açıklık değildir. İlk örneklem tablosundaki maksimumlar örneklenmiş değerlerdir; tüm yüzeyde global uzaklık optimizasyonu değildir.

Alt kapak vertexleri ve üçgen merkezlerinden toplam 1932 dikey örnek incelendi. Çok hücreli bloklarda toplam 11 kanal/birleşim noktası ayrıca kontrol edildi.

## Çerçeve ve bütün konumlar

| Asset | 6×6 konum | Dış kenar konumu | Minimum XY iç duvar açıklığı | Çerçeve BVH kesişimi |
|---|---:|---:|---:|---:|
| Block_4x1_Export | 18 | 14 | 0.035000 | 0 |
| Obstacle_1x1_Stone_Export | 36 | 20 | 0.035000 | 0 |
| Special_1x1_Star_Export | 36 | 20 | 0.035000 | 0 |
| Block_3x1 | 24 | 16 | 0.035000 | 0 |
| Block_2x1_Export | 30 | 18 | 0.035000 | 0 |
| Block_1x1_Export | 36 | 20 | 0.035000 | 0 |
| Block_2x2_Export | 25 | 16 | 0.035000 | 0 |

205 geçerli konumun tamamında, gerçek onaylı meshlerle üç tahta nesnesine karşı BVH yüzey kesişimi kontrol edildi: 0. Bunların 124 tanesi dış kenara oturur. İlave ayırıcı Z düzlemi analizi tamamen iç içe kalma durumlarını da dışlar. Yuvarlatılmış iç duvar sınırı gerçek frame profilinin .100 köşe yarıçapıyla kontrol edildi; vertexler ve bunların arasındaki yüzler konveks açıklık içinde kalır.

Çerçeve en yüksek Z=-0.006; bloklar Z>=0. Çerçeveyle blok hareket zarfı arasında en az 0.006 dikey açıklık vardır. Böylece çerçeve yalnızca son hücre konumlarında değil, tahta içindeki ara görsel hareket konumlarında da blok geometrisini kesmez. Komşu blokların birbirine alt yan yüzeyden temas etmesi onaylanan tasarımdır; bu blok–tahta fiziksel temasından farklıdır.

## 5×5 uyumluluğu

Aynı grid_pitch=1, görsel hücre genişliği=0.97, hücre boşluğu=0.03 ve aynı asset ölçüleriyle 136 konum sayısal olarak kontrol edildi. Merkez eksenleri -2,-1,0,1,2 olur. Yeniden ölçeklendirme gerekmez. Bu test gerçek asset vertexlerini kullanır; ayrı bir 5×5 tahta sahnesi üretip karşılaştırma değildir.

Mevcut üreticinin temel hücre/platform hesabı rows/columns parametrelerine bağlıdır. Ancak Board_6x6 adlandırmaları ve mevcut yedi assetin fit dizilimi 6×6 özelindedir. Aynı fit dizilimi 5×5 için kopyalanırsa şu nesneler sınır dışına çıkar: Special_1x1_Star_Export, Block_1x1_Export, Block_2x2_Export. 5×5 üretiminde isimler ve test dizilimi uyarlanmalıdır; grid veya asset ölçüsü değişikliği gerekmez. Bu denetimde script/asset düzeltmesi uygulanmadı.

## Pivot, ölçek ve UV

Yedi kaynak assetin originleri yerel taban merkezi (0,0,0), taban Z=0; hepsinde scale=(1,1,1), rotation=(0,0,0). Fit kopyaları aynı mesh verisini paylaşır. Üç tahta meshinin yerel originleri nominal oyun düzlemi merkezidir. Board_6x6_Root sahne önizlemesi için dünya (0,10,0) konumundadır; ileride export edilirken bu önizleme offseti oyun koordinatı olarak aktarılmamalı, yerel tahta koordinatları esas alınmalıdır. Bu aşamada Transform uygulanmadı.

10 mesh üzerinde birer aktif UV katmanı vardır. Tüm UV koordinatları sonlu ve [0,1] içinde; sıfır alanlı UV üçgeni 0. UV island çakışmaları, texel density, lightmap UV2 ve Unity tangent aktarımı bu denetimin kapsamı değildir.

Bütün meshlerde kapalı manifold ve tutarlı yüz sarımı doğrulandı: manifold dışı kenar 0, dejenere geometri yüzü 0, ters yönlü komşu sarım kenarı 0; toplam işaretli hacimler pozitif. Normal uzunlukları sonlu/birim; maksimum uzunluk hatası 1.8742865059984126e-07. Ancak bu büyüklük kontrolü normal yönünün doğru olduğunu tek başına göstermez.

## Açık teknik bulgu: alt bevel custom normals

Aşağıdaki sayılar vertex sayısı değil, yüz köşesi / loop normal sayısıdır. Geometrik yüz normalinin ters yarımküresine bakan custom shading normalleri tespit edildi (dot < -0.0001).

| Mesh | Uyuşmayan köşe normali | Yerel geometri bandı Z | Sonuç |
|---|---:|---|---|
| Board_6x6_Base | 156 | -0.180000 … -0.175000 | Yön düzeltmesi gerekli |
| Board_6x6_Frame | 0 | — | PASS |
| Board_6x6_Cells | 3024 | -0.061000 … -0.052000 | Yön düzeltmesi gerekli |
| Block_4x1_Export | 294 | 0.000000 … 0.004000 | Yön düzeltmesi gerekli |
| Obstacle_1x1_Stone_Export | 0 | — | PASS |
| Special_1x1_Star_Export | 480 | 0.000000 … 0.004000 | Yön düzeltmesi gerekli |
| Block_3x1 | 294 | 0.000000 … 0.004000 | Yön düzeltmesi gerekli |
| Block_2x1_Export | 582 | 0.000000 … 0.004000 | Yön düzeltmesi gerekli |
| Block_1x1_Export | 576 | 0.000000 … 0.004000 | Yön düzeltmesi gerekli |
| Block_2x2_Export | 300 | 0.000000 … 0.004000 | Yön düzeltmesi gerekli |

Bulgular alt çevre bevel bandında sınırlıdır. Geometrik yüzler ters çevrilmiş değildir; problem özel shading normallerinin alt kenarda yukarı yönlü tanımlanmasıdır. Kesişim veya grid doğruluğunu bozmaz, ancak farklı kamera/ışık ve Unity imported normals kullanımında alt kenar aydınlatmasını bozabilir. Önceki birim/sonlu normal testi bu yön sorununu yakalamaz; bu denetimde geometrik yüz normaliyle yön karşılaştırması eklendi.

Öneri: İleride yalnızca bu alt bevel custom normals bölgesini kontrollü olarak düzeltmek; vertex konumları, topoloji, üst panel profili, materyal ve UV korunabilir. Kullanıcının onaylı modellere dokunmama isteği nedeniyle hiçbir normal yeniden hesaplanmadı veya yazılmadı. **Bu bulgu kapanmadan tam normal aktarım onayı verilmez.** Taş ve dış çerçevede bu yön sorunu tespit edilmedi.

## Yürütülen araçlar ve değişmezlik

Blender MCP execute_blender_code ile audit_current_scene.py, inspect_normal_directions.py ve sample_channel_clearances.py çalıştırıldı. Yerel rapor/CSV üretimi için write_technical_report.py paketli Python ile yürütüldü. C# veya gameplay değişikliği yok; GDD 3.1 kapsamındaki 5×5/6×6 oyun alanı kuralları etkilenmedi.

Başlangıç/son geometri, topoloji, normal, UV, materyal, dünya Transform ve Collection üyeliği parmak izlerinde değişen nesne yok. Yeni Blender nesnesi oluşturulmadı. UI seçim ve aktif sahne aynı kaldı. Diskteki AssetPreview.blend SHA-256 öncesi/sonrası aynı:

`cc314a3341b21f2b567265d0b080ece03146f7b84ee602bbd2b1a26910655d9d`

Unity import, shader yeniden eşleme, eksen dönüşümünün fiili import testi, collider/oyun fiziği ve cihaz performansı test edilmedi. FBX/GLB export yapılmadı. Blender Z-up → Unity Y-up dönüşümü ileride exporter/importer üzerinden bir kez uygulanmalı; burada dönüşüm uygulanmadı.

Makine tarafından okunabilir sonuçlar audit.json, özet ölçümler measurements.csv dosyasında bulunur.