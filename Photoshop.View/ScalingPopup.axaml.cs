using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Photoshop.View.ViewModels;

namespace Photoshop.View;

public partial class ScalingPopup : Window
{
    public ScalingPopup()
    {
        InitializeComponent();

        var context = new ScalingContext();

        DataContext = context;

        Observable.Merge(
                context.OnCancelButtonClick,
                context.OnOkButtonClick)
            .Subscribe(Close);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}