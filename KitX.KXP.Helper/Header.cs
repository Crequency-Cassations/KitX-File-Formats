namespace KitX.KXP.Helper;

internal static class Header
{
    public static byte[] header = "It is a KXP file"u8.ToArray();

    /// <summary>
    /// 验证文件是否为 KXP 文件
    /// </summary>
    /// <param name="src">读取到的文件标头</param>
    /// <returns>是否是 KXP 文件</returns>
    public static bool IsKXP(ref byte[] src, int index = 0, int count = 16)
    {
        for (int i = index; i < index + count; ++i)
            if (src[i] != header[i])
                return false;
        return true;
    }
}
