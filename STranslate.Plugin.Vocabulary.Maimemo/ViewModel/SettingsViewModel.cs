using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace STranslate.Plugin.Vocabulary.Maimemo.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        BookName = _settings.BookName;
        BookID = _settings.BookID;
        Token = _settings.Token;

        PropertyChanged += OnSettingsViewModelPropertyChanged;
    }

    private void OnSettingsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BookName):
                _settings.BookName = BookName;
                break;
                case nameof(BookID):
                    _settings.BookID = BookID;
                break;
                case nameof(Token):
                    _settings.Token = Token;
                break;
            default:
                break;
        }

        _context.SaveSettingStorage<Settings>();
    }

    public void Dispose() => PropertyChanged -= OnSettingsViewModelPropertyChanged;

    [ObservableProperty] public partial string BookName { get; set;  }
    [ObservableProperty] public partial string BookID { get; set;  }
    [ObservableProperty] public partial string Token { get; set;  }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = string.Empty;


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;
        BookID = string.Empty;

        const string url = "https://open.maimemo.com/open/api/v1/notepads";
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(_settings.BookName, $"生词本服务: {_context.MetaData.Name} 中生词本名称为空");

            var options = new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {_settings.Token}" }
                }
            };
            var bookList = await _context.HttpService.GetAsync(url, options, cancellationToken);
            var bookId = GetIdByNameInArray(bookList, _settings.BookName);
            if (string.IsNullOrWhiteSpace(bookId))
            {
                var content = new
                {
                    notepad = new
                    {
                        status = "PUBLISHED",
                        content = "first",
                        title = _settings.BookName,
                        brief = "create by stranslate",
                        tags = new[] { "其他" }
                    }
                };
                var resp = await _context.HttpService.PostAsync(url, content, options, cancellationToken);
                bookId = GetIdByName(resp);
                ArgumentException.ThrowIfNullOrWhiteSpace(bookId,
                    $"创建生词本服务: {_context.MetaData.Name}->生词本名称: {_settings.BookName} 失败, 接口回复: {resp}");
            }
            _context.Snackbar.ShowSuccess("获取成功");
            BookID = bookId;
            ErrorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            _context.Snackbar.ShowError("获取失败");
            var msg = $"检查生词本服务： {_context.MetaData.Name} 配置失败, {ex.Message}";
            ErrorMessage = msg;
            _context.Logger.LogError(msg);
            return false;
        }
    }

    private static string GetIdByName(string json)
    {
        var jObject = JsonNode.Parse(json);
        return jObject?["data"]?["notepad"]?["id"]?.ToString() ?? string.Empty;
    }

    private static string GetIdByNameInArray(string json, string name)
    {
        var jObject = JsonNode.Parse(json);
        if (jObject?["data"]?["notepads"] is not JsonArray jArray) return string.Empty;

        foreach (var jToken in jArray)
        {
            if (jToken is not JsonNode item) continue;

            if (item["title"]?.ToString() == name)
                return item["id"]?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}
