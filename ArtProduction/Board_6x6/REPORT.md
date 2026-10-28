# Block Rift — 6×6 tahta üretim ve ölçü raporu

Sonuç: PASS. A seçeneği uygulandı. Mevcut asset geometrileri, ölçekleri, pivotları, materyalleri ve önizleme konumları değişmedi. Export yapılmadı.

## Kesin ölçüler

| Değer | Blender birimi |
|---|---|
| Mantıksal hücre aralığı | 1.000 |
| Görsel hücre X/Y genişliği | 0.970 |
| Görsel hücreler arası boşluk | 0.030 |
| Mantıksal BoardSurfaceHeight | 0.000 |
| BlockPlacementOffset / blok tabanı Z | 0.000 |
| Gerçek görsel hücre tepesi Z | -0.001 |
| Hücre yüksekliği | 0.060 |
| Taşıyıcı platform Z aralığı | -0.180 … -0.061 |
| Dış çerçeve Z aralığı | -0.061 … -0.006 |
| Dış tahta X/Y ölçüsü | 6.240 × 6.240 |
| Mantıksal oynama alanı | 6.000 × 6.000 |
| İç çerçeve düz kenar açıklığı | 6.070 |

Hücre tepeleri kullanıcı izniyle Z=0 altına 0.001 alınmıştır. Bu, blok-hücre tabanında eş düzlemli yüzey ve Z-fighting oluşmasını önleyen görsel toleranstır. Bloklar yükseltilmedi; görsel hücreyle blok altı arasındaki minimum dikey açıklık 0.001 birimdir. Tam fiziksel temas iddia edilmez. Hücre kabartmalarının tamamı Z<0, bütün blok geometrileri Z>=0 olduğu için alt yüzeylerin içinden geçme yoktur.

Bloklar arası alt gövde teması onaylanan A seçeneğine uygun biçimde korunur. Üst bevel, panel kanalları, baklava oyukları, taş ve yıldız formları değiştirilmedi.

## Gerçek mesh fit sonuçları

| Kaynak asset | 6×6 geçerli konum sayısı | Minimum XY çerçeve açıklığı | Sonuç |
|---|---|---|---|
| Block_4x1_Export | 18 | 0.035000 | PASS |
| Obstacle_1x1_Stone_Export | 36 | 0.035000 | PASS |
| Special_1x1_Star_Export | 36 | 0.035000 | PASS |
| Block_3x1 | 24 | 0.035000 | PASS |
| Block_2x1_Export | 30 | 0.035000 | PASS |
| Block_1x1_Export | 36 | 0.035000 | PASS |
| Block_2x2_Export | 25 | 0.035000 | PASS |

Toplam 205 konumda gerçek vertex bulutlarıyla tahta sınırları, panel merkezleri ve yuvarlatılmış çerçeve açıklığı kontrol edildi. Ölçüm toleransı 0.00001 birim. Çok hücreli asset panel merkezleri önceden gerçek oyuk tabanı bileşenlerinden ölçülmüştür. Maksimum mevcut panel merkezi sapması yaklaşık 1.49e-8 birimdir.

Ana sahnedeki Board_6x6_FitTests koleksiyonunda yedi gerçek yerleşim kopyası vardır; hepsi onaylı assetlerin orijinal mesh verisini kullanır, scale=1 ve Z=0. Bu yedi mesh için üç tahta nesnesine karşı gerçek BVH yüzey kesişim testi yapıldı: hepsi 0. Alt yüzeyler altına denk gelen 16 hücre merkezinde ışın kesitiyle görsel hücre tepe yüksekliği -0.001 doğrulandı.

Görsel test düzenindeki 21 blok çifti ayırıcı düzlem yöntemiyle kontrol edildi: hacimsel penetrasyon yok; komşu alt gövde teması kabul edildi. Çerçevenin en yüksek Z değeri -0.006 olduğu için Z>=0 blokların tüm ara hareket zarfından 0.006 aşağıdadır. Böylece çerçeve hareketi geometrik olarak engellemez. Bu bir Unity Collider/Physics testi değildir.

5×5 için aynı asset ölçeğinde 136 konumun vertex zarfı ve panel merkezleri aynı grid aralığıyla sayısal kontrol edildi: PASS. Ayrı 5×5 tahta veya yeni hareketli asset üretilmedi.

36 hücre gerçek bağlı mesh bileşenlerinden tekrar ölçüldü: her biri 0.97×0.97; bütün merkezler grid üzerinde, bütün tepe yükseklikleri -0.001.

## Topoloji ve optimizasyon

| Tahta nesnesi | Vertex | Üçgen | Manifold dışı / dejenere |
|---|---|---|---|
| Board_6x6_Base | 314 | 624 | 0 / 0 |
| Board_6x6_Frame | 468 | 936 | 0 / 0 |
| Board_6x6_Cells | 8136 | 16128 | 0 / 0 |

Toplam 17.688 üçgen, 8.918 vertex. Üç tahta nesnesinde kapalı manifold, pozitif hacim, sonlu UV, birim/sonlu custom normals ve sonlu Mikk tangent kontrolü geçti. Tahta sabit üç mesh olarak birleştirilebilir; mevcut onay kapsamında böyle bir export/birleştirme yapılmadı. Mobil cihaz üzerinde performans ölçülmedi.

## Materyal ve görünüş

Hücre: MAT_Board_Cream, sRGB #F5D9B8, Metallic=0, Roughness=0.35. Platform ve çerçeve: MAT_Board_Beige, sRGB #D9A77D, Metallic=0, Roughness=0.40. Principled Base Color değerleri sRGB→lineer çevrilir. Unity URP Lit eşlemesinde Smoothness sırasıyla 0.65 ve 0.60. Blender shader ağacının Unity tarafından otomatik dönüştürüldüğü iddia edilmez.

Referanstaki 6×6 sembolsüz krem hücreler, ince yumuşak ayırımlar ve tek bej taşıyıcı korunur. Hücre kabartmaları mevcut blok yüksekliğine göre daha alçak tutulur; bu, yerleşim yüksekliği gereksiniminin bilinçli sonucudur. Çevre dekorları yeni asset olarak eklenmedi.

## Sahne ve dosyalar

AssetPreview.blend içinde Board_6x6 koleksiyonu: Board_6x6_Root, Board_6x6_Base, Board_6x6_Frame, Board_6x6_Cells. Root yalnızca sahne düzeni için (0,10,0) önizleme offsetindedir; çocuk meshler yerel olarak oyun yüzeyi merkezini origin kabul eder. Blok testleri ayrı Board_6x6_FitTests koleksiyonundadır. Mevcut modellerin yerleri değiştirilmemiştir.

Boş ve dolu review sahneleri: Review_Board_6x6_Empty / Review_Board_6x6_Fits. Yeni ışık ve kamera Preview_Studio_Board_6x6 içindedir; önceki Preview_Studio korunur. Review kopyaları orijinal meshleri paylaşır ve ayrı sahnelerdedir.

Renders: Empty_Top.png, Empty_Perspective40.png, Empty_Side.png, Fits_Top.png, Fits_Perspective40.png, Fits_Side.png. Gerçek materyaller ve mevcut assetler kullanıldı. Görünüşlerin aynı studio koşullarıyla alındığı Board_6x6_ReviewSheet.png paftası hazırlandı ve görsel olarak incelendi.

Üretim öncesi yedek: C:\Users\sahba\BlockRift\ArtProduction\Board_6x6\Before_BoardCreation_20261027_142920.blend. Parametreler parameters.json, nesne/konum bilgileri build_metadata.json, sayısal sonuçlar fit_report.json içinde. Scriptler klasörde ve Blender Text datablocklarında saklanır.

## Çalıştırılan işlemler ve sınırlar

- Blender MCP execute_blender_code: generate_board.py → PASS.
- Blender MCP execute_blender_code: verify_board.py → PASS; hücre omuz normalleri rafine edildikten sonra nihai kontroller tekrar çalıştırıldı.
- Blender MCP execute_blender_code: render_board.py, ardından render_existing_board.py → altı final render üretildi.
- Paketli Python/Pillow: compose_and_report.py → pafta ve rapor üretildi.
- Başlangıç/son mesh, topoloji, normal, UV, materyal, Transform ve Collection üyeliği parmak izleri karşılaştırıldı: önceki nesneler için değişiklik listesi boş.
- GDD 3.1 (5×5/6×6 alan) ve 3.3 (mevcut dikdörtgenler) etkilenmedi. Core, oyun hareketi, Unity C# veya Unity Assets değiştirilmedi.
- Fit düzeni bir oynanabilir bölüm veya solver doğrulaması değildir. Unity import, shader yeniden eşleme, collider, cihaz performansı ve FBX/GLB export testleri yapılmadı.

## Yeniden üretim

Before_BoardCreation yedeği üzerinden generate_board.py, verify_board.py ve render_board.py sırasıyla çalıştırılabilir. Üretici mevcut Board_6x6 koleksiyonunun üzerine yazmayı reddeder. Hazır review sahnelerinde render_existing_board.py tekrar çalıştırılabilir. Varsayılan üretim 6×6 içindir; üretim parametreleri ve görsel merkez hesabı 5×5 boyutu için aynı pitch ve ölçeği korur; yeni 5×5 asset oluşturmak ayrı bir üretim işlemidir.