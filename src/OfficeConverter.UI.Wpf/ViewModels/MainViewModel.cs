using System.Collections.ObjectModel;

namespace OfficeConverter.UI.Wpf.ViewModels;

public class MainViewModel
{
    public ObservableCollection<JobViewModel> Jobs { get; } = new();
}
