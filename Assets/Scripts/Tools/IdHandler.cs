using System;

namespace Tools
{
    public static class IdHandler
    {

        #region Generation

        public static string GenerateRandomId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static int GetCurrentTimestamp()
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            return (int)((DateTimeOffset)now).ToUnixTimeSeconds();
        }

        #endregion

    }
}