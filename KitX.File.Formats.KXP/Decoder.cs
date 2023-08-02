using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KitX.File.Formats.KXP;

public class Decoder
{
    public Decoder(string packagePath, Options? options = null)
    {
        PackagePath = packagePath;

        Options = options ?? new();
    }

    public string PackagePath { get; set; }

    public Options Options { get; set; }

    /// <summary>
    /// 文件地图项
    /// </summary>
    internal struct FileMapItem
    {
        /// <summary>
        /// 文件名长度
        /// </summary>
        internal long FileNamePathLength;

        /// <summary>
        /// 文件体长度
        /// </summary>
        internal long FileBodyLength;
    }

    /// <summary>
    /// 获取加载器结构和插件结构的字符串
    /// </summary>
    /// <returns>加载器结构和插件结构的字符串</returns>
    public Tuple<string, string> GetLoaderAndPluginStruct()
    {
        var fs = new FileStream(PackagePath, FileMode.Open, FileAccess.Read);
        var reader = new BinaryReader(fs);

        var header = reader.ReadBytes(16);

        if (!Header.IsKXP(ref header))
            throw new InvalidDataException("It's not a KXP Package.");

        _ = reader.ReadBytes(16);

        long loaderStructLength, pluginStructLength;

        loaderStructLength = BitConverter.ToInt64(reader.ReadBytes(8), 0);

        var loaderStructString = Encoding.UTF8.GetString(reader.ReadBytes((int)loaderStructLength));

        pluginStructLength = BitConverter.ToInt64(reader.ReadBytes(8), 0);

        var pluginStructString = Encoding.UTF8.GetString(reader.ReadBytes((int)pluginStructLength));

        var result = new Tuple<string, string>(loaderStructString, pluginStructString);

        reader.Close();
        fs.Close();

        return result;
    }

    /// <summary>
    /// 解码包体
    /// </summary>
    /// <returns>返回 LoaderStruct 的 json 字符串 和 PluginStruct 的 json 字符串</returns>
    /// <param name="releaseFolder">释放文件的路径</param>
    /// <exception cref="Exception">哈希校验错误</exception>
    public Tuple<string, string> Decode(string releaseFolder)
    {
        if (!Directory.Exists(releaseFolder))
            _ = Directory.CreateDirectory(releaseFolder);   //  如果释放文件夹不存在就创建

        var src = File.ReadAllBytes(PackagePath);    //  读取包的全部字节

        #region 获取文件表头, 验证文件是否为 KXP 文件 (0 - 15)

        var header = new byte[16];   //  文件标头

        for (int i = 0; i < 16; ++i) header[i] = src[i];

        if (!Header.IsKXP(ref header))
            throw new Exception("It's not a KXP Package.");

        #endregion

        #region 获取哈希校验值 (16 - 31)

        var hash = new byte[16];     //  哈希部分

        var cursor = 16;     //  文件流指针

        for (; cursor < 32; ++cursor)       //  取出哈希部分的字节
            hash[cursor - 16] = src[cursor];

        if (Options.Verbose)
            Console.WriteLine($"Hash Code: {Encoding.UTF8.GetString(hash)}");

        #endregion

        #region 获取 LoaderStruct 部分

        var loaderStructLengthByte = new byte[8];

        for (int i = 0; i < 8; ++i, ++cursor) loaderStructLengthByte[i] = src[cursor];

        var loaderStructLength = BitConverter.ToInt64(loaderStructLengthByte, 0);

        if (Options.Verbose)
            Console.WriteLine($"Loader Struct Length: {loaderStructLength}");

        var loaderStructByte = new byte[loaderStructLength];     //  Loader Struct 的 Byte 数组

        for (int i = 0; i < loaderStructLength; ++i, ++cursor) loaderStructByte[i] = src[cursor];

        #endregion

        #region 获取 PluginStruct 部分

        var pluginStructLengthByte = new byte[8];

        for (int i = 0; i < 8; ++i, ++cursor) pluginStructLengthByte[i] = src[cursor];

        var pluginStructLength = BitConverter.ToInt64(pluginStructLengthByte, 0);

        if (Options.Verbose)
            Console.WriteLine($"Plugin Struct Length: {pluginStructLength}");

        var pluginStructByte = new byte[pluginStructLength];     //  Plugin Struct 的 Byte 数组

        for (int i = 0; i < pluginStructLength; ++i, ++cursor) pluginStructByte[i] = src[cursor];

        #endregion

        #region 获取 LoaderStruct 和 PluginStruct 字符串

        var loaderStruct = Encoding.UTF8.GetString(loaderStructByte);
        var pluginStruct = Encoding.UTF8.GetString(pluginStructByte);

        if (Options.Verbose)
        {
            Console.WriteLine($"Loader Struct: {loaderStruct}");
            Console.WriteLine($"Plugin Struct: {pluginStruct}");
        }

        #endregion

        #region 获取文件地图

        var fileMapLengthByte = new byte[8];     //  文件地图长度

        for (int i = 0; i < 8; ++i, ++cursor) fileMapLengthByte[i] = src[cursor];

        var fileMapLength = BitConverter.ToInt64(fileMapLengthByte, 0);

        if (Options.Verbose)
            Console.WriteLine($"File Map Length: {fileMapLength}");

        var FileMap = new List<FileMapItem>();        //  文件地图

        for (long i = 0; i < fileMapLength; ++i)
        {
            var fileNameLength = new byte[8];    //  文件名长度
            var fileBodyLength = new byte[8];    //  文件体长度
            for (int j = 0; j < 8; ++j, ++cursor) fileNameLength[j] = src[cursor];
            for (int j = 0; j < 8; ++j, ++cursor) fileBodyLength[j] = src[cursor];

            FileMap.Add(new FileMapItem()
            {
                FileNamePathLength = BitConverter.ToInt64(fileNameLength, 0),
                FileBodyLength = BitConverter.ToInt64(fileBodyLength, 0)
            });

            if (Options.Verbose)
            {
                Console.WriteLine($"File Name Length: {BitConverter.ToInt64(fileNameLength, 0)}");
                Console.WriteLine($"File Body Length: {BitConverter.ToInt64(fileBodyLength, 0)}");
            }
        }

        #endregion

        #region 获取源文件文件名与文件体并立即写回释放文件夹

        foreach (var item in FileMap)
        {
            var fn = new byte[item.FileNamePathLength];
            var fb = new byte[item.FileBodyLength];

            for (int i = 0; i < item.FileNamePathLength; ++i, ++cursor)
                fn[i] = src[cursor];

            for (int i = 0; i < item.FileBodyLength; ++i, ++cursor)
                fb[i] = src[cursor];

            var fileName = Encoding.UTF8.GetString(fn);
            var dirPath = Path.GetDirectoryName(
                Path.GetFullPath($"{releaseFolder}/{fileName}")
            );

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            File.WriteAllBytes($"{releaseFolder}/{fileName}", fb);
        }

        #endregion

        #region 计算哈希校验值

        using var md5 = MD5.Create();

        var localHash = md5.ComputeHash(src, 32, src.Length - 32);

        if (Options.Verbose)
            Console.WriteLine($"Local Hash Code: {Encoding.UTF8.GetString(localHash)}");

        #endregion

        #region 哈希校验不一样时抛出异常

        for (var i = 0; i < 16; ++i)
            if (!hash[i].Equals(localHash[i]))
                throw new Exception("MD5 Hash Error.");

        #endregion

        return Tuple.Create(loaderStruct, pluginStruct);
    }
}
