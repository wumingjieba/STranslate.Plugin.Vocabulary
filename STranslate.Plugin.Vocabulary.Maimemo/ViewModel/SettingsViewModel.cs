using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.ComponentModel;
using System;

namespace STranslate.Plugin.Vocabulary.Maimemo.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        // 初始化读取路径
        FilePath = _settings.FilePath;

        PropertyChanged += OnSettingsViewModelPropertyChanged;
    }

    private void OnSettingsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilePath))
        {
            _settings.FilePath = FilePath;
            _context.SaveSettingStorage<Settings>();
        }
    }

    public void Dispose() => PropertyChanged -= OnSettingsViewModelPropertyChanged;

    [ObservableProperty] 
    public partial string FilePath { get; set; }

    [RelayCommand]
    private void SelectPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择或创建你的生词本文件"
        };

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
        }
    }
}