# 1x1 Stone — final refine

AssetPreview.blend içindeki Obstacle_1x1_Stone_Export güncellendi. İşlem öncesi yedek Before_FinalRefine.blend; son sürüm ayrıca Obstacle_1x1_Stone_Final.blend olarak saklanır. Referans plastik bloklar, diğer modeller ve asıl Preview_Studio değişmedi. Core/GDD veya Unity Assets değişikliği ve export yok.

Merkezi dudak/duvarın 210 vertexine en fazla 0.004537 birimlik küçük yerel düzensizlik eklendi. Aşınmış dış köşeler, dış silüet ve taban değiştirilmedi. Büyük yüzeylerdeki gereksiz üçgen normal geçişleri temizlenirken bilinçli yontu düzlemleri ayrı normal gruplarında tutuldu. Vertex/üçgen eklenmedi: 1.612 vertex, 3.220 üçgen.

Çatlakları taşıyan UV yüzlerinden ve mesh kanallarından belirlenen 261 vertex kilitlendi: ölçülen yer değiştirme sıfır. UV koordinatları aynı. Yüzey/normal görüntülerinin canlı kaynak piksel verisi birebir korunarak Final klasörüne kaydedildi ve pack edildi. İlk görüntü kopyasında packed/cache verisi farkı render kontrolünde yakalanıp düzeltildi; nihai görüntüler orijinal canlı piksel verisiyle sıfır hatalı karşılaştırıldı. Kaydedilmiş PNG'ler ayrıca yeniden yüklenerek kontrol edildi; hata qa.json içinde.

## Materyal / Unity eşlemesi

BR_Stone_Final_URP materyalinde ayrı Base Color Tint, Roughness ve Normal Scale kontrolleri etiketlendi. Tint lineer (0.085,0.085,0.085), Metallic=0, Roughness=0.87; URP Lit karşılığı Smoothness yaklaşık 0.13. Normal Scale=0.85. Ana renk/roughness geometriye yazılmaz.

Stone_1x1_Surface.png lineer gri yüzey çarpanıdır: URP Base Map'te sRGB kapalı, Base Color bağımsız ayarlanır. Stone_1x1_Normal.png tangent +Y normaldir: Unity Texture Type=Normal map, sRGB kapalı. Haritalar 512x512, .blend içinde packed ve ayrıca PNG olarak mevcut. Blender materyal node ağacı Unity materyaline otomatik dönüştürülmüş sayılmaz; Unity import/shader runtime henüz denenmedi.

## Gerçekleştirilen doğrulama

Blender MCP execute_blender_code ile refine_final.py ve render_and_save.py çalıştırıldı. Geometri: tek bağlı kapalı manifold, sıfır manifold dışı kenar, sıfır dejenere yüzey, pozitif hacim. BVH: vertex paylaşmayan üçgenler arasında sıfır kesişim. Köşe normalleri sonlu ve birim; Mikk tangentler sonlu. Ölçü 1x1x0.30 ve yerel taban-merkezi pivot korundu. Transform aynı; önizleme konumu (3.5,4,0), ölçek (1,1,1), rotasyon sıfır.

Üstten ve 40 derece perspektif render alındı, Block_1x1 ile aynı birim ölçekte yan yana karşılaştırıldı. İki karşılaştırma objesinin ölçekleri (1,1,1). Son renderlar Blender MCP look ile incelendi. Detaylı sonuçlar qa.json içinde. Mobil cihaz performansı ölçülmedi.

Tekrar üretmek için yedek sahneyi açıp refine_final.py, ardından render_and_save.py çalıştırılabilir. İlk script final işaretli modelde tekrar uygulamayı ve yedek üzerine yazmayı reddeder. Mevcut onaylı modellerin üretim kaynakları korunmuştur.
