namespace KitX.KXP.Helper
{
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
            102, // F
            105, // i
            108, // l
            101, // e

            // It is a KXP File
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
