using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter
{
    class AutoQuit
    {
        private static int ENDTIME = 0;
        private static bool DONE = false;

        internal static void Load()
        {
            MenuProvider.MenuInstance.AddItem(new MenuItem("QuitTheGameAfterGameOver", "Auto Quit the game after game over")).SetValue(true);

            Game.OnUpdate += Game_OnUpdate;
            Game.OnEnd += Game_OnEnd;
        }

        private static void Game_OnUpdate(System.EventArgs args)
        {
            if (ENDTIME == 0 || DONE == true)
                return;

            if (ENDTIME + 3000 <= Environment.TickCount)
            {
                DONE = true;
                Game.Quit();
            }
        }

        private static void Game_OnEnd(GameEndEventArgs args)
        {
            ENDTIME = Environment.TickCount;
        }
    }
}
