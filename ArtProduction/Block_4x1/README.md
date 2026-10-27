# Block_4x1 — inceleme sürümü

AssetPreview.blend içinde Block_4x1 koleksiyonuna Block_4x1_Export eklendi. Önceki ortak sahne AssetPreview_Before4x1.blend olarak yedeklendi. Block_4x1.blend ayrıca kayıt kopyasıdır. Yalnız yeni 4x1 modeli üretildi; Unity Assets ve Core değiştirilmedi. GDD mekanik değişikliği yok.

Tek kesintisiz gövde, dört yükseltilmiş panel, dört baklava oyması. Paneller mevcut Block_3x1 mesh'inin ilk panelinden ölçeklenmeden doğrudan alınmıştır. Her panelin normalize edilmiş 2.016 vertex koordinatı 3x1 ile birebir aynı; dört panel için ölçülen maksimum fark 0. Hücre merkezleri yerel X=-1.5,-0.5,0.5,1.5, merkez aralığı tam 1 birim. Panel tabanları arası 0.012 ve düz tepeler arası 0.160 birim, 3x1 ile aynı. Oyuk derinliği 0.040; tepe Z=0.300, oyuk tabanı Z=0.260, üç ayırıcı merkezinde Z=0.220.

Ölçüler X/Y/Z=4/1/0.30. Yerel pivot taban merkezinde; birim ölçek, sıfır rotasyon. Ortak önizleme konumu (0,4,0); gelecekte bağımsız export sırasında bu sergileme konumu ile yerel pivot ayrı ele alınmalı. 8.554 Blender vertex, 17.104 üçgen. UV ve nötr gri materyal mevcut. Materyal mevcut 3x1 ile paylaşılır; shader değiştirilmedi. Mobil runtime performansı henüz ölçülmedi.

## Gerçekleştirilen kontroller

Blender MCP execute_blender_code üzerinden generate_block_4x1.py ve verify_block_4x1.py çalıştırıldı. Bmesh: tek bağlı kapalı manifold, sıfır dejenere yüzey, pozitif hacim. BVH: vertex paylaşmayan üçgenler arasında sıfır kesişim. Ray testleri dört oyuk/düz tepe ve üç ayırıcı yüksekliğini doğruladı. Normal vektörleri sonlu ve birim uzunlukta; yüz yönleri üretim sırasında yeniden hesaplandı.

Mevcut 1x1/2x1/3x1 ve Preview_Studio objeleri yedek dosyadan yüklenen durumla karşılaştırıldı. Mesh koordinatları, topoloji, smooth bayrakları, materyal bölgeleri, UV, köşe normalleri, shader değerleri ve dünya Transform'ları aynı. Kamera ve ışık parametreleri de aynı. Karşılaştırma renderı ayrı objeler/kamera/ışıklarla oluşturuldu; ana Preview_Studio değişmedi. İlk koruma denetiminde karşılaştırma sahnesine doğrudan bağlanan 3x1'in ek koleksiyon üyeliği yakalandı; bağlantı kaldırılıp aynı mesh'i paylaşan ayrı karşılaştırma objesi kullanıldı. Nihai yedek karşılaştırması PASS.

Top.png, Perspective.png (40 derece), Side.png ve Compare_3x1_4x1.png Blender MCP look ile incelendi. Üç kanal ve gövde-panel çevre çizgisi korunuyor. Nötr materyal/aydınlatma, yeşil referansın kontrastıyla birebir eşleşme iddiası taşımaz.

qa.json makine tarafından okunabilir sonuçları içerir. FBX export yapılmadı. Kullanıcı değerlendirmesi bekleniyor.
