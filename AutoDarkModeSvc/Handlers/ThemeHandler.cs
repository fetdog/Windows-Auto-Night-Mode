﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using AutoDarkModeSvc.Config;

/*
 * Source: https://github.com/kuchienkz/KAWAII-Theme-Swithcer/blob/master/KAWAII%20Theme%20Switcher/KAWAII%20Theme%20Helper.cs
 * Originally created by Kuchienkz.
 * Email: wahyu.darkflame@gmail.com
 * Licensed under: GNU General Public License v3.0
 * 
 * Other Contributors (modified by):
 * Armin2208
 * Spiritreader
*/

namespace AutoDarkModeSvc.Handlers
{
    public static class ThemeHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        [ComImport, Guid("D23CC733-5522-406D-8DFB-B3CF5EF52A71"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITheme
        {
            [DispId(0x60010000)]
            string DisplayName
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
            [DispId(0x60010001)]
            string VisualStyle
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
        }
        [ComImport, Guid("0646EBBE-C1B7-4045-8FD0-FFD65D3FC792"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IThemeManager
        {
            [DispId(0x60010000)]
            ITheme CurrentTheme
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ApplyTheme([In, MarshalAs(UnmanagedType.BStr)] string bstrThemePath);
        }
        [ComImport, Guid("A2C56C2A-E63A-433E-9953-92E94F0122EA"), CoClass(typeof(ThemeManagerClass))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public interface ThemeManager : IThemeManager { }
        [ComImport, Guid("C04B329E-5823-4415-9C93-BA44688947B0"), ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate)]
        public class ThemeManagerClass : IThemeManager, ThemeManager
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ApplyTheme([In, MarshalAs(UnmanagedType.BStr)] string bstrThemePath);
            [DispId(0x60010000)]
            public virtual extern ITheme CurrentTheme
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
        }
        private static class NativeMethods
        {
            [DllImport("UxTheme.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsThemeActive();
        }
        public static string GetCurrentThemeName()
        {
            return new ThemeManagerClass().CurrentTheme.DisplayName;
        }
        public static void Apply(string themeFilePath)
        {
            Thread thread = new(() =>
            {
                try
                {
                    //PowerHandler.DisableEnergySaver(AdmConfigBuilder.Instance().Config);
                    new ThemeManagerClass().ApplyTheme(themeFilePath);
                    GlobalState.Instance().CurrentWindowsThemeName = GetCurrentThemeName();
                    //PowerHandler.RestoreEnergySaver(AdmConfigBuilder.Instance().Config);
                    Logger.Info($"applied theme \"{themeFilePath}\" successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"couldn't apply theme \"{themeFilePath}\"");
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "theme handler thread was interrupted");
            }
        }
        public static string GetCurrentVisualStyleName()
        {
            return Path.GetFileName(new ThemeManagerClass().CurrentTheme.VisualStyle);
        }
        public static string GetThemeStatus()
        {
            return NativeMethods.IsThemeActive() ? "running" : "stopped";
        }
    }
}