# Block Rift — 6×6 tahta öncesi ölçü raporu

Durum: Ölçüm tamamlandı. Tahta üretilmedi; pozitif yan boşluk / yüzey teması tercihi için onay bekleniyor.

## Gerçek mesh ölçüleri

| Nesne | Dünya X×Y×Z | Ölçek | Yerel taban Z | Panel merkezi hatası |
|---|---|---|---|---|
| Block_1x1_Export | 1.0000000, 1.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |
| Block_2x1_Export | 2.0000001, 1.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |
| Block_3x1 | 3.0000000, 1.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |
| Block_4x1_Export | 4.0000000, 1.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |
| Block_2x2_Export | 2.0000000, 2.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |
| Obstacle_1x1_Stone_Export | 1.0000000, 1.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |
| Special_1x1_Star_Export | 1.0000000, 1.0000000, 0.3000000 | 1.0000000, 1.0000000, 1.0000000 | 0.0000000 | 0.0000000 |

Bütün originler yerel taban merkezindedir. Dönüşler sıfır, parent yok. Block_3x1_Export adıyla nesne yok; onaylı 3×1 mesh gerçek adı Block_3x1 ile ölçüldü, yeniden adlandırılmadı.

## Dünya sınırları ve mevcut önizleme konumları

| Nesne | Dünya origin | Dünya X min/max | Dünya Y min/max | Dünya Z min/max |
|---|---|---|---|---|
| Block_1x1_Export | -1.3000000, 0.0000000, 0.0000000 | -1.8000000, -0.8000000 | -0.5000000, 0.5000000 | 0.0000000, 0.3000000 |
| Block_2x1_Export | 0.7000000, 0.0000000, 0.0000000 | -0.3000000, 1.7000000 | -0.5000000, 0.5000000 | 0.0000000, 0.3000000 |
| Block_3x1 | 0.0000000, 2.0000000, 0.0000000 | -1.5000000, 1.5000000 | 1.5000000, 2.5000000 | 0.0000000, 0.3000000 |
| Block_4x1_Export | 0.0000000, 4.0000000, 0.0000000 | -2.0000000, 2.0000000 | 3.5000000, 4.5000000 | 0.0000000, 0.3000000 |
| Block_2x2_Export | 3.5000000, 2.0000000, 0.0000000 | 2.5000000, 4.5000000 | 1.0000000, 3.0000000 | 0.0000000, 0.3000000 |
| Obstacle_1x1_Stone_Export | 3.5000000, 4.0000000, 0.0000000 | 3.0000000, 4.0000000 | 3.5000000, 4.5000000 | 0.0000000, 0.3000000 |
| Special_1x1_Star_Export | 5.1999998, 4.0000000, 0.0000000 | 4.6999998, 5.6999998 | 3.5000000, 4.5000000 | 0.0000000, 0.3000000 |

## Ortak ölçü sistemi

- Ölçülen grid_pitch = 1.0000000. Çok hücreli blokların baklava tabanlarının bağlı mesh bileşenlerinden ölçülen panel merkezleri tam 1 birim aralıklı.
- Tolerans: 0.00001 Blender birimi. Maksimum dünya ölçü hatası 5.960464477539063e-08; maksimum panel merkezi hatası 1.4901161193847656e-08. İkisi de tolerans içinde.
- 1×1/taş/yıldız dış ölçüsü 1×1; 2×1/3×1/4×1 genişliği tam hücre sayısı. 2×2 ölçüsü 2×2. Bevel kaynaklı anlamlı grid taşması yok; yıldız gövdesindeki sayısal sapma 5.96e-8.
- Yıldızın beşli simetri hatası 6.14e-8 birim. Taşın merkez ölçümü gerçek dış sınırlarından yapıldı.
- Ortak yerel taban Z=0; ortak tepe Z=0.30. BoardSurfaceHeight=0 ve BlockPlacementOffset=0 önerilebilir: taşıyıcı platform negatif Z yönüne kurulmalı. Bunlar henüz oluşturulmuş tahta değerleri değildir.
- 5×5 ve 6×6 için aynı pitch kullanılabilir; merkezleme c(i)=(i-(N-1)/2)×1. Bu bir görsel koordinat eşlemesidir, yeni oyun kuralları değildir. Unity dönüşümünde Blender Z yüksekliği Unity Y olur; satır yönü açıkça seçilmelidir. Unity sunum eşlemesi bu aşamada değiştirilmedi.

## Kritik boşluk bulgusu

Pitch=1 ile yan yana gelen tam genişlikli gövdelerin minimum X boşluğu 0.0000000. Alt yan yüzeyler temas eder. Yuvarlatılmış tepe kesitlerinde görünür açıklık vardır. Örnek iki Block_1x1 için gerçek mesh ışın kesiti:

| Yerel Z | Y=0 kesitinde komşu yüzeyler arası açıklık |
|---|---|
| 0.100 | 0.0000000 |
| 0.210 | 0.0000000 |
| 0.280 | 0.0546889 |
| 0.299 | 0.1359425 |

Dolayısıyla görsel üst ayrımı mümkündür; bütün gövde boyunca pozitif fiziksel açıklık mevcut değildir. Bu iki kavram aynı kabul edilmemelidir.

Yedi model tipi arasında 49 yatay komşuluk çifti, gerçek vertex sınırları ve seçili Z seviyelerinde BVH ray-cast kesitleriyle incelendi. Ayırıcı düzlem sınırları bütün çiftlerde 0 boşluk verdi; bu yerleşimlerde pozitif hacim çakışması olmadığını gösterir. Bazı düzensiz yüzeylerde belirli kesitlerde daha fazla açıklık olabilir. Bu kontrol henüz oluşturulmamış tahta ve çerçeveyle çarpışma testi değildir.

Örneğin pozitif boşluk elde etmek için pitch=1.02 seçilirse 4×1 panel merkezleri, ortalanmış yerleşimde hücrelerden sırasıyla +0.03,+0.01,-0.01,-0.03 birim sapar. Sadece dış çerçeveyi veya hücrelerin görsel boyutunu değiştirmek blok gövdeleri arasındaki teması çözmez. Mevcut blokları gizlice ölçeklendirme/geometri değiştirme yapılmadı.

## Önerilen karar

A — Öneri: mevcut modeller ve pitch=1 korunsun; alt gövde teması kabul edilsin, görsel ayrım mevcut üst bevel ile sağlansın. Tahtanın sembolsüz hücreleri örneğin 0.97 görsel genişlik ve 0.03 hücre arası boşlukla üretilebilir; bu tahta hücre aralığıdır, bloklar arasında fiziksel boşluk iddiası değildir. Çerçeve iç sınırı dolu hücre zarfının dışına alınmalıdır. Tahta tasarım ölçüleri onaydan sonra belirlenip tam fit testleri yapılmalıdır.

B — Bütün gövdeler arasında pozitif fiziksel açıklık zorunluysa, mevcut blokların panel merkezlerini koruyan enine boyut revizyonu ayrıca onaylanmalıdır. Uniform scale veya pitch artırımı uzun blok merkezlerini bozar; mevcut değiştirmeme kısıtı nedeniyle burada uygulanamaz.

## Doğrulama ve korunma

measure_assets.py Blender MCP ile yürütüldü. Mesh koordinatları üzerinden dünya/yerel bbox, origin, scale, panel merkezleri, yuvarlatma sınırları ve komşuluk kesitleri ölçüldü. Başlangıç/son mesh, topoloji, normal, UV, materyal ve Transform parmak izleri aynı. Kaynak AssetPreview.blend üzerine kaydedilmedi. Yedek: ArtProduction\Board_6x6\Analysis\Before_BoardAnalysis_20261027_141857.blend.

Oyun kodu ve Core değiştirilmedi. GDD 3.1 kapsamında 5×5/6×6 ve içerideki hedef hücre kuralları korunur; bu çalışma yalnızca görsel asset ölçümüdür. Unity kaynaklarında cell size/world mapping araması sonuç vermedi; Unity entegrasyonu doğrulandı diye raporlanmıyor.

## Henüz yapılmayanlar

Tahta/çerçeve/hücre üretimi, gerçek yerleşim kopyaları, çerçeve-fit testleri, tahta yüzey yüksekliği testi ve tahta renderları bu karar sonrasına bırakıldı. Export yapılmadı.