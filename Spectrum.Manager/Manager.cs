﻿using System;
using System.Collections.Generic;
using System.IO;
using Spectrum.API;
using Spectrum.API.Configuration;
using Spectrum.API.Input;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using Spectrum.Manager.Lua;
using Spectrum.Manager.Managed;

namespace Spectrum.Manager
{
    public class Manager : IManager
    {
        public ILoader LuaLoader { get; set; }
        public IExecutor LuaExecutor { get; set; }

        private PluginContainer ManagedPluginContainer { get; set; }
        private PluginLoader ManagedPluginLoader { get; set; }

        private string ScriptDirectory { get; }
        private string OnDemandScriptDirectory { get; }
        private string PluginDirectory { get; }

        private Dictionary<Hotkey, string> ScriptHotkeys { get; set; }
        private Dictionary<Hotkey, Action> ActionHotkeys { get; set; }

        public bool CanLoadScripts => Directory.Exists(ScriptDirectory);
        public bool CanLoadPlugins => Directory.Exists(PluginDirectory);

        public Settings Settings { get; private set; }
        public Settings ScriptHotkeySettings { get; private set; }

        public Manager()
        {
            InitializeSettings();
            InitializeScriptHotkeys();
            ActionHotkeys = new Dictionary<Hotkey, Action>();

            ScriptDirectory = Defaults.ScriptDirectory;
            PluginDirectory = Defaults.PluginDirectory;
            OnDemandScriptDirectory = Defaults.OnDemandScriptDirectory;

            if (Settings.GetValue<bool>("LoadScripts"))
            {
                TryInitializeLua();
                StartLua();
            }

            if (Settings.GetValue<bool>("LoadPlugins"))
            {
                LoadExtensions();
                StartExtensions();
            }
        }

        public void UpdateExtensions()
        {
            if (ScriptHotkeys.Count > 0)
            {
                foreach (var hotkey in ScriptHotkeys)
                {
                    if (hotkey.Key.IsPressed)
                    {
                        LuaExecutor.Execute(hotkey.Value);
                    }
                }    
            }

            if (ActionHotkeys.Count > 0)
            {
                foreach (var hotkey in ActionHotkeys)
                {
                    if (hotkey.Key.IsPressed)
                    {
                        hotkey.Value.Invoke();
                    }
                }
            }

            if (ManagedPluginContainer != null)
            {
                foreach (var pluginInfo in ManagedPluginContainer)
                {
                    if (pluginInfo.Enabled && pluginInfo.IsUpdatable)
                    {
                        ((IUpdatable)pluginInfo.Plugin).Update();
                    }
                }
            }
        }

        public void AddHotkey(Hotkey hotkey, Action action)
        {
            if (ScriptHotkeys.ContainsKey(hotkey))
            {
                Console.WriteLine($"MANAGER: Warning. The hotkey '{hotkey}' you were trying to assign was already assigned to a script '{ScriptHotkeys[hotkey]}.");
                Console.WriteLine("         Spectrum will not re-assign this hotkey.");
                return;
            }

            if (ActionHotkeys.ContainsKey(hotkey))
            {
                Console.WriteLine($"MANAGER: Warning. The hotkey '{hotkey}' you were trying to assign was already assigned to an existing action.");
                Console.WriteLine("         Spectrum will not re-assign this hotkey.");
                return;
            }
            ActionHotkeys.Add(hotkey, action);
        }

        private void InitializeSettings()
        {
            try
            {
                Settings = new Settings(typeof(Manager));
                if (Settings["FirstRun"] == string.Empty || Settings.GetValue<bool>("FirstRun"))
                {
                    RecreateSettings();
                }
            }
            catch
            {
                Console.WriteLine("MANAGER: Couldn't load settings. Defaults loaded.");
            }
        }

        private void RecreateSettings()
        {
            Settings["FirstRun"] = "false";
            Settings["LoadPlugins"] = "true";
            Settings["LoadScripts"] = "true";

            Settings.Save();
        }

        private void InitializeScriptHotkeys()
        {
            try
            {
                ScriptHotkeySettings = new Settings(typeof(Manager), "Hotkeys");
                ScriptHotkeys = new Dictionary<Hotkey, string>();

                foreach (var s in ScriptHotkeySettings)
                {
                    var hotkey = new Hotkey(s.Key);

                    if (ScriptHotkeys.ContainsKey(hotkey))
                    {
                        Console.WriteLine($"MANAGER: Warning. The hotkey '{hotkey}' has already been assigned to '{ScriptHotkeys[hotkey]}'.");
                        Console.WriteLine("         Spectrum will not re-assign this hotkey.");
                        continue;
                    }
                    ScriptHotkeys.Add(hotkey, s.Value);
                }
            }
            catch
            {
                Console.WriteLine("MANAGER: Couldn't load script hotkeys.");
            }
        }

        private void TryInitializeLua()
        {
            if (CanLoadScripts)
            {
                LuaLoader = new Loader(ScriptDirectory, OnDemandScriptDirectory);
                LuaLoader.LoadAll();
            }
            else
            {
                Console.WriteLine($"Can't load or execute scripts. Directory '{ScriptDirectory}' does not exist.");
            }
        }

        private void StartLua()
        {
            if (CanLoadScripts)
            {
                LuaExecutor = new Executor(LuaLoader);
                LuaExecutor.ExecuteAll();
            }
        }

        private void LoadExtensions()
        {
            ManagedPluginContainer = new PluginContainer();
            ManagedPluginLoader = new PluginLoader(PluginDirectory, ManagedPluginContainer);
            ManagedPluginLoader.LoadPlugins();
        }

        private void StartExtensions()
        {
            foreach (var pluginInfo in ManagedPluginContainer)
            {
                pluginInfo.Plugin.Initialize(this);
            }
        }
    }
}
