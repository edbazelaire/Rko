// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("mZmb4YPppRMgqB2HC3tXn8BzMGbDhSeVP9j75G9LA6JQ0B8nm8aDd7g7NToKuDswOLg7OzqKdsEWjEqP9ClbUjaYHJA93GjuDwV30RGSxNyafs8QGQKFvh0zjPPsPersLChqM8t6duEiGe2+nu3t220pr6kT8DbFCd8cXEH6tT7HOcw9a4SxJxQDKYrNNS/cHunPuYxnWabxUkTtRJrDLTIzK/Wye5P3Nkhvn+Ki6S2stel/eAZSpaEa2wCY3bVQHjJy25BmCLhh0FzUiT+fAtR7Eu8CB7PDjpfxRmGKJ/XxvS9rubYGGM2Y5GvVOF0tMAGlMADhdoeCpp8C0/RmbANKBUQKuDsYCjc8MxC8crzNNzs7Oz86Oan43R0StU2NXTg5Ozo7");
        private static int[] order = new int[] { 3,9,9,6,11,7,13,9,12,12,12,12,12,13,14 };
        private static int key = 58;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
