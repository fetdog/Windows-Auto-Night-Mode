﻿using AutoDarkModeConfig;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.SwitchComponents.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoDarkModeSvc
{
    class ComponentManager
    {
        private static ComponentManager instance;
        public static ComponentManager Instance()
        {
            if (instance == null)
            {
                instance = new ComponentManager();
            }
            return instance;
        }

        private readonly List<ISwitchComponent> Components;
        private AdmConfigBuilder Builder { get; }
        private Theme lastSorting = Theme.Unknown;

        // Components
        private readonly ISwitchComponent AppsSwitch = new AppsSwitch();
        private readonly ISwitchComponent ColorFilterSwitch = new ColorFilterSwitch();
        private readonly ISwitchComponent OfficeSwitch = new OfficeSwitch();
        private readonly ISwitchComponent SystemSwitch = new SystemSwitch();
        //private ISwitchComponent TaskbarAccentColorSwitch;
        private readonly ISwitchComponent WallpaperSwitch = new WallpaperSwitch();

        /// <summary>
        /// Instructs all components to refresh their settings objects by injecting a new settings object
        /// from the currently available configuration instance
        /// </summary>
        public void UpdateSettings()
        {
            AppsSwitch.UpdateSettingsState(Builder.Config.AppsSwitch);
            ColorFilterSwitch.UpdateSettingsState(Builder.Config.ColorFilterSwitch);
            OfficeSwitch.UpdateSettingsState(Builder.Config.OfficeSwitch);
            SystemSwitch.UpdateSettingsState(Builder.Config.SystemSwitch);
            WallpaperSwitch.UpdateSettingsState(Builder.Config.WallpaperSwitch);
            //TaskbarAccentcolorSwitch.UpdateSettingsState(Builder.Config.TaskbarAccentColorSwitch);
        }
        ComponentManager()
        {
            Builder = AdmConfigBuilder.Instance();
            Components = new List<ISwitchComponent>
            {
                AppsSwitch,
                ColorFilterSwitch,
                OfficeSwitch,
                SystemSwitch,
                //TaskbarAccentColorSwitch
                WallpaperSwitch
            };
            UpdateSettings();
        }

        /// <summary>
        /// Calls the disable hooks for all components
        /// </summary>
        public void InvokeDisableHooks()
        {
            Components.ForEach(c => c.DisableHook());
        }

        /// <summary>
        /// Sets the one time force flag for all modules
        /// </summary>
        public void ForceAll()
        {
            Components.ForEach(c =>  c.ForceSwitch = true);
        }

        /// <summary>
        /// Checks whether components need an update, which is true: <br/>
        /// if their <see cref="ISwitchComponent.ComponentNeedsUpdate(Theme)"/> method returns true <br/>
        /// or the module has the force switch flag enabled <br/>
        /// or the module was disabled and is still initialized
        /// <param name="newTheme">The theme that should be checked against</param>
        /// <returns>a list of components that require an update</returns>
        /// </summary>
        public List<ISwitchComponent> GetComponentsToUpdate(Theme newTheme)
        {
            List<ISwitchComponent> shouldUpdate = new();
            foreach (ISwitchComponent c in Components)
            {
                // require update if theme mode is enabled, the module is enabled and compatible with theme mode
                if (c.Enabled && c.ThemeHandlerCompatibility && Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (c.ComponentNeedsUpdate(newTheme))
                    {
                        shouldUpdate.Add(c);
                    }
                }
                // require update if module is enabled and theme mode is disabled (previously known as classic mode)
                else if (c.Enabled && !Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (c.ComponentNeedsUpdate(newTheme))
                    {
                        shouldUpdate.Add(c);
                    }
                }
                // require update if the component is no longer enabled but still initialized. this will trigger the deinit hook
                else if (!c.Enabled && c.Initialized)
                {
                    shouldUpdate.Add(c);
                }
                // if the force flag is set to true, we also need to update
                else if (c.ForceSwitch)
                {
                    shouldUpdate.Add(c);
                }
            }
            return shouldUpdate;
        }

        /// <summary>
        /// Runs all components in a synchronized context and sorts them prior to execution based on their priorities
        /// </summary>
        /// <param name="components">The components to be run</param>
        /// <param name="newTheme">the requested theme to switch to</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Run(List<ISwitchComponent> components, Theme newTheme)
        {
            if (newTheme == Theme.Dark && lastSorting != Theme.Dark)
            {
                components.Sort((x, y) => x.PriorityToDark.CompareTo(y.PriorityToDark));
                lastSorting = Theme.Dark;
            }
            else if (newTheme == Theme.Light && lastSorting != Theme.Light)
            {
                components.Sort((x, y) => x.PriorityToLight.CompareTo(y.PriorityToLight));
                lastSorting = Theme.Light;
            }
            components.ForEach(c =>
            {
                if (Builder.Config.WindowsThemeMode.Enabled && c.ThemeHandlerCompatibility)
                {
                    c.Switch(newTheme);
                }
                else if (!Builder.Config.WindowsThemeMode.Enabled)
                {
                    c.Switch(newTheme);
                }
            });
        }
    }
}
