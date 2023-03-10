using System;
using System.Reactive;
using System.Reactive.Linq;
using Photoshop.Domain.Scaling;
using Photoshop.View.Utils.Extensions;
using ReactiveUI;

namespace Photoshop.View.ViewModels;

public class ScalingContext : ReactiveObject
{
    private ScalingType _scalingType;

    public ScalingContext()
    {
        IsBcSplines = this.ObservableForPropertyValue(x => x.ScalingType)
            .Select(x => x is ScalingType.BcSplines);
        
        OnOkButtonClick = ReactiveCommand.Create<Unit, ScalingData>(
            _ => new ScalingData(B, C, ScalingType, Width, Height),
            canExecute: Observable.Return(true));

        OnCancelButtonClick = ReactiveCommand.Create<Unit, ScalingData?>(
            _ => null,
            canExecute: Observable.Return(true));
    }
    
    private ScalingType[] ScalingTypes { get; } = Enum.GetValues<ScalingType>();

    private IObservable<bool> IsBcSplines { get; }

    public ScalingType ScalingType
    {
        get => _scalingType;
        set => this.RaiseAndSetIfChanged(ref _scalingType, value);
    }

    public float B { get; set; }

    public float C { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }

    public ReactiveCommand<Unit, ScalingData> OnOkButtonClick { get; }
    
    public ReactiveCommand<Unit, ScalingData?> OnCancelButtonClick { get; }
}