using Microsoft.Extensions.Logging;
using STranslate.Plugin.Vocabulary.Maimemo.View;
using STranslate.Plugin.Vocabulary.Maimemo.ViewModel;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace STranslate.Plugin.Vocabulary.Maimemo;

public class Main : IVocabularyPlugin
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public void Dispose() => _viewModel?.Dispose();

    // 这里是核心：被改造后的保存逻辑
    public async Task<VocabularyResult> SaveAsync(string text, CancellationToken cancellationToken)
    {
        var result = new VocabularyResult();
        var startTime = DateTime.Now;

        try
        {
            // 1. 你的“秘密路径”（请确保 D 盘有 TranslateHelper 这个文件夹）
            string filePath = @"D:\MyCode\TranslateHelper\plugin_words_for_app.txt";
            
            // 2. 清理单词并转小写
            var newText = text.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(newText))
            {
                return result.Fail("单词为空");
            }

            // 3. 准备写入的内容（单词 + 换行符）
            string content = newText + Environment.NewLine;

            // 4. 异步追加写入本地文件 (文件不存在会自动创建)
            await File.AppendAllTextAsync(filePath, content, cancellationToken);

            // 5. 写入成功后，在屏幕右下角弹个绿色的提示
            Context.Notification.Show("STranslate", $"已成功写入本地: {newText}");
            Context.Logger.LogInformation($"本地生词写入成功: {newText}");
            
            return result;
        }
        catch (Exception ex)
        {
            // 如果出错（比如文件被占用、没权限），直接报错
            var msg = $"写入本地文件失败: {ex.Message}";
            Context.Notification.Show("STranslate", msg);
            Context.Logger.LogInformation(msg);
            return result.Fail(ex.Message);
        }
        finally
        {
            result.Duration = DateTime.Now - startTime;
        }
    }
}