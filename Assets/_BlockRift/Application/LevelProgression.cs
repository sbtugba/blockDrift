using System;
using System.Collections.Generic;
using System.Linq;
using BlockRift.Core;

namespace BlockRift.App
{
    /// <summary>Yayın sırası ve oyuncu ilerlemesi; sahne, dosya, Unity ve hareket kuralı içermez.</summary>
    public sealed class LevelProgression
    {
        /// <summary>Tahta/ekonomi içermeyen sürümlü ilerleme görüntüsü.</summary>
        public sealed class Snapshot
        {
            public int SaveSchemaVersion
            {
                get;
            }
            public IReadOnlyList<string> Completed
            {
                get;
            }
            public string Selected
            {
                get;
            }
            public Snapshot(int version, IEnumerable<string> completed, string selected)
            {
                SaveSchemaVersion = version;
                Completed = new List<string>(completed).AsReadOnly();
                Selected = selected;
            }
        }
        public IReadOnlyList<string> Levels
        {
            get;
        }
        public Snapshot State
        {
            get; private set;
        }
        public string ReconciliationNotice
        {
            get; private set;
        }
        private GameSession active;

        /// <summary>Manifest sırasını okur; kaldırılmış/kilitli kayıtları deterministik biçimde uzlaştırır.</summary>
        public LevelProgression(string manifest, Snapshot saved = null)
        {
            var root = LevelJsonSerializer.ParseJsonObject(manifest);
            if (!root.TryGetValue("schemaVersion", out var version) || !(version is long v) || v != 1 ||
                !root.TryGetValue("levels", out var values) || !(values is List<object> ids) || ids.Count == 0 ||
                ids.Any(id => !(id is string text) || string.IsNullOrWhiteSpace(text)) || ids.Cast<string>().Distinct(StringComparer.Ordinal).Count() != ids.Count)
                throw new FormatException("Geçersiz yayın manifesti; schemaVersion=1 ve benzersiz bölüm ID'leri gerekli.");
            Levels = ids.Cast<string>().ToList().AsReadOnly();
            if (saved != null)
                ValidateSnapshot(saved);
            var complete = new List<string>();
            foreach (var id in Levels)
            {
                if (saved == null || !saved.Completed.Contains(id))
                    break;
                complete.Add(id);
            }
            State = new Snapshot(1, complete, Levels[0]);
            string selected = saved?.Selected;
            if (!IsUnlocked(selected))
                selected = Levels.FirstOrDefault(id => !complete.Contains(id)) ?? Levels.Last();
            State = new Snapshot(1, complete, selected);
            if (saved != null && (!saved.Completed.SequenceEqual(complete) || saved.Selected != selected))
                ReconciliationNotice = "Kayıt güncel katalogla uzlaştırıldı; kaldırılmış/kilitli ID'ler korunmadı.";
        }

        /// <summary>Kayıt sürümü ve temel içerik bozuksa sessiz başlangıç yapılmaz.</summary>
        public static void ValidateSnapshot(Snapshot saved)
        {
            if (saved.SaveSchemaVersion != 1)
                throw new NotSupportedException("Desteklenmeyen saveSchemaVersion: " + saved.SaveSchemaVersion);
            if (string.IsNullOrWhiteSpace(saved.Selected) || saved.Completed.Any(string.IsNullOrWhiteSpace) || saved.Completed.Distinct(StringComparer.Ordinal).Count() != saved.Completed.Count)
                throw new FormatException("Geçersiz ilerleme kaydı.");
        }

        /// <summary>Bir sonraki bölüm yalnız önceki tamamlandığında açıktır.</summary>
        public bool IsUnlocked(string id)
        {
            int index = Levels.ToList().IndexOf(id);
            return index >= 0 && (index == 0 || State.Completed.Contains(Levels[index - 1]));
        }

        /// <summary>Yükleme ve kayıt başarıyla bitmeden seçimi değiştirmez.</summary>
        public Snapshot Selection(string id)
        {
            if (!Levels.Contains(id))
                throw new ArgumentException("Yayın kataloğunda olmayan bölüm: " + id, nameof(id));
            if (!IsUnlocked(id))
                throw new InvalidOperationException("Bölüm kilitli: " + id);
            return new Snapshot(1, State.Completed, id);
        }

        /// <summary>Başarıyla açılan mevcut GameSession kimliğini authoritative bildirim kaynağı yapar.</summary>
        public void Activate(GameSession session, Snapshot selected)
        {
            if (session == null || selected.Selected != session.Level.Id || !selected.Completed.SequenceEqual(State.Completed))
                throw new InvalidOperationException("Oturum/seçim uyuşmuyor.");
            Selection(selected.Selected);
            active = session;
            State = selected;
        }

        /// <summary>Yalnız aktif, gerçekten kazanmış ve daha önce kaydedilmemiş oturum için aday kayıt üretir.</summary>
        public Snapshot Completion(GameSession session)
        {
            if (!ReferenceEquals(session, active) || session == null || !session.Won || State.Completed.Contains(session.Level.Id))
                return null;
            return new Snapshot(1, State.Completed.Concat(new[] { session.Level.Id }), State.Selected);
        }

        /// <summary>Disk yazımı başarılı olduktan sonra aynı kazanma kanıtını bellekte kesinleştirir.</summary>
        public void AcceptCompletion(GameSession session, Snapshot completed)
        {
            var expected = Completion(session);
            if (expected == null || completed == null || expected.Selected != completed.Selected || !expected.Completed.SequenceEqual(completed.Completed))
                throw new InvalidOperationException("Geçersiz veya eski kazanma bildirimi.");
            State = completed;
        }

        /// <summary>Sonraki yayın ID'sini döndürür; son bölümde sayıdan yeni ID üretmez.</summary>
        public string NextLevel()
        {
            int next = Levels.ToList().IndexOf(State.Selected) + 1;
            return next < Levels.Count && IsUnlocked(Levels[next]) ? Levels[next] : null;
        }
    }
}
