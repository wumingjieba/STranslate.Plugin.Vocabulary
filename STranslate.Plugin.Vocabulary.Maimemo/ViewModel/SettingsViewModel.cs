using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // 必须有这个，否则 [RelayCommand] 无效
using Microsoft.Win32; // 必须有这个，否则找不到 SaveFileDialog

namespace STranslate.Plugin.Vocabulary.Maimemo.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;
        // 初始化界面显示的路径
        FilePath = _settings.FilePath;
        PropertyChanged += OnSettingsViewModelPropertyChanged;
    }

    private void OnSettingsViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilePath))
        {
            _settings.FilePath = FilePath;
            _context.SaveSettingStorage<Settings>();
        }
    }

    [ObservableProperty] public partial string FilePath { get; set; }

    // --- 核心：增加选择文件命令 ---
    [RelayCommand]
    private void SelectPath()
    {
        // 调起 Windows 系统的文件选择/保存框
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择或创建你的生词本文件"
        };

        if (dialog.ShowDialog() == true)
        {
            // 将选中的路径同步回输入框
            FilePath = dialog.FileName;
        }
    }
} // 确保类闭合
    public void Dispose() => PropertyChanged -= OnSettingsViewModelPropertyChanged;
}