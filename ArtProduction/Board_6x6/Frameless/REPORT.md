# Block Rift — çerçevesiz 6×6 alternatif

Durum: Alternatif üretildi; kullanıcı görsel onayı bekleniyor. Unity export yapılmadı.

## Yapılan değişiklik

Board_6x6_Frameless Collection oluşturuldu. Yeni varyantta dış çerçeve yoktur; kalın alt platform yerine tek kapalı, ince sıcak bej destek mesh vardır. Önceki çerçeveli tahta, onun hücreleri ve diğer onaylı assetler silinmedi veya değiştirilmedi. İşlem öncesi yedek: C:\Users\sahba\BlockRift\ArtProduction\Board_6x6\Frameless\Before_Frameless_20261027_181007.blend.

| Özellik | Önceki çerçeveli | Yeni çerçevesiz |
|---|---:|---:|
| Dış X/Y ölçüsü | 6.240 × 6.240 | 5.980 × 5.980 |
| Taşıyıcı kalınlığı | 0.119 | 0.006 |
| Taşıyıcı Z aralığı | -0.180 … -0.061 | -0.067 … -0.061 |
| Yükseltilmiş dış çerçeve | Var | Yok |
| Destek dış sınırı — hücre yan sınırı farkı | yaklaşık 0.135 | en fazla 0.005 |
| Hücre merkezi aralığı | 1.000 | 1.000 |
| Hücre X/Y genişliği | 0.970 | 0.970 |
| Hücre yüksekliği | 0.060 | 0.060 |
| Gerçek hücre tepesi Z | -0.001 | -0.001 |
| BoardSurfaceHeight / BlockPlacementOffset | 0 / 0 | 0 / 0 |

Destek kalınlığı yaklaşık %95 azaltıldı. Dış köşeler .150 yarıçaplı yuvarlak profille ve çok küçük kenar geçişleriyle oluşturuldu. Yeni desteğin tek başına üst sınırı -0.061 olduğundan yükselen kenarlık oluşturmaz. Hücrelerin özgün yan yüzeyleri kendi 0.060 yüksekliğiyle kalır; hücre geometrisini koruma şartı gereği bunlar inceltilmedi.

## Korunan veriler ve sahne düzeni

Board_6x6_Frameless_Cells, önceki Board_6x6_Cells ile birebir aynı mesh datablockunu kullanır. Vertexler, topoloji, UV, custom normals, materyal bölgeleri ve bütün yerel hücre merkezleri aynı kalır. Yeni hücre üretimi veya gizli ölçek değişikliği yoktur.

Yeni variant rootu yalnızca yan yana önizleme için (8,10,0) konumundadır. Yerel oyun koordinatları önceki tahtayla aynıdır; Z yerleşimi değişmez. Mevcut çerçeveli tahtanın root konumu ve bütün orijinal asset konumları korunur. Onay sonrası entegrasyonda root önizleme offseti oyun konumu olarak kullanılmamalıdır.

Koleksiyonlar: Board_6x6_Frameless ve Board_6x6_Frameless_FitTests. Nesneler: Board_6x6_Frameless_Root, Board_6x6_Frameless_Support, Board_6x6_Frameless_Cells. Yedi fit kopyası orijinal onaylı assetlerin meshlerini paylaşır.

## Yerleşim testleri

| Kaynak asset | Grid merkezi hatası | Hücre ortası alt açıklığı | Yeni destek kesişimi | Hücre kesişimi |
|---|---:|---:|---:|---:|
| Block_4x1_Export | 0.00000000 | 0.001000 | 0 | 0 |
| Obstacle_1x1_Stone_Export | 0.00000000 | 0.001000 | 0 | 0 |
| Special_1x1_Star_Export | 0.00000000 | 0.001000 | 0 | 0 |
| Block_3x1 | 0.00000000 | 0.001000 | 0 | 0 |
| Block_2x1_Export | 0.00000000 | 0.001000 | 0 | 0 |
| Block_1x1_Export | 0.00000000 | 0.001000 | 0 | 0 |
| Block_2x2_Export | 0.00000000 | 0.001000 | 0 | 0 |

Yedi fiziksel mesh kopyasıyla ray-cast ve BVH testleri tekrarlandı: PASS. Ayrıca toplam 205 geçerli konum ve bunların 124 dış kenar konumu gerçek vertex sınırları ve iki tahta meshine karşı BVH kesişimiyle kontrol edildi: 0 kesişim. Tolerans 0.00001. Bloklar Z=0, hücre tepeleri Z=-0.001; önceki görsel .001 tolerans korundu. Yeni destek tepesine minimum dikey açıklık .061 birimdir.

Bütün bu konumlarda alt kapak vertexlerinin destek dışına taşmadığı, yuvarlatılmış destek sınırı ve poligon yay örnekleme payı dahil kontrol edildi. Konservatif en küçük alt kapak kenar payı: 0.00566137 birim. Dış çerçeve varyantta bulunmadığı için onunla hareket engeli yoktur.

Yeni destek: 274 vertex / 544 triangle; kapalı manifold, pozitif hacim, dejenere yüzey 0, yeni destek üzerinde ters yarımküre custom shading normal köşesi 0. Hücrelerle toplam varyant 16.672 üçgendir; önceki tahta 17.688 üçgendi. Özgün hücre ve assetlerin önceki denetimde saptanan alt bevel normal bulguları bu revizyonda düzeltilmedi; aynen korundu.

## Görsel karşılaştırma ve kum entegrasyonu

Framed_*, Frameless_* ve FramelessFits_* renderları aynı ışık, kamera ve renk yönetiminde üstten, 40° perspektif ve yandan alındı. Dokuz PNG gerçek şeffaf RGBA arka plana sahiptir. Karşılaştırma paftasındaki düz nötr renk yalnızca Pillow ile hazırlanmış sunum zeminidir; Blender sahnesinde zemin meshine karşılık gelmez.

Yeni kum, zemin, kaya, bitki, çevre modeli veya dekor üretimi yoktur. Oluşturulan tek yeni özgün mesh ince destektir; diğer yeni mesh nesneleri önceki modellerin referans kopyalarıdır. Render sahnelerinde mevcut stüdyo zemini de kullanılmadı. Yeni ışık ve kamera Preview_Studio_Frameless_NoGround içinde yer alır; önceki Preview_Studio değişmedi.

2D kum üzerinde gömülme ve temas gölgesi bu aşamada Unity içinde gösterilmedi. İnce destek, minimum dış taşma ve yumuşak kenarlar bunun için hazırlandı. Kum görseli gerçek bir gölge alıcı 3D yüzey olmadığından, nihai temas gölgesi Unity sunumunda ayrıca ayarlanmalıdır; hazır prefab/script değişikliği veya entegrasyon yapıldığı iddia edilmez.

## Çalıştırılan işlemler

- Blender MCP execute_blender_code: create_frameless.py → alternatif ve yedek oluşturuldu.
- Blender MCP execute_blender_code: verify_frameless.py → 7 tip / 205 konum / 124 dış kenar PASS.
- Blender MCP execute_blender_code: render_comparison.py → 9 RGBA render üretildi.
- Blender MCP ile son parmak izi / destek ayak izi kontrolü: orijinal nesnelerde değişiklik 0, beklenmeyen yeni geometri 0.
- Paketli Python/Pillow: compose_and_report.py → karşılaştırma paftası ve rapor.

Kaynak dosya AssetPreview.blend yeni alternatif eklenerek kaydedildi. Mevcut modellerin geometri, normal, UV, materyal, Transform ve Collection üyelikleri başlangıç/son parmak izleriyle aynı doğrulandı. Unity Assets, prefablar, C# scriptleri, Packages ve ProjectSettings bu çalışma tarafından yazılmadı. Git çalışma ağacında başka değişiklikler gözlendi; bunlara müdahale edilmedi veya temiz çalışma ağacı iddiasında bulunulmadı. Core ve GDD kuralları değişmedi; burada oynanabilir bölüm üretilmedi.

Parametrik dosyalar ve test sonuçları bu klasörde ve Blender Text datablocklarında saklanır. Yeni varyant onaylanana kadar önceki çerçeveli tasarım referans olarak korunur.