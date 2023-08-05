using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace KitX.Formats.KXP;

using File = System.IO.File;

public class Encoder
{
    public Encoder(
        List<string> filesToInclude,
        string loaderStruct,
        string pluginStruct,
        Options? options = null
    )
    {
        FilesToInclude = filesToInclude;
        LoaderStruct = loaderStruct;
        PluginStruct = pluginStruct;
        Options = options ?? new();
    }

    public List<string> FilesToInclude { get; set; }

    public string LoaderStruct { get; set; }

    public string PluginStruct { get; set; }

    public Options Options { get; set; }

    /// <summary>
    /// 编码包体
    /// </summary>
    /// <param name="rootDir">包体源文件根路径</param>
    /// <param name="saveFolder">包体输出路径</param>
    /// <param name="filename">包体名称, 自带 .kxp 后缀</param>
    /// <exception cref="Exception">空错误</exception>
    public void Encode(string rootDir, string saveFolder, string filename)
    {
        #region 读取每个文件的 byte[]

        var files = new Dictionary<string, byte[]>();

        foreach (var item in FilesToInclude)
            if (File.Exists(item))
                files.Add(
                    GetRelativePath(rootDir, item),
                    File.ReadAllBytes(item)
                );
            else Console.WriteLine($"File {item} not exists.");

        #endregion

        var loaderStruct = Encoding.UTF8.GetBytes(LoaderStruct);
        var pluginStruct = Encoding.UTF8.GetBytes(PluginStruct);

        var fileCount = files.Keys.Count;   //  待封装的文件数量

        if (Options.Verbose)
            Console.WriteLine($"Count of files to include: {fileCount}");

        var target = $"{saveFolder}\\{filename}.kxp";    //  目标文件路径

        if (File.Exists(target)) File.Delete(target);   //  如果目标文件存在就覆盖掉

        var fs = File.Create(target);    //  目标文件文件流

        var all_files = new Queue<byte>();  //  除开头哈希校验值的部分其它全部值

        #region 入队 LoaderStruct 部分

        EnqueueSingleLong(ref all_files, loaderStruct.LongLength);      //  入队加载器结构的长度

        if (Options.Verbose)
            Console.WriteLine($"Loader Struct Length: {loaderStruct.LongLength}");

        foreach (var item in loaderStruct)
            all_files.Enqueue(item);    //  入队加载器结构

        #endregion

        #region 入队 PluginStruct 部分

        EnqueueSingleLong(ref all_files, pluginStruct.LongLength);      //  入队插件结构的长度

        if (Options.Verbose)
            Console.WriteLine($"Plugin Struct Length: {pluginStruct.LongLength}");

        foreach (var item in pluginStruct)
            all_files.Enqueue(item);    //  入队插件结构

        #endregion

        #region 入队文件表部分

        EnqueueSingleLong(ref all_files, fileCount);    //  入队文件表长度

        var fileNames = new List<byte[]>();

        foreach (var item in files)
        {
            var fileNameByte = Encoding.UTF8.GetBytes(item.Key);    //  获取文件相对路径的二进制值
            fileNames.Add(fileNameByte);    //  添加文件相对路径的二进制值

            EnqueueSingleLong(ref all_files, fileNameByte.LongLength);  //  入队每个相对路径的长度
            EnqueueSingleLong(ref all_files, item.Value.LongLength);    //  入队每个文件的长度

            if (Options.Verbose)
            {
                Console.WriteLine($"Relative Path: {item.Key}");
                Console.WriteLine($"Path Length:   {fileNameByte.LongLength}");
                Console.WriteLine($"File Length:   {item.Value.LongLength}");
            }
        }

        #endregion

        #region 入队文件部分

        var index = 0;

        foreach (var item in files)
        {
            foreach (var fn in fileNames[index])
                all_files.Enqueue(fn);

            foreach (var one in item.Value)
                all_files.Enqueue(one);

            ++index;
        }

        #endregion

        #region 计算哈希校验

        var filesBytes = all_files.ToArray();

        using var md5 = MD5.Create();

        var hash = md5.ComputeHash(filesBytes);      //  计算文件体哈希

        if (Options.Verbose)
            Console.WriteLine($"Hash Code: {Encoding.UTF8.GetString(hash)}");

        #endregion

        #region 写入文件

        foreach (var item in Header.header)
            fs.WriteByte(item);     //  写入文件标头

        foreach (var item in hash)
            fs.WriteByte(item);     //  写入哈希值

        foreach (var item in filesBytes)
        {
            fs.WriteByte(item);     //  写入文件体
            fs.Flush(); //  每写入一个文件就清空缓冲区并确保写入文件
        }

        #endregion

        fs.Flush();
        fs.Close();
        fs.Dispose();
    }

    /// <summary>
    /// 入队单个 long 值作为 8 个 byte
    /// </summary>
    /// <param name="queue">队列</param>
    /// <param name="value">long 值</param>
    private static void EnqueueSingleLong(ref Queue<byte> queue, long value)
    {
        var bytes = BitConverter.GetBytes(value);

        foreach (var item in bytes)
            queue.Enqueue(item);
    }

    /// <summary>
    /// 获取相对路径
    /// </summary>
    /// <param name="rootDir">根目录</param>
    /// <param name="filePath">文件目录</param>
    /// <returns>相对于根目录的文件目录</returns>
    /// <exception cref="ArgumentException">参数异常</exception>
    private static string GetRelativePath(string rootDir, string filePath)
    {
        if (filePath.StartsWith(rootDir))
            return filePath.Replace(rootDir, "");
        else throw new ArgumentException("文件路径不属于所给的根目录");
    }
}
