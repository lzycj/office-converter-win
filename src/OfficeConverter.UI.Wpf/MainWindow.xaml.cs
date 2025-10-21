using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using OfficeConverter.Core.Models;
using OfficeConverter.Core.Runtime;
using OfficeConverter.UI.Wpf.ViewModels;

namespace OfficeConverter.UI.Wpf;

public partial class MainWindow : Window
{
    public MainViewModel VM { get; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = VM = new MainViewModel();
    }

    private async Task EnqueueFilesAsync(IEnumerable<string> paths)
    {
        foreach (var p in paths)
        {
            if (File.Exists(p))
            {
                var jobVm = new JobViewModel { FileName = p, Status = JobStatus.Queued };
                VM.Jobs.Add(jobVm);

                var job = new ConversionJob
                {
                    InputPath = p,
                    TargetFormat = "pdf",
                    Timeout = TimeSpan.FromSeconds(30),
                    Priority = 0
                };

                Services.Dispatcher.JobStatusChanged += (j, status) =>
                {
                    if (j.Id == job.Id)
                    {
                        Dispatcher.Invoke(() => jobVm.Status = status);
                    }
                };
                Services.Dispatcher.JobAttempt += (j, attempt) =>
                {
                    if (j.Id == job.Id)
                    {
                        Dispatcher.Invoke(() => jobVm.Progress = $"Attempt {attempt}");
                    }
                };

                _ = Task.Run(async () =>
                {
                    var result = await Services.Dispatcher.EnqueueAsync(job, CancellationToken.None);
                    Dispatcher.Invoke(() =>
                    {
                        if (result.Success)
                        {
                            jobVm.OutputPath = result.OutputPaths?.FirstOrDefault();
                            jobVm.LogPath = result.LogPath;
                        }
                        else
                        {
                            jobVm.Progress = result.ErrorCode;
                            jobVm.LogPath = result.LogPath;
                        }
                    });
                });
            }
        }
    }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Select files"
        };
        if (dlg.ShowDialog() == true)
        {
            await EnqueueFilesAsync(dlg.FileNames);
        }
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder"
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var files = Directory.EnumerateFiles(dlg.SelectedPath, "*.*", SearchOption.AllDirectories);
            await EnqueueFilesAsync(files);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings placeholder", "Settings");
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            await EnqueueFilesAsync(files);
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Output_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBlock tb && File.Exists(tb.Text))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer",
                Arguments = $"/select,\"{tb.Text}\"",
                UseShellExecute = true
            });
        }
    }

    private void Log_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBlock tb && File.Exists(tb.Text))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = tb.Text,
                UseShellExecute = true
            });
        }
    }
}
