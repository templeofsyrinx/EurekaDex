﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Interface;
using ImGuiNET;

namespace DeepDungeonDex
{
    internal class Font : IDisposable
    {
        private ImFontConfigPtr _fontCfg;
        private ImFontConfigPtr _fontCfgMerge;
        private (GCHandle, int) _gameSymFont;
        private ImVector _ranges;
        private ImVector _jpRanges;
        private ImVector _krRanges;
        private ImVector _tcRanges;
        private ImVector _scRanges;
        private (GCHandle, int, float) _regularFont;
        private (GCHandle, int, float) _jpFont;
        private (GCHandle, int, float) _krFont;
        private (GCHandle, int, float) _tcFont;
        private (GCHandle, int, float) _scFont;
        private GCHandle _symRange = GCHandle.Alloc(
            new ushort[] {
                0xE020,
                0xE0DB,
                0,
            },
            GCHandleType.Pinned
        );
        internal static ImFontPtr RegularFont;

        private readonly Configuration _config;

        public unsafe Font(Configuration config)
        {
            _config = config;
            SetUpRanges();
            SetUpFonts();
            _fontCfg = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig()) { FontDataOwnedByAtlas = false };
            _fontCfgMerge = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig()) { FontDataOwnedByAtlas = false, MergeMode = true };
            var gameSym = new HttpClient().GetAsync("https://img.finalfantasyxiv.com/lds/pc/global/fonts/FFXIV_Lodestone_SSF.ttf")
                .Result
                .Content
                .ReadAsByteArrayAsync()
                .Result;
            _gameSymFont = (
                GCHandle.Alloc(gameSym, GCHandleType.Pinned),
                gameSym.Length
            );
        }

        public byte[] GetResource(string path)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path)!;
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        public unsafe void SetUpRanges()
        {
            //Thanks anna for showing me how to add fonts and ranges to ImGui
#pragma warning disable CS8632
            static ImVector buildRange(IReadOnlyList<ushort>? chars, params IntPtr[] ranges)
            {
#pragma warning restore CS8632
                var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
                // text
                foreach (var range in ranges)
                {
                    builder.AddRanges(range);
                }
                // chars
                if (chars != null)
                {
                    for (var i = 0; i < chars.Count; i += 2)
                    {
                        if (chars[i] == 0)
                        {
                            break;
                        }
                        for (var j = (uint)chars[i]; j <= chars[i + 1]; j++)
                        {
                            builder.AddChar((ushort)j);
                        }
                    }
                }
                // various symbols
                builder.AddText("←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～");
                // French
                builder.AddText("Œœ");
                // Romanian
                builder.AddText("ĂăÂâÎîȘșȚț");
                // "Enclosed Alphanumerics" (partial) https://www.compart.com/en/unicode/block/U+2460
                for (var i = 0x2460; i <= 0x24B5; i++)
                {
                    builder.AddChar((char)i);
                }
                builder.AddChar('⓪');
                builder.BuildRanges(out var result);
                builder.Destroy();
                return result;
            }

            var ranges = new List<IntPtr>
            {
                ImGui.GetIO().Fonts.GetGlyphRangesDefault(),
                ImGui.GetIO().Fonts.GetGlyphRangesJapanese(),
            };

            _ranges = buildRange(null, ranges.ToArray());
            _jpRanges = buildRange(GlyphRangesJapanese.GlyphRanges);
            _krRanges = buildRange(null, ImGui.GetIO().Fonts.GetGlyphRangesKorean());
            _tcRanges = buildRange(null, ImGui.GetIO().Fonts.GetGlyphRangesChineseFull());
            _scRanges = buildRange(null, ImGui.GetIO().Fonts.GetGlyphRangesChineseSimplifiedCommon());
        }

        public void SetUpFonts()
        {
            var regular = GetResource("DeepDungeonDex.fonts.NotoSans-Regular.ttf");
            var jp = GetResource("DeepDungeonDex.fonts.NotoSansJP-Regular.otf");
            var kr = GetResource("DeepDungeonDex.fonts.NotoSansKR-Regular.otf");
            var tc = GetResource("DeepDungeonDex.fonts.NotoSansTC-Regular.otf");
            var sc = GetResource("DeepDungeonDex.fonts.NotoSansSC-Regular.otf");

            if (_regularFont.Item1.IsAllocated)
                _regularFont.Item1.Free();
            if (_jpFont.Item1.IsAllocated)
                _jpFont.Item1.Free();
            if (_krFont.Item1.IsAllocated)
                _krFont.Item1.Free();
            if (_tcFont.Item1.IsAllocated)
                _tcFont.Item1.Free();
            if (_scFont.Item1.IsAllocated)
                _scFont.Item1.Free();

            _regularFont = (GCHandle.Alloc(regular, GCHandleType.Pinned), regular.Length, 1f);
            _jpFont = (GCHandle.Alloc(jp, GCHandleType.Pinned), jp.Length, 1f);
            _krFont = (GCHandle.Alloc(kr, GCHandleType.Pinned), kr.Length, 1f);
            _tcFont = (GCHandle.Alloc(tc, GCHandleType.Pinned), tc.Length, 1f);
            _scFont = (GCHandle.Alloc(sc, GCHandleType.Pinned), sc.Length, 1f);
        }

        public void BuildFonts()
        {
            RegularFont = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(_regularFont.Item1.AddrOfPinnedObject(), _regularFont.Item2, _config.FontSize, _fontCfg, _ranges.Data);
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(_jpFont.Item1.AddrOfPinnedObject(), _jpFont.Item2, _config.FontSize, _fontCfgMerge, _jpRanges.Data);
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(_krFont.Item1.AddrOfPinnedObject(), _krFont.Item2, _config.FontSize, _fontCfgMerge, _krRanges.Data);
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(_tcFont.Item1.AddrOfPinnedObject(), _tcFont.Item2, _config.FontSize, _fontCfgMerge, _tcRanges.Data);
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(_scFont.Item1.AddrOfPinnedObject(), _scFont.Item2, _config.FontSize, _fontCfgMerge, _scRanges.Data);
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(_gameSymFont.Item1.AddrOfPinnedObject(), _gameSymFont.Item2, _config.FontSize, _fontCfgMerge, _symRange.AddrOfPinnedObject());
        }

        public void Dispose()
        {
            if (_regularFont.Item1.IsAllocated)
                _regularFont.Item1.Free();
            if (_jpFont.Item1.IsAllocated)
                _jpFont.Item1.Free();
            if (_krFont.Item1.IsAllocated)
                _krFont.Item1.Free();
            if (_tcFont.Item1.IsAllocated)
                _tcFont.Item1.Free();
            if (_scFont.Item1.IsAllocated)
                _scFont.Item1.Free();
            if (_symRange.IsAllocated)
                _symRange.Free();
            if (_gameSymFont.Item1.IsAllocated)
                _gameSymFont.Item1.Free();

            _fontCfg.Destroy();
            _fontCfgMerge.Destroy();
        }
    }
}
