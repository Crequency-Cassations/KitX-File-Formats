namespace KitX.Formats.KXP;

internal static class Header
{
    public static byte[] header = new byte[16]
    {
            073, // I
            116, // t
            032, //
            105, // i
            115, // s
            032, //
            097, // a
            032, //
            075, // K
            088, // X
            080, // P
            032, //
            102, // f
            105, // i
            108, // l
            101, // e

        // It is a KXP file
    };

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
