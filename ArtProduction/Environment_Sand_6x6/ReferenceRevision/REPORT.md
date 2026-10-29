# Referans esaslı sığ kum yuvası revizyonu

Yeni sürüm Review_Sand_Alternative_Reference sahnesinde korunur. Önceki sürümler ve bütün onaylı mesh assetler değişmeden kalır. İşlem öncesi tam Blender yedeği Before_Alternatives_*.blend dosyasındadır. Unity dosyası yazılmadı ve export yapılmadı.

## Gerçek mesh çöküntüsü

Tahta çevresindeki halka yükseklikleri yeniden tanımlandı. Kum kenarı ortalama Z=-0.044, sığ oluk dibi Z=-0.048, dış kum Z≈-0.016 … -0.020. Oluk dış yüzeyden yaklaşık 0.032 birim aşağıdadır. Dikey duvar ve yüksek kum tepesi yoktur. Bütün çevrede yumuşak eğim ve düşük genlikli organik dalga kullanılır. Hücreler arasındaki bej destek aynen korunur.

Destek tepesi Z=-0.061 ve tabanı Z=-0.067, iç kum kenarı Z≈-0.044 altında kaldığından görünür alt platform kenarı gizlenir. İç sınır, hücre alt yan yüzeyine ray-cast ile oturtulur; dikiş açıklığını önlemek için sadece alt yan yüzeyde 0.0002 birim kontrollü giriş vardır. Hücre geometrisi değiştirilmedi.

Gerçek BVH kontrolü 506 kum–alt hücre yan yüzeyi üçgen çifti kesişimi gösterdi. Temas eden kum üçgenlerinin en yüksek vertexi Z=-0.04118552. Kumun tamamında en yüksek Z=-0.01319922; hücre üstü Z=-0.001 ve blok yerleşim altı Z=0. Dolayısıyla temas yalnızca alt yan yüzeylerle sınırlıdır. Kum üst hücre düzlemine ve hareketli blok hacmine erişmez. Üst yüzey serbestliği pozitif Z ayırma payıyla doğrulandı; fizik motoru veya Unity collider testi değildir. Gameplay collider eklenmedi.

## Çatlak ağları

Seed 57281 ile bağımsız 3 ana uzun ağ üretildi: yaklaşık 2.84, 1.86 ve 2.21 grid birimi. Bunlara 0.32–0.53 birim dallar, 2 orta ve 7 mikro patika eşlik eder. Her patikada bağımsız yön, dönüş, genişlik, derinlik ve taper vardır. Ağlar tahta koruma alanının dışında kalır. Ana patikalar daha köşeli parçalara sadeleştirildi; V profilini andıran height/normal eğimi, koyu iç yüzey ve hafif aşınmış kenar tonları kullanıldı. Çatlaklar gerçek mesh oyukları değildir; 1024² albedo ve normal haritalarıyla sağlanan yüzey kırılmasıdır. Yakın çekimde doku çözünürlüğü görünür ayrıntıyı sınırlar.

## Koruma ve teknik bütçe

1.728 vertex, 3.168 triangle, tek materyal ve iki 1024² harita. Hücre geometrisi, renkleri, normal/UV verileri, konumları ve ölçekleri kaynak mesh parmak izleriyle korundu. Önceki kum sürümlerinin meshleri üzerine yazılmadı. Yalnızca yeni kum sürümü ve onun ayrı materyali üretildi. Cihaz performansı ölçülmedi.

## Yürütülen işlemler

build_reference_revision.py Blender MCP ile çalıştırıldı; şablon kodundaki üretim bölümü sığ yuva profiliyle ve daha uzun patika parametreleriyle değiştirildi. refine_reference_cracks.py ile yeni sürümün albedo/normal profili rafine edildi. Top, Perspective40, üç Edge ve LargeCrack renderları Blender ile üretildi. Sayısal halka yüksekliği ve BVH temas ölçümleri measurements.json / contact_check.json içinde. compose_report.py paketli Python/Pillow ile çalıştırıldı. Üstten, kenar ve çatlak yakın çekimleri görsel olarak incelendi.

Hiçbir görüntü Unity Game View değildir. Referanstaki UI, renkli blok dizilimi veya dış dekorlar yeniden üretilmedi; bu revizyon yalnızca kum tasarımını ele alır. Kaynak tasarımın mekanikleri/GDD/Core değiştirilmedi. Genel şekilli tahta üretimi bu revizyonun kapsamında uygulanmadı.
