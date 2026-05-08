using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using Studyzy.IMEWLConverter;
using Studyzy.IMEWLConverter.Entities;
using OldCodeType = Studyzy.IMEWLConverter.Entities.CodeType;
using NewCodeType = ImeWlConverter.Abstractions.Enums.CodeType;

namespace ImeWlConverterMac.Adapters;

/// <summary>
/// Adapts new IFormatExporter to legacy IWordLibraryExport interface for GUI compatibility.
/// </summary>
public sealed class NewFormatExporterAdapter : IWordLibraryExport
{
    private readonly IFormatExporter _exporter;

    public NewFormatExporterAdapter(IFormatExporter exporter)
    {
        _exporter = exporter;
    }

    public Encoding Encoding => Encoding.UTF8;
    public OldCodeType CodeType => OldCodeType.Pinyin;

    public event Action<string>? ExportErrorNotice;

    public IList<string> Export(WordLibraryList wlList)
    {
        var entries = ConvertToWordEntries(wlList);

        using var ms = new MemoryStream();
        _exporter.ExportAsync(entries, ms).GetAwaiter().GetResult();
        ms.Position = 0;

        using var reader = new StreamReader(ms, Encoding.UTF8);
        var content = reader.ReadToEnd();

        return new List<string> { content };
    }

    public string ExportLine(WordLibrary wl)
    {
        var entry = ConvertToWordEntry(wl);
        if (entry == null) return string.Empty;

        var entries = new List<WordEntry> { entry };
        using var ms = new MemoryStream();
        _exporter.ExportAsync(entries, ms).GetAwaiter().GetResult();
        ms.Position = 0;

        using var reader = new StreamReader(ms, Encoding.UTF8);
        return reader.ReadToEnd().TrimEnd('\r', '\n');
    }

    private static List<WordEntry> ConvertToWordEntries(WordLibraryList wlList)
    {
        var entries = new List<WordEntry>(wlList.Count);
        foreach (var wl in wlList)
        {
            var entry = ConvertToWordEntry(wl);
            if (entry != null)
                entries.Add(entry);
        }
        return entries;
    }

    private static WordEntry? ConvertToWordEntry(WordLibrary wl)
    {
        if (string.IsNullOrEmpty(wl.Word))
            return null;

        WordCode? code = null;
        if (wl.Codes != null && wl.Codes.Count > 0)
        {
            var segments = new List<IReadOnlyList<string>>(wl.Codes.Count);
            foreach (var segment in wl.Codes)
            {
                segments.Add(segment.ToList());
            }
            code = new WordCode { Segments = segments };
        }

        return new WordEntry
        {
            Word = wl.Word,
            Rank = wl.Rank,
            IsEnglish = wl.IsEnglish,
            CodeType = MapOldCodeTypeToNew(wl.CodeType),
            Code = code
        };
    }

    private static NewCodeType MapOldCodeTypeToNew(OldCodeType old) => old switch
    {
        OldCodeType.Pinyin => NewCodeType.Pinyin,
        OldCodeType.Wubi => NewCodeType.Wubi86,
        OldCodeType.Wubi98 => NewCodeType.Wubi98,
        OldCodeType.WubiNewAge => NewCodeType.WubiNewAge,
        OldCodeType.Zhengma => NewCodeType.Zhengma,
        OldCodeType.Cangjie => NewCodeType.Cangjie5,
        OldCodeType.TerraPinyin => NewCodeType.TerraPinyin,
        OldCodeType.Zhuyin => NewCodeType.Zhuyin,
        OldCodeType.English => NewCodeType.English,
        OldCodeType.UserDefine => NewCodeType.UserDefine,
        OldCodeType.NoCode => NewCodeType.NoCode,
        OldCodeType.QingsongErbi => NewCodeType.QingsongErbi,
        OldCodeType.ChaoqiangErbi => NewCodeType.ChaoqiangErbi,
        OldCodeType.XiandaiErbi => NewCodeType.XiandaiErbi,
        OldCodeType.ChaoqingYinxin => NewCodeType.YinxingErbi,
        OldCodeType.Chaoyin => NewCodeType.Chaoyin,
        OldCodeType.Yong => NewCodeType.Yong,
        OldCodeType.InnerCode => NewCodeType.InnerCode,
        OldCodeType.UserDefinePhrase => NewCodeType.UserDefinePhrase,
        OldCodeType.Unknown => NewCodeType.Unknown,
        _ => NewCodeType.Pinyin
    };
}
