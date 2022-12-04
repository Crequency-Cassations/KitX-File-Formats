namespace KitX.KXP.Helper
{
    internal static class Header
    {
        public static byte[] header = new byte[16]
        {
            110,    //  n
            105,    //  i
            100,    //  d
            117,    //  u
            111,    //  o
            106,    //  j
            105,    //  i
            117,    //  u
            104,    //  h
            117,    //  u
            105,    //  i
            108,    //  l
            097,    //  a
            105,    //  i
            097,    //  a
            063,    //  ?
        };

        /// <summary>
        /// 验证文件是否为 KXP 文件
        /// </summary>
        /// <param name="src">读取到的文件标头</param>
        /// <returns>是否是 KXP 文件</returns>
        public static bool IsKXP(ref byte[] src)
        {
            for (int i = 0; i < 16; ++i) if (src[i] != header[i]) return false;
            return true;
        }
    }
}
