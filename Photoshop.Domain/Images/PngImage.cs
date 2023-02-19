﻿using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Photoshop.Domain.Utils;

namespace Photoshop.Domain.Images;

public class PngImage : IImage
{
    private readonly ImageData _data;
    private float _gamma;

    public PngImage(ImageData data, float gamma)
    {
        this._data = data;
        this._gamma = gamma;
    }
    public static bool CheckFileHeader(byte[] image)
    {
        return image[0] == 137 && image[1] == 80 && image[2] == 78 && image[3] == 71 && image[4] == 13 && image[5] == 10 &&
               image[6] == 26 && image[7] == 10;
    }
    
    private enum ChunkType
    {
        IHDR,
        PLTE,
        IDAT,
        IEND,
        gAMA,
        NOT_SUPPORTED
    }
    private record ChunkInfo (int DataSize, int DataStart, ChunkType Type)
    {
        public int TotalSize()
        {
            return DataSize + 12;
        }
    }

    public ImageData GetData()
    {
        return  _data;
    }
    
    private int ReadInt(byte[] image, int ind)
    {
        return (1 << 24) * image[ind] + (1 << 16) * image[ind + 1] + (1 << 8) * image[ind + 2] + image[ind + 3];
    }

    private ChunkInfo ReadChunk(byte[] image, int chunkStart)
    {
        int size = ReadInt(image, chunkStart);
        var chunkTypeStr = Encoding.ASCII.GetString(image, chunkStart + 4, 4);

        bool result = Enum.TryParse(typeof(ChunkType), chunkTypeStr, out var chunkType);
        if (result)
            return new ChunkInfo(size, chunkStart + 8, (ChunkType)chunkType!);
        else
            return new ChunkInfo(size, chunkStart + 8, ChunkType.NOT_SUPPORTED);
    }

    public PngImage(byte[] image)
    {
        var gammaConverter = new GammaConverter();

        if (!CheckFileHeader(image))
        {
            throw new ArgumentException("Некорректный PNG файл");
        }

        int ind = 8;
        int height = 0, width = 0, colorType = 0;
        PixelFormat pixelFormat = PixelFormat.Gray;
        byte[]? imageBytes = default;
        byte[]? palette = default;
        float gamma = 1;
        Dictionary<string, int> chunkTypeCnt = new Dictionary<string, int>();
        var types = Enum.GetValues<ChunkType>();
        int bytesRead = 0;
        int coef = 1;
            
        foreach (var type in types)
        {
            chunkTypeCnt[type.ToString()] = 0;
        }
            
        ChunkInfo chunk = ReadChunk(image, ind);
        if (chunk.Type != ChunkType.IHDR)
        {
            throw new ArgumentException("Некорректный PNG файл");
        }

        while (chunk.Type != ChunkType.IEND)
        {
            chunkTypeCnt[chunk.Type.ToString()]++; // ChunkType.IEND не прибавит единицы, так как цикл while на нем завершится
            switch (chunk.Type)
            {
                case ChunkType.IHDR:
                    width = ReadInt(image, chunk.DataStart);
                    height = ReadInt(image, chunk.DataStart + 4);
                    int bitDepth = image[chunk.DataStart + 8];
                    colorType = image[chunk.DataStart + 9];
                        
                    if (bitDepth != 8)
                    {
                        throw new ArgumentException("Некорректный PNG файл");
                    }

                    if (colorType != 0 && colorType != 2 && colorType != 3)
                    {
                        throw new ArgumentException("Некорректный PNG файл");
                    }

                    pixelFormat = colorType == 0 ? PixelFormat.Gray : PixelFormat.Rgb;
                    coef = (pixelFormat == PixelFormat.Gray ? 1 : 3);
                        
                    imageBytes = new byte[(width * coef + 1) * height + 100];
                    break;

                case ChunkType.PLTE:
                    if (chunk.DataSize % 3 != 0 || chunk.DataSize > 256 * 3)
                    {
                        throw new ArgumentException("Некорректный PNG файл");
                    }

                    palette = new byte[chunk.DataSize];
                    Array.Copy(image, chunk.DataStart, palette, 0, chunk.DataSize);
                    break;

                case ChunkType.IDAT:
                        
                    if (imageBytes is null)
                        throw new ArgumentException("Некорректный PNG файл");

                    var memoryStream = new MemoryStream(image, chunk.DataStart, chunk.DataSize);
                    var zlibStream = new ZLibStream(memoryStream, CompressionMode.Decompress);
                    int bytesWritten;
                    while ((bytesWritten = zlibStream.Read(imageBytes, bytesRead, imageBytes.Length - bytesRead)) > 0)
                    {
                        bytesRead += bytesWritten;    
                    }
                    if (bytesRead > (width * coef + 1) * height)
                    {
                        throw new ArgumentException("Некорректный PNG файл");
                    }
                    zlibStream.Close();
                    break;
                    
                case ChunkType.gAMA:
                    gamma = ReadInt(image, chunk.DataStart) / 100000f;
                    break;
                    
                case ChunkType.IEND:
                    break;
            }
            ind += chunk.TotalSize();
                
            chunk = ReadChunk(image, ind);
        }

        if (chunkTypeCnt["IHDR"] != 1 || chunkTypeCnt["gAMA"] > 1 || chunkTypeCnt["IDAT"] == 0 ||
            chunkTypeCnt["PLTE"] > (colorType == 0 ? 0 : 1))
        {
            throw new ArgumentException("Некорректный PNG файл");
        }
            
        coef = (colorType == 2 ? 3 : 1);
            
        if (bytesRead != (height * (width * coef + 1)))
        {
            throw new ArgumentException("Некорректный PNG файл" + bytesRead + " " + height + " " + width + " " + coef + " " + colorType);
        }
            
        float[] pixels = new float[height * width * coef]; 
            
        if (imageBytes is null)
            throw new ArgumentException("Некорректный PNG файл");
            
        if (colorType == 3)
        {
            if (palette is null)
                throw new ArgumentException("Некорректный PNG файл");
                
            pixels = new float[height * width * 3]; 
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    pixels[(i * width + j) * 3] = palette[imageBytes[(i * (width + 1) + j + 1)] * 3]; //(width + 1) и +1 возникают из-за байта фильтрации в начале каждой строки 
                    pixels[(i * width + j) * 3 + 1] = palette[imageBytes[(i * (width + 1) + j + 1)] * 3 + 1];
                    pixels[(i * width + j) * 3 + 2] = palette[imageBytes[(i * (width + 1) + j + 1)] * 3 + 2];
                }
            }
        }
        else
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width * coef; j++)
                {
                    pixels[i * width * coef + j] = imageBytes[i * (width * coef + 1) + j + 1];
                }
            }
        }
            
        var noGammaImageData = new ImageData(pixels, pixelFormat, height, width);
        _data = gammaConverter.ConvertGamma(noGammaImageData, gamma, 1);
    }
    
    
    
    private int GetCRC(ReadOnlySpan<byte> data)
    {
        CrcCalculator calculator = new CrcCalculator();
        return calculator.CalculateCrc(data);
    }
    
    private void WriteIntToStream(Stream stream, int value)
    {
        stream.WriteByte((byte) (value >> 24));
        stream.WriteByte((byte) (value >> 16 & 255));
        stream.WriteByte((byte) (value >> 8 & 255));
        stream.WriteByte((byte) (value & 255));
    }
    
    private void AddChunk(Stream outputStream, ChunkType chunkType, ReadOnlySpan<byte> data)
    {
        if (chunkType == ChunkType.NOT_SUPPORTED)
        {
            return;
        }

        WriteIntToStream(outputStream, data.Length);
        outputStream.Write(Encoding.ASCII.GetBytes(chunkType.ToString()));
        outputStream.Write(data);
        WriteIntToStream(outputStream, GetCRC(data));
    }

    private void AddChunk(Stream outputStream, ChunkType chunkType, List<byte> buffer)
    {
        ReadOnlySpan<byte> span = CollectionsMarshal.AsSpan(buffer);
        AddChunk(outputStream, chunkType, span);
        buffer.Clear();
    }
    
    static void CopyStream(System.IO.Stream src, System.IO.Stream dest)
    {
        byte[] buffer = new byte[1024];
        int len;
        while ((len = src.Read(buffer, 0, buffer.Length)) > 0) {
            Console.WriteLine(len);
            dest.Write(buffer, 0, len);
        }
        dest.Flush();
    }

    public byte[] GetFile()
    {
        Stream outputStream = new MemoryStream();
        List<byte> buffer = new List<byte>();
        
        outputStream.WriteByte(137);
        outputStream.WriteByte(80);
        outputStream.WriteByte(78);
        outputStream.WriteByte(71);
        outputStream.WriteByte(13);
        outputStream.WriteByte(10);
        outputStream.WriteByte(26);
        outputStream.WriteByte(10);
        
        buffer.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(_data.Width)));
        buffer.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(_data.Height)));
        buffer.Add(8);
        buffer.Add(_data.PixelFormat == PixelFormat.Gray ? (byte) 0 : (byte) 2);
        buffer.Add(0);
        buffer.Add(0);
        buffer.Add(0);
        AddChunk(outputStream, ChunkType.IHDR, buffer);

        var bufferArray = new byte[_data.Pixels.Length + _data.Height]; //Array.ConvertAll(_data.Pixels, x => (byte) (x *  255));
        int coef = (_data.PixelFormat == PixelFormat.Gray ? 1 : 3);
        for (int i = 0; i < _data.Height; i++)
        {
            for (int j = 0; j < _data.Width * coef; j++)
            {
                bufferArray[i * (_data.Width * coef + 1) + j + 1] = (byte) (_data.Pixels[_data.Width * coef * i + j]);
            }
        }

        var memoryStream = new MemoryStream();
        var memoryStream2 = new MemoryStream(bufferArray);
        var zlibStream = new ZLibStream(memoryStream, CompressionMode.Compress);
        CopyStream(memoryStream2, zlibStream);
        var span = new Span<byte>(bufferArray, 0, (int) memoryStream.Length);
        memoryStream.Position = 0;
        int bytesRead = memoryStream.Read(span);
        AddChunk(outputStream, ChunkType.IDAT, span);
        zlibStream.Close();
        memoryStream.Close();
        
        buffer.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((int) _gamma * 100000)));
        AddChunk(outputStream, ChunkType.gAMA, buffer);
        
        AddChunk(outputStream, ChunkType.IEND, ReadOnlySpan<byte>.Empty);
        
        byte[] file = new byte[outputStream.Length];
        outputStream.Position = 0;
        outputStream.Read(file);
        return file;
    }
}