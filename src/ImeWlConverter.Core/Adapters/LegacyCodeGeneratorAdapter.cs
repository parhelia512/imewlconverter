using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using Studyzy.IMEWLConverter.Generaters;

namespace ImeWlConverter.Core.Adapters;

/// <summary>
/// Adapts legacy <see cref="IWordCodeGenerater"/> to new <see cref="ICodeGenerator"/> interface.
/// </summary>
public sealed class LegacyCodeGeneratorAdapter : ICodeGenerator
{
    private readonly IWordCodeGenerater _legacyGenerator;

    public LegacyCodeGeneratorAdapter(IWordCodeGenerater legacyGenerator, CodeType supportedType)
    {
        _legacyGenerator = legacyGenerator;
        SupportedType = supportedType;
    }

    public CodeType SupportedType { get; }

    public bool Is1Char1Code => _legacyGenerator.Is1Char1Code;

    public WordCode GenerateCode(string word)
    {
        var codes = _legacyGenerator.GetCodeOfString(word);
        if (codes == null || codes.Count == 0)
            return new WordCode { Segments = [] };

        var segments = new List<IReadOnlyList<string>>(codes.Count);
        foreach (var segment in codes)
        {
            segments.Add(segment.ToList());
        }

        return new WordCode { Segments = segments };
    }
}
