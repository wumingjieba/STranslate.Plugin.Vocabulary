namespace STranslate.Plugin.Vocabulary.Maimemo;

public class Settings
{
    // 将原有的 BookName 改名为 FilePath，或者直接新增一个
    public string FilePath { get; set; } = @"D:\TranslateHelper\words_for_app.txt";
    
    // 其他不用的字段（Token, BookID）可以留着不理，也可以删掉
    public string Token { get; set; } = string.Empty;
}