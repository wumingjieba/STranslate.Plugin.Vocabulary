using Microsoft.Extensions.Logging;
using STranslate.Plugin.Vocabulary.Maimemo.View;
using STranslate.Plugin.Vocabulary.Maimemo.ViewModel;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using System.Windows.Interop;

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

    public async Task<VocabularyResult> SaveAsync(string text, CancellationToken cancellationToken)
    {
        var result = new VocabularyResult();
        var startTime = DateTime.Now;

        const string url = "https://open.maimemo.com/open/api/v1/notepads";
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Settings.BookID, "BookId不可为空");
            var newUrl = $"{url}/{Settings.BookID}";
            var newText = text.ToLower().Trim();

            var options = new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {Settings.Token}" }
                }
            };

            var resultJson = await Context.HttpService.GetAsync(newUrl, options, cancellationToken);
            var resultList = GetResult(resultJson);

            if (resultList.Contains(newText))
            {
                var msg = $"{Context.MetaData.Name}生词本 {Settings.BookName}({Settings.BookID}) 已存在单词: {newText}";
                Context.Notification.Show("STranslate", msg);
                Context.Logger.LogInformation(msg);
                return result.Fail(resultJson);
            }
            else
            {
                resultList.Add(newText);
            }

            var content = new
            {
                notepad = new
                {
                    status = "PUBLISHED",
                    content = string.Join(",", resultList),
                    title = Settings.BookName,
                    brief = "create by stranslate",
                    tags = new[] { "其他" }
                }
            };

            var resp = await Context.HttpService.PostAsync(newUrl, content, options, cancellationToken);
            if (!GetFinalResult(resp))
            {
                var msg = $"{Context.MetaData.Name}保存至生词本{Settings.BookName}({Settings.BookID})失败, Raw: {resp}";

                Context.Notification.Show("STranslate", msg);
                Context.Logger.LogInformation(msg);
                return result.Fail(resp);
            }

            // 二次检查结果是否真的插入了
            resultJson = await Context.HttpService.GetAsync(newUrl, options, cancellationToken);
            resultList = GetResult(resultJson);

            if (!resultList.Contains(newText))
            {
                var msg = $"{Context.MetaData.Name}保存至生词本{Settings.BookName}({Settings.BookID})失败, 原因是二次检查时发现服务不接受该单词: {newText}";
                Context.Notification.Show("STranslate", msg);
                Context.Logger.LogInformation(msg);
                return result.Fail(resultJson);
            }
            return result;
        }
        catch (Exception ex)
        {
            var msg = $"{Context.MetaData.Name}保存至生词本{Settings.BookName}({Settings.BookID})失败, 请检查配置保存后重试\n错误信息: {ex.Message}";
            Context.Notification.Show("STranslate", msg);
            Context.Logger.LogInformation(msg);
            return result.Fail(ex.Message);
        }
        finally
        {
            result.Duration = DateTime.Now - startTime;
        }
    }

    private static bool GetFinalResult(string json)
    {
        var jObject = JsonNode.Parse(json);
        if (jObject?["success"]?.ToString() != "true")
            throw new Exception($"接口回复: {json}");

        return true;
    }

    public static List<string> GetResult(string json)
    {
        var jObject = JsonNode.Parse(json);
        var resultList = new List<string>();
        if (jObject?["data"]?["notepad"]?["list"] is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not JsonNode obj) continue;
                if (obj["type"]?.ToString() == "WORD")
                {
                    if (obj["word"]?.ToString() is string str && !string.IsNullOrWhiteSpace(str))
                        resultList.Add(str);
                }
            }
        }

        return resultList;
    }
}