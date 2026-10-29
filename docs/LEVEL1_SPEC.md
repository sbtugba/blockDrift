# Level 1 — Küçük Aktarma / Movement Rules V2

Güncel uygulama sözleşmesi: 2026-10-28. Onaylı kurallar `GAME_DESIGN.md` §3.3–4; tetikleyici/gravity ayrımı §5. Eski sözleşme [arşivde](archive/development/LEVEL1_RULES_V1_SPEC.md) korunur.

- Core yalnız sekiz şekli kabul eder; 3×3 geçersizdir. `rulesVersion=2`; 1 ve bilinmeyen sürümler yükleme, manuel geçiş ve Solver girişinde reddedilir.
- 2×1/3×1/4×1 yalnız yatay; 1×2/1×3/1×4 yalnız dikey; 1×1/2×2 ve yıldız dört yön. Manuel izin tek Core API'sindedir; Input ve Solver ikinci kural motoru içermez.
- Tek eksenli sürükleme, mesafeden bağımsız bir hamle, NO_OP, ara hücre çarpışması, sınırlar ve hedefte release öncesi kazanma korunur.
- Board 6×6, grid pitch 1. Yıldız (2,5), hedef (5,0). A 1×3 (3,2), B 3×1 (3,1), C 1×2 (2,0), D 1×1 (5,2), E 2×1 (4,3), F 2×2 (0,3). Taşlar (0,2), (4,2), (4,0); tetikleyici yok. Koordinat başlangıcı sol alt.
- Core BoardValidator + aynı Core geçişini kullanan BFS/replay tamamlanmadan görsel entegrasyon kabul edilmez. Yeni Core sonucu 8 hamledir; önceki bağımsız araç sonucu kanıt yerine kullanılmaz.
- Aynı onaylı FBX mesh'leri, mevcut URP materyalleri ve BoardRoot/PiecesRoot kullanılır. Sabit yönelimli dikey prefablar model çocuğunda 90° Y dönüşü kullanır; oyuncu rotasyonu ve ölçek hilesi yoktur.
- Kamera/arka plan/tahta geometrisi ve Blender kaynakları değişmez. Gravity, Undo/Hint, ekonomi/save ve Generator bu görevin kapsamı dışındadır. Trigger'lı level mevcut motor tarafından açıkça desteklenmiyor hatasıyla reddedilir.
- Kabul: sekiz şeklin izinli/yasak yönleri, sürüm retleri, Solver minimum/replay/INCOMPLETE, kesin koordinatlar, prefab/materyal bağları, mouse/touch, yasak görsel hareket, Restart ve anında Win. Gerçek Game View PNG ve test kayıtları [teslim raporunda](LEVEL1_REPORT.md).
