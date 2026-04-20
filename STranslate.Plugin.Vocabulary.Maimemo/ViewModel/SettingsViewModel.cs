using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32; // 必须引用这个来调用系统文件窗口

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
        var openFileDialog = new SaveFileDialog // 用 SaveFileDialog 可以方便创建新文件
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "选择或创建你的生词本文件"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            FilePath = openFileDialog.FileName;
        }
    }

    public void Dispose() => PropertyChanged -= OnSettingsViewModelPropertyChanged;
}