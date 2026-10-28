# Block Rift — 2026-10-27 hareket kuralı güncellemesi

Bu paket **yalnızca beş dokümantasyonu** günceller. Unity C# Core, Solver, sahne, prefab ve eski bölüm verileri **değiştirilmemiştir**.

## Yeni manuel hareket kuralları
- 1×1: dört yön
- 2×1 / 3×1 / 4×1: yalnız yatay
- 1×2 / 1×3 / 1×4: yalnız dikey
- 2×2: dört yön
- 3×3: kullanımdan kaldırıldı
- Yıldızlı 1×1 hedef parça: dört yön
- Yer çekimi, hareketli engellerin hepsini şekil ne olursa olsun aşağı düşürebilir.

## Kurulum
ZIP içindeki `AGENTS.md`, `docs/` ve `.agents/` yapısını projenin köküne kopyala. Önce var olan dosyaları yedekle ve Git diff ile karşılaştır. `AGENTS.md` kullanıcının sohbet içinde paylaştığı 2026-10-23 açıklamalarını ve yeni level-design yönlendirmesini içerir; başka yerel eklemelerin varsa diff sırasında koru.

Sonraki ayrı iş: Core / Validator / Solver / Hint / Input / level serialization ve testlerini yeni `rulesVersion` ile TDD üzerinden migrate et; eski levels ve testleri yeniden doğrula.
