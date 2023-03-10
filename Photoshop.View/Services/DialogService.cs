using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Photoshop.View.Services.Interfaces;
using Photoshop.View.Utils;
using Photoshop.View.Windows;

namespace Photoshop.View.Services;

public class DialogService : IDialogService
{
    private readonly Window _parentWindow;

    private readonly List<FileDialogFilter> _fileDialogFilters = new()
    {
        new FileDialogFilter
        {
            Name = "All files"
        },
        new FileDialogFilter
        {
            Name = "PNM (*.ppm, *.pgm, *.pnm)",
            Extensions = new List<string> { "ppm", "pgm", "pnm" }
        },
        new FileDialogFilter
        {
            Name = "PNG (*.png)",
            Extensions = new List<string> { "png" }
        }
    };

    public DialogService(IMainWindowProvider mainWindowProvider)
    {
        _parentWindow = mainWindowProvider.Get();
    }

    public async Task<string?> ShowOpenFileDialogAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filters = _fileDialogFilters,
            AllowMultiple = false,
        };

        var result = await dialog.ShowAsync(_parentWindow);
        return result?[0];
    }

    public async Task<string?> ShowSaveFileDialogAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filters = _fileDialogFilters
        };

        return await dialog.ShowAsync(_parentWindow);
    }

    public async Task ShowErrorAsync(string message)
    {
        var dialog = new ErrorDialog
        {
            DataContext = new ErrorContext(message)
        };

        await dialog.ShowDialog(_parentWindow);
    }
}