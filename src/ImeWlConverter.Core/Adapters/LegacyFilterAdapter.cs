using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using Studyzy.IMEWLConverter.Entities;
using Studyzy.IMEWLConverter.Filters;

namespace ImeWlConverter.Core.Adapters;

/// <summary>
/// Adapts legacy <see cref="ISingleFilter"/> to new <see cref="IWordFilter"/> interface.
/// </summary>
public sealed class LegacyFilterAdapter : IWordFilter
{
    private readonly ISingleFilter _legacyFilter;

    public LegacyFilterAdapter(ISingleFilter legacyFilter)
    {
        _legacyFilter = legacyFilter;
    }

    public bool ShouldKeep(WordEntry entry)
    {
        var wl = new WordLibrary
        {
            Word = entry.Word,
            Rank = entry.Rank,
            IsEnglish = entry.IsEnglish
        };
        return _legacyFilter.IsKeep(wl);
    }
}
