using Avalonia.Controls;

namespace Photoshop.View.Utils;

public interface IMainWindowProvider
{
    Window Get();
}

public class MainWindowProvider : IMainWindowProvider
{
    private readonly Window _mainWindow;

    public MainWindowProvider(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public Window Get() => _mainWindow;
}