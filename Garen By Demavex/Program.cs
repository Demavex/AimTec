﻿namespace Garen_By_Demavex
{
    using Aimtec;
    using Aimtec.SDK.Events;


    class Program
    {
        static void Main(string[] args)
        {
            GameEvents.GameStart += GameEvents_GameStart;
        }

        private static void GameEvents_GameStart()
        {
            if (ObjectManager.GetLocalPlayer().ChampionName != "Garen")
                return;

            var Garen = new Garen();
        }
    }
}