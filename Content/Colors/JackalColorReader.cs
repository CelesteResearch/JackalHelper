// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.JackalHelper.Content.Colors
{
    public static class JackalColorReader
    {
        private static readonly PropertyInfo[] ColorProperties = typeof(PackedColor).GetProperties();

        public static PackedColor GetColor(string color)
        {
            foreach (var property in ColorProperties)
                if (color.Equals(property.Name, StringComparison.OrdinalIgnoreCase))
                    return (PackedColor)property.GetValue(default(PackedColor), null);
            try
            {
                return SourceHexColor.HexToColor(color.Replace("#", ""));
            }
            catch
            {
                return PackedColor.Transparent;
            }
        }
    }
}
