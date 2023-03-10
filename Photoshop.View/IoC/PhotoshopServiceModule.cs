using Avalonia.Controls;
using Ninject.Modules;
using Photoshop.Domain;
using Photoshop.Domain.ImageEditors;
using Photoshop.Domain.ImageEditors.Factory;
using Photoshop.Domain.Images.Factory;
using Photoshop.Domain.Scaling;
using Photoshop.Domain.Utils;
using Photoshop.View.Services;
using Photoshop.View.Services.Interfaces;
using Photoshop.View.Utils;
using Photoshop.View.ViewModels;

namespace Photoshop.View.IoC;

public class PhotoshopServiceModule : NinjectModule
{
    private readonly Window _mainWindow;

    public PhotoshopServiceModule(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public override void Load()
    {
        Bind<IMainWindowProvider>().To<MainWindowProvider>()
            .InSingletonScope()
            .WithConstructorArgument("mainWindow", _mainWindow);
     
        Bind<IImageService>().To<ImageService>();
        Bind<IDialogService>().To<DialogService>();
        
        Bind<IImageEditorFactory>().To<ImageEditorFactory>();
        Bind<IImageFactory>().To<ImageFactory>();
        Bind<ICommandFactory>().To<CommandFactory>();
        
        Bind<IColorSpaceConverter>().To<ColorSpaceConverter>();
        Bind<IGammaConverter>().To<GammaConverter>();
        Bind<IDitheringConverter>().To<DitheringConverter>();
        Bind<IScalingConverter>().To<ScalingConverter>();

        Bind<PhotoEditionContext>().To<PhotoEditionContext>().InSingletonScope();
        Bind<ColorSpaceContext>().To<ColorSpaceContext>().InSingletonScope();
    }
}