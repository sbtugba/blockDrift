using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlockRift.Core;
namespace BlockRift.App
{
    /// <summary>Kayıt yolu ve Unity API'lerinden bağımsız ilerleme saklama portu.</summary>
    public interface IProgressStore
    {
        LevelProgression.Snapshot Load();
        void Save(LevelProgression.Snapshot snapshot);
    }
    /// <summary>Dışarıdan verilen dosya yoluna sürümlü JSON ilerlemesi yazar.</summary>
    public sealed class JsonProgressStore : IProgressStore
    {
        public string RecoveryNotice { get; private set; }
        private readonly string path;
        /// <summary>Unity persistentDataPath veya test klasörü çağıran tarafından verilir.</summary>
        public JsonProgressStore(string path)
        {
            if(string.IsNullOrWhiteSpace(path))throw new ArgumentException("Kayıt yolu boş olamaz.",nameof(path));
            this.path=Path.GetFullPath(path);
        }

        /// <summary>Yarım tmp dosyası yok sayılır; bozuk ana kayıt varsa sağlam yedek açık bildirimle okunur.</summary>
        public LevelProgression.Snapshot Load()
        {
            RecoveryNotice=null;
            if(File.Exists(path))
            {
                try{return Read(path);}
                catch(FormatException){if(!File.Exists(path+".bak"))throw;}
            }
            else if(!File.Exists(path+".bak"))return null;
            var backup=Read(path+".bak");RecoveryNotice="Ana kayıt eksik/bozuk; sağlam yedek okundu.";return backup;
        }

        private static LevelProgression.Snapshot Read(string file)
        {
            string json=File.ReadAllText(file);
            if(string.IsNullOrWhiteSpace(json))throw new FormatException("İlerleme kaydı boş veya yarım yazılmış.");
            var root=LevelJsonSerializer.ParseJsonObject(json);
            if(!root.TryGetValue("saveSchemaVersion",out var version)||!(version is long v))throw new FormatException("saveSchemaVersion eksik/geçersiz.");
            if(v!=1)throw new NotSupportedException("Desteklenmeyen saveSchemaVersion: "+v);
            if(!root.TryGetValue("completed",out var completed)||!(completed is List<object> ids)||ids.Any(id=>!(id is string))||
                !root.TryGetValue("selected",out var selected)||!(selected is string text))throw new FormatException("İlerleme alanları geçersiz.");
            var result=new LevelProgression.Snapshot(1,ids.Cast<string>(),text);LevelProgression.ValidateSnapshot(result);return result;
        }

        /// <summary>Önce aynı klasörde tmp dosyasını flush eder; mevcut kayıt atomik Replace ve yedekle korunur.</summary>
        public void Save(LevelProgression.Snapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));LevelProgression.ValidateSnapshot(snapshot);
            string json=LevelJsonSerializer.WriteJsonObject(new Dictionary<string,object>{{"saveSchemaVersion",1},{"completed",snapshot.Completed.Cast<object>().ToList()},{"selected",snapshot.Selected}});
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] bytes=Encoding.UTF8.GetBytes(json);
            using(var stream=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write,FileShare.None))
            {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            // Geçersiz ana dosya sağlam yedeğin üstüne yazılmaz.
            if(File.Exists(path))
            {
                try{Read(path);}
                catch(FormatException)
                {
                    if(!File.Exists(path+".bak"))throw;
                    Read(path+".bak");File.Replace(path+".tmp",path,null);return;
                }
                File.Replace(path+".tmp",path,path+".bak");
            }
            else File.Move(path+".tmp",path);
        }
    }
}
