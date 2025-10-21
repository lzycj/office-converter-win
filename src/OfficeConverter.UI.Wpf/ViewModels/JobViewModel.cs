using OfficeConverter.Core.Runtime;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OfficeConverter.UI.Wpf.ViewModels;

public class JobViewModel : INotifyPropertyChanged
{
    private string _fileName = string.Empty;
    private JobStatus _status;
    private string? _progress;
    private string? _outputPath;
    private string? _logPath;

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public JobStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string? Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    public string? OutputPath
    {
        get => _outputPath;
        set { _outputPath = value; OnPropertyChanged(); }
    }

    public string? LogPath
    {
        get => _logPath;
        set { _logPath = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
