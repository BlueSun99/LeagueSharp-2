﻿using System;

using LeagueSharp;

namespace SharpShooter
{
    class Initializer
    {
        internal static void Initialize()
        {
            Console.WriteLine("SharpShooter: HelloWorld!");

            MenuProvider.initialize();

            if(PluginLoader.LoadPlugin(ObjectManager.Player.ChampionName))
            {
                //nothing yet
            }
        }
    }
}
