using LeagueSharp;
using LeagueSharp.Common;

namespace SharpShooter
{
    class AutoQuit
    {
        internal static void Load()
        {
            MenuProvider.MenuInstance.AddItem(new MenuItem("QuitTheGameAfterGameOver", "Auto Quit the game after game over")).SetValue(true);
            Game.OnEnd += Game_OnEnd;
        }

        private static void Game_OnEnd(GameEndEventArgs args)
        {
            if (MenuProvider.MenuInstance.Item("QuitTheGameAfterGameOver").GetValue<bool>())
            {
                System.Threading.Tasks.Task.Run(
                async () =>
                {
                    await System.Threading.Tasks.Task.Delay(3000);
                    Game.Quit();
                });
            }
        }
    }
}
