﻿using System;
using System.Collections.Generic;
using System.Text;
using ByteSizeLib;

namespace NuGetCalcWeb.ViewModels
{
    public static class TemplateExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return true;
            using (var e = source.GetEnumerator())
                return !e.MoveNext();
        }

        public static string Times(this string s, int count)
        {
            if (count == 0) return "";
            if (count < 0) throw new ArgumentOutOfRangeException();

            var sb = new StringBuilder(s.Length * count);
            for (var i = 0; i < count; i++)
                sb.Append(s);
            return sb.ToString();
        }

        public static string HumanizeBytes(this long length)
        {
            var b = ByteSize.FromBytes(length);
            return $"{b.LargestWholeNumberValue:0.##} {b.LargestWholeNumberSymbol}";
        }
    }
}