﻿using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace StarlightRiver.Core.Loaders
{
    class UILoader : ILoadable
    {
        public static List<UserInterface> UserInterfaces = new List<UserInterface>();
        public static List<SmartUIState> UIStates = new List<SmartUIState>();

        public void Load()
        {
            Mod mod = StarlightRiver.Instance;

            foreach(Type t in mod.Code.GetTypes())
            {
                if (t.IsSubclassOf(typeof(SmartUIState)))
                {
                    var state = (SmartUIState)Activator.CreateInstance(t, null);
                    var userInterface = new UserInterface();
                    userInterface.SetState(state);

                    UIStates.Add(state);
                    UserInterfaces.Add(userInterface);
                }
            }
        }

        public void Unload()
        {
            UserInterfaces = null;
            UIStates = null;
        }

        public static void AddLayer(List<GameInterfaceLayer> layers, UserInterface userInterface, UIState state, int index, bool visible)
        {
            string name = state == null ? "Unknown" : state.ToString();
            layers.Insert(index, new LegacyGameInterfaceLayer("StarlightRiver: " + name,
                delegate
                {
                    if (visible)
                    {
                        userInterface.Update(Main._drawInterfaceGameTime);
                        state.Draw(Main.spriteBatch);
                    }
                    return true;
                }, InterfaceScaleType.UI));
        }

        public static T GetUIState<T>() where T : SmartUIState => UIStates.FirstOrDefault(n => n is T) as T;
    }
}
