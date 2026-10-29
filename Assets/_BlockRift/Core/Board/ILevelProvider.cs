namespace BlockRift.Core
{
    /// <summary>Bölüm verisine erişim sözleşmesidir; Unity Resources veya dosya sisteminden bağımsızdır.</summary>
    public interface ILevelProvider
    {
        /// <summary>Verilen bölüm kimliğiyle doğrulanmış LevelDefinition döndürür.</summary>
        LevelDefinition GetLevel(string levelId);
    }
}

