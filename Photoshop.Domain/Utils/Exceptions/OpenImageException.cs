﻿namespace Photoshop.Domain.Utils.Exceptions;

public class OpenImageException : Exception
{
    public OpenImageException(string message) : base(message)
    {
    }
}