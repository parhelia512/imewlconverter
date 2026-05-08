using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using Studyzy.IMEWLConverter.Entities;
using OldBatchFilter = Studyzy.IMEWLConverter.Filters.IBatchFilter;

namespace ImeWlConverter.Core.Adapters;

/// <summary>
/// Adapts legacy <see cref="OldBatchFilter"/> to new <see cref="IBatchFilter"/> interface.
/// </summary>
public sealed class LegacyBatchFilterAdapter : IBatchFilter
{
    private readonly OldBatchFilter _legacyFilter;

    public LegacyBatchFilterAdapter(OldBatchFilter legacyFilter)
    {
        _legacyFilter = legacyFilter;
    }

    public IReadOnlyList<WordEntry> Filter(IReadOnlyList<WordEntry> entries)
    {
        var legacyList = new WordLibraryList();
        foreach (var entry in entries)
        {
            legacyList.Add(new WordLibrary
            {
                Word = entry.Word,
                Rank = entry.Rank,
                IsEnglish = entry.IsEnglish
            });
        }

        var filtered = _legacyFilter.Filter(legacyList);
        var result = new List<WordEntry>(filtered.Count);
        foreach (var wl in filtered)
        {
            result.Add(new WordEntry
            {
                Word = wl.Word,
                Rank = wl.Rank,
                IsEnglish = wl.IsEnglish,
                CodeType = LegacyImporterAdapter.MapCodeType(wl.CodeType)
            });
        }

        return result;
    }
}
