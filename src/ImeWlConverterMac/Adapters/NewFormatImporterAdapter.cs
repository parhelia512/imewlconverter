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
/// Adapts new IFormatImporter to legacy IWordLibraryImport interface for GUI compatibility.
/// </summary>
public sealed class NewFormatImporterAdapter : IWordLibraryImport
{
    private readonly IFormatImporter _importer;

    public NewFormatImporterAdapter(IFormatImporter importer)
    {
        _importer = importer;
    }

    public int CountWord { get; set; }
    public int CurrentStatus { get; set; }
    public bool IsText => !_importer.Metadata.IsBinary;
    public OldCodeType CodeType => MapCodeType(_importer.Metadata.Id);

    public event Action<string>? ImportLineErrorNotice;

    public WordLibraryList Import(string path)
    {
        using var stream = File.OpenRead(path);
        var result = _importer.ImportAsync(stream).GetAwaiter().GetResult();
        var wlList = new WordLibraryList();

        foreach (var entry in result.Entries)
        {
            var wl = ConvertToWordLibrary(entry);
            if (wl != null)
                wlList.Add(wl);
        }

        CountWord = wlList.Count;
        return wlList;
    }

    public WordLibraryList ImportLine(string str)
    {
        // Not all new importers support line-by-line import; return empty list
        return new WordLibraryList();
    }

    private static WordLibrary? ConvertToWordLibrary(WordEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Word))
            return null;

        var wl = new WordLibrary
        {
            Word = entry.Word,
            Rank = entry.Rank,
            IsEnglish = entry.IsEnglish,
            CodeType = MapNewCodeTypeToOld(entry.CodeType)
        };

        if (entry.Code != null)
        {
            var code = new Code();
            foreach (var segment in entry.Code.Segments)
            {
                code.Add(segment.ToList());
            }
            wl.Codes = code;
        }

        return wl;
    }

    private static OldCodeType MapNewCodeTypeToOld(NewCodeType newType) => newType switch
    {
        NewCodeType.Pinyin => OldCodeType.Pinyin,
        NewCodeType.Wubi86 => OldCodeType.Wubi,
        NewCodeType.Wubi98 => OldCodeType.Wubi98,
        NewCodeType.WubiNewAge => OldCodeType.WubiNewAge,
        NewCodeType.Zhengma => OldCodeType.Zhengma,
        NewCodeType.Cangjie5 => OldCodeType.Cangjie,
        NewCodeType.TerraPinyin => OldCodeType.TerraPinyin,
        NewCodeType.Zhuyin => OldCodeType.Zhuyin,
        NewCodeType.English => OldCodeType.English,
        NewCodeType.UserDefine => OldCodeType.UserDefine,
        NewCodeType.NoCode => OldCodeType.NoCode,
        NewCodeType.QingsongErbi => OldCodeType.QingsongErbi,
        NewCodeType.ChaoqiangErbi => OldCodeType.ChaoqiangErbi,
        NewCodeType.XiandaiErbi => OldCodeType.XiandaiErbi,
        NewCodeType.YinxingErbi => OldCodeType.ChaoqingYinxin,
        NewCodeType.Chaoyin => OldCodeType.Chaoyin,
        NewCodeType.Yong => OldCodeType.Yong,
        NewCodeType.InnerCode => OldCodeType.InnerCode,
        NewCodeType.UserDefinePhrase => OldCodeType.UserDefinePhrase,
        NewCodeType.Unknown => OldCodeType.Unknown,
        _ => OldCodeType.Pinyin
    };

    private static OldCodeType MapCodeType(string formatId) => formatId switch
    {
        "wb86" => OldCodeType.Wubi,
        "wb98" => OldCodeType.Wubi98,
        "wbnewage" => OldCodeType.WubiNewAge,
        "qqwb" => OldCodeType.Wubi,
        "xywb" => OldCodeType.Wubi,
        "cjpt" => OldCodeType.Cangjie,
        "rime" => OldCodeType.Pinyin,
        _ => OldCodeType.Pinyin
    };
}
