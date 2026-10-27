# Block_3x1

Onaylı Block_1x1_Final mesh kesitleri ve Block_2x1 birleşik gövde/panel bağlantısı kullanıldı. Yalnız 3x1 üretildi; mevcut 1x1 ve 2x1 mesh, materyal bağlantıları ve Transform değerleri korundu. Core ve Unity Assets dosyaları değiştirilmedi. GDD oyun mekaniği değişikliği yok.

- Ölçü: yerel X/Y/Z = 3 / 1 / 0.30, Blender Z-up.
- Tek obje ve tek bağlı kapalı mesh; üç yükseltilmiş panel, üç geometrik baklava oyması.
- Oyuk derinliği 0.040. Panel tabanındaki tek taraflı 0.006 pay, 2x1 ile aynı; komşu tabanlar arası 0.012, üst platolar arası 0.160.
- Nötr gri materyal; doku veya sabit beyaz yansıma geometrisi yok.
- Yerel pivot taban merkezinde, scale (1,1,1), rotation sıfır. Ortak önizlemede obje konumu (0,2,0); export aşamasında yerel pivot ile sahne yerleşimi ayrı ele alınmalı.
- Her çevre kesiti masterın 192 örneğinden her ikinci nokta alınarak 96 örnekle temsil edildi. Örneklenen üst/oyuk koordinatları master ile aynı; aradaki kenarlar daha seyrek yaklaşıklar. Tam yoğunlukta kopya ile birebir aynı topoloji iddiası yok.
- 6.538 vertex, 13.072 üçgen. Bu Blender mesh sayısıdır; Unity'nin normal/UV ayrımları import vertex sayısını artırabilir. Cihaz performansı ölçülmedi.

## Doğrulama

Blender MCP execute_blender_code üzerinden generate_block_3x1.py çalıştırıldı. Bmesh denetimleri: 1 bağlı bileşen, 0 manifold dışı kenar, 0 dejenere yüzey, pozitif hacim, UV mevcut. Master .blend dosyasının SHA-256 özeti değişmedi; mevcut sahnedeki 1x1/2x1 Transform/mesh/material referansları önce-sonra aynı.

Ek BVH ray testi üç baklava merkezinde Z=0.260, üç düz üst alanda Z=0.300 ve iki ayırıcı merkezinde Z=0.220 buldu. Normal vektörleri sonlu ve birim uzunlukta. Pivot, birim ölçek ve sıfır rotasyon doğrulandı. Sayısal sonuçlar qa.json dosyasında.

Top.png, Perspective.png, Side.png ve Family_Comparison.png renderları Blender MCP look ile görsel olarak incelendi. Aile yüksekliği ve oyuk oranları tutarlı; yan gövde kesintisiz. Nötr stüdyo aydınlatması yeşil referanstaki güçlü kontrastı tekrarlamaz. Mobil runtime ve Unity/URP doğrulaması bu adımda yapılmadı.

## Sahne düzeni

ArtProduction/AssetPreview/AssetPreview.blend güncellendi; öncesi AssetPreview_Before3x1.blend olarak korundu. AssetPreview sahnesinde Block_1x1, Block_2x1, Block_3x1 koleksiyonları ayrı; kamera/ışık/zemin Preview_Studio içinde. Yeni model tekil BR_Block_3x1_Review sahnesine de bağlı. Block_3x1.blend aynı dosyanın ayrı kayıt kopyasıdır; master 1x1/2x1 dosyaları üzerine yazılmadı.

FBX dışa aktarımı yapılmadı.
