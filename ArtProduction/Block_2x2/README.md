# Block_2x2 — inceleme sürümü

Mevcut AssetPreview.blend içinde yalnız Block_2x2 üretildi. Collection: Block_2x2, obje: Block_2x2_Export. Ortak sahnenin önceki hali AssetPreview_Before2x2.blend olarak korundu. Block_2x2.blend ayrı kayıt kopyasıdır. Üretim ve doğrulama kodları bu klasörde korunur; önceki modellerin üretim kodları değiştirilmedi.

Tek kesintisiz gövde üzerinde 2x2 düzeninde dört yükseltilmiş panel ve dört geometrik baklava oyması. Paneller onaylı 3x1'in 1x1-master tabanlı panelinden ölçeklenmeden alınır. Toplam X/Y/Z=2/2/0.30. Merkezler (-.5,-.5),(.5,-.5),(-.5,.5),(.5,.5); merkez aralıkları iki yönde de 1 birim. Panel taban aralıkları 0.012, düz tepe aralıkları 0.160 birim; iki eksende eşit. Oyuk derinliği 0.040.

Yerel pivot taban merkezinde; ölçek (1,1,1), rotasyon sıfır. Sergileme konumu (3.5,2,0); diğer modellerle kesişmez. Nötr gri materyal bölgesi ve özel köşe normalleri aileyle aynı. 8.564 Blender vertex, 17.124 üçgen; UV mevcut. Unity import vertex sayısı normal/UV ayrımları nedeniyle farklı olabilir. Mobil cihaz performansı ölçülmedi.

## Yürütülen doğrulama

Blender MCP execute_blender_code üzerinden generate_block_2x2.py ve verify_block_2x2.py yürütüldü. Bmesh: 1 bağlı kapalı manifold, sıfır manifold dışı kenar ve dejenere yüzey; pozitif hacim. BVH: vertex paylaşmayan üçgenler arasında sıfır kesişim. Dört panelin her birinin 2.016 vertex koordinatı onaylı panelle karşılaştırıldı; normalize edilmiş maksimum fark dört panelde de sıfır.

Ray testleri dört oyuk tabanında Z=0.260, dört düz tepede Z=0.300, yatay/dikey ayırıcılar ve merkezde Z=0.220 doğruladı. Merkez çevresinde dört farklı yarıçapta karşılıklı diyagonal örnekler alındı; en büyük yükseklik farkı yaklaşık 1.19e-7. Merkez gövde yüzeyiyle kapalı, delik değil. Normaller sonlu ve birim uzunlukta. Pivot, birim ölçek, sıfır rotasyon ve diğer modellerden ayrı yerleşim doğrulandı.

Üretim öncesi/sonrası mevcut sahne objelerinin mesh/topoloji/UV/köşe normali/smooth/materyal referansı, Transform, koleksiyon üyeliği ve kamera/ışık ayarları karşılaştırıldı; değişmedi. 1x1 master dosyasının SHA-256 özeti aynı. Preview_Studio ayarları orijinal koleksiyona dokunmadan ayrı inceleme sahnelerine kopyalandı. Oyun kuralları, Core ve Unity Assets dosyaları değiştirilmedi.

Top.png, Perspective.png (40 derece), Side.png, Center_Closeup.png ve Compare_1x1_2x1_2x2.png Blender MCP look ile görsel olarak incelendi. Merkezde dört oval köşenin arasında simetrik bir gövde yüzeyi görünür; ince ayırıcı çizgiler korunur. Nötr aydınlatma yeşil referansın renk/kontrastını kopyalamaz. İnceleme için kullanıcı onayı bekleniyor.

FBX export yapılmadı. Sayısal sonuçlar qa.json dosyasında.
