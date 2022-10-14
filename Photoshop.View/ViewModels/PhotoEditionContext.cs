﻿using System;
using System.IO;
using Avalonia.Skia.Helpers;
using Photoshop.Domain;
using Photoshop.Domain.ImageEditors;
using Photoshop.Domain.ImageEditors.Factory;
using Photoshop.Domain.Images;
using Photoshop.Domain.Images.Factory;
using Photoshop.View.Commands;
using Photoshop.View.Converters;
using Photoshop.View.Extensions;
using ReactiveUI;
using IImage = Avalonia.Media.IImage;

namespace Photoshop.View.ViewModels;
public class PhotoEditionContext : ReactiveObject
{
    private readonly IImageConverter _imageConverter;
    
    private IImageEditor? _imageEditor = null;

    public PhotoEditionContext(
        OpenImageCommand openImage, 
        SaveImageCommand saveImage, 
        IImageFactory imageFactory, 
        IImageEditorFactory imageEditorFactory, 
        IImageConverter imageConverter)
    {
        _imageConverter = imageConverter;

        SaveImage = saveImage;
        OpenImage = openImage;
        OpenImage.StreamCallback = async stream =>
        {
            int length = (int) stream.Length;
            var bytes = new byte[length];
            await stream.ReadAsync(bytes, 0, length);
            var image = imageFactory.GetImage(bytes);

            var imageData = image.GetData();
            ImageEditor = imageEditorFactory.GetImageEditor(imageData);
        };
        SaveImage.PathCallback = path =>
        {
            if (path.Length < 4)
                return; //Стоит ввести фидбек для пользователя
            if (ImageEditor == null)
            {
                return;
            }
            string extension = path.Substring(path.Length - 4, 4).ToLower();
            Photoshop.Domain.Images.IImage image;
            switch (extension)
            {
                case ".pgm":
                    image = new PnmImage(ImageEditor.GetData(), PixelFormat.Gray);
                    break;
                case ".ppm":
                    image = new PnmImage(ImageEditor.GetData(), PixelFormat.Rgb);
                    break;
                default:
                    return;
            }
            var imageData = image.GetFile();
            using var fileStream = File.Open(path, FileMode.Create);
            fileStream.Write(imageData);
        };
    }

    public OpenImageCommand OpenImage { get; }
    public SaveImageCommand SaveImage { get; }
    
    public IImage? Image
    {
        get => ImageEditor == null ? null : _imageConverter.ConvertToBitmap(ImageEditor.GetData());
    }

    private IImageEditor? ImageEditor
    {
        get => _imageEditor;
        set
        {
            _imageEditor = this.RaiseAndSetIfChanged(ref _imageEditor, value);
            this.RaisePropertyChanged("Image");
            SaveImage.OnCanExecuteChanged();
        }
    }
}