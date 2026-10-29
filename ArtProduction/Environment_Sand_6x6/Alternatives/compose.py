import json,collections
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
p=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',24)
def sheet(name,views):
 c=Image.new('RGB',(2100,len(views)*600+55),'#eee9e1');d=ImageDraw.Draw(c)
 for col,label in enumerate('ABC'):
  d.text((col*700+20,12),'Alternatif '+label,fill='#332b25',font=font)
  for row,(view,title) in enumerate(views):
   im=Image.open(p/label/(view+'.png')).convert('RGBA');im.thumbnail((680,550))
   bg=Image.new('RGBA',im.size,(238,233,225,255));bg.alpha_composite(im)
   c.paste(bg.convert('RGB'),(col*700+(700-im.width)//2,row*600+85))
   d.text((col*700+20,row*600+52),title,fill='#332b25',font=font)
 c.save(p/name)
sheet('Alternatives.png',[('Top','Üstten'),('Perspective40','40° perspektif'),('LargeCrack','Büyük çatlak'),('MicroCrack','Mikro çatlak')])
sheet('ThreeEdgeSections.png',[('Edge1','Ön kenar / sol'),('Edge2','Ön kenar / sağ'),('Edge3','Sağ kenar')])
r=json.loads((p/'results.json').read_text());contact=json.loads((p/'contact_verification.json').read_text())
lines=['# Üç kum alternatifi','','Mevcut onaylı mesh assetler korunarak üç ayrı review sahnesi üretildi; ana kum modeli üzerine yazılmadı. Üç alternatif ayrı mesh/material datablocklarına sahiptir. Unity dosyalarına yazılmadı; export yapılmadı. İşlem öncesi Before_Alternatives yedeği klasördedir.','','## Bağımsız çatlak sistemi','','Her patika bağımsız başlangıç noktası, açı, adım dönüşü, uzunluk, genişlik, derinlik, kesinti ve taper üretir. Döndürülmüş/ölçeklenmiş şablon çatlak yoktur. Büyük/orta/mikro sınıfları farklı aralıklar kullanır. Küçük çatlaklar normal/albedo haritasındadır; gerçek geometrik oyuk olarak iddia edilmez. Bazı yürüyüşler tahta koruma sınırı nedeniyle kısalabilir. Parametreler ve üretilen bütün noktalar A/B/C parameters.json dosyalarında saklıdır.','','| Alternatif | Seed | Büyük patika | Orta patika | Mikro patika |','|---|---|---|---|---|']
for a in r:
 counts=collections.Counter(c['category'] for c in a['paths']);lines.append(f"| {a['alternative']} | {a['seed']} | {counts['large']} | {counts['medium']} | {counts['micro']} |")
lines+=['','Patika sayıları dalları ayrı sayar. Dağılım sabit aralıklı değildir; bazı alanlar temiz bırakılır. Mikro detaylar tam tahta görünümünde bilinçli olarak çok hafiftir; yakın çekimde de sınırlı okunurluğa sahiptir. 1024² doku bütçesi bu ayrıntı seviyesini sınırlar.','','## Gerçek kuma gömülme','','Üç ayrı bölgede kumun iç sınırı, hücrenin ölçülen alt yan yüzeyine ray-cast ile oturtuldu. Dikiş açıklığını kaldırmak için kum yalnızca alt yan yüzey içine 0.00035 birim girer. Tahta veya hücre vertexleri değiştirilmedi. Bu kasıtlı alt-yan kesişme, hareketli blok çakışmasıyla aynı değildir.','','Her alternatifte 49 kum–hücre üçgen çifti alt yan yüzeylerde kesişir. Bu bölgelerin en yüksek kum vertexi Z=-0.01812344; hücre üst düzlemi Z=-0.001, blok yerleşim hacmi Z>=0. Pozitif dikey ayırma payı üst yüzey ve hareketli blokların kapanmasını dışlar. Kumun tümü negatif Z altında kalır. Bu doğrulama yalnızca gölgeye dayanmaz; gerçek BVH kesişimiyle alt yan sarılma gösterildi.','','Üç kenar yakın çekimi ThreeEdgeSections.png içinde gösterilir. Geometrik temas profili alternatifler arasında sabit tutuldu; alternatifler çatlak dağılımını karşılaştırır. Kamera çok alçak olduğu için arazi dış sınırının ötesinde görüntü arka planı görülebilir; bu oyun kamerası kadrajı değildir.','','Her alternatif 1.728 vertex / 3.168 üçgen, tek kum materyali ve iki adet 1024² harita kullanır. Gameplay Collider eklenmedi. Mevcut grid, asset konumları ve ölçekleri korundu.','','## Yürütülen kontroller ve sınırlar','','alternatives.py Blender MCP ile çalıştırıldı; araç bağlantısı son yanıtta veri alamasa da üç klasördeki renderlar, results.json ve kaydedilmiş .blend tamamlandı. Ardından canlı Blender üzerinden BVH temas doğrulaması yapıldı ve contact_verification.json kaydedildi. Öncesi/sonrası kaynak mesh, normal, UV, materyal ve Transform parmak izleri aynı doğrulandı. compose.py paketli Python/Pillow ile çalıştırıldı. Büyük ve mikro çatlak ile kenar yakın çekimleri görsel olarak incelendi.','','Unity import, cihaz performansı ve oyun kuralları test edilmedi. Bu görev yalnızca Blender ortamı için üç görsel seçeneği sunar; hiçbir seçenek onaylanmış sayılmadı. Genel şekilli tahta üreticisi bu göreve eklenmedi.']
(p/'REPORT.md').write_text('\n'.join(lines),encoding='utf-8')
