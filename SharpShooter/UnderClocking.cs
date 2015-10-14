using System;

namespace SharpShooter
{
    class UnderClocking
    {
        private static int lasttick;

        internal static bool NeedtoUnderClocking()
        {
            if (lasttick + 50 <= Environment.TickCount)
            {
                lasttick = Environment.TickCount;
                return false;
            }

            return true;
        }
    }
}
