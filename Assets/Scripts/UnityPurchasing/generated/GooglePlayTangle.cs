// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("XohLCxat4mmQbptqPNPmcENUft3vbGJtXe9sZ2/vbGxt3SGWQdsd2JpieItJvpju2zAO8aYFE7oTzZR6L1EF8vZNjFfPiuIHSWUljMcxX+/NKZhHTlXS6Upk26S7ar27e389ZGVkfKLlLMSgYR84yLX1vnr74r4oNocLg95oyFWDLEW4VVDklNnAphGjfgwFYc9Lx2qLP7lYUiCGRsWTi87OzLbUvvJEd/9K0FwsAMiXJGcxXe9sT11ga2RH6yXrmmBsbGxobW6U0nDCaI+sszgcVPUHh0hwzJHUIDbdcKKm6ng87uFRT5rPszyCbwp6nC0htnVOuunJurqMOn74/kSnYZJnVvJnV7Yh0NXxyFWEozE7VB1SE/6vikpF4hraCm9ubG1s");
        private static int[] order = new int[] { 13,1,2,8,11,12,12,12,8,13,13,13,12,13,14 };
        private static int key = 109;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
