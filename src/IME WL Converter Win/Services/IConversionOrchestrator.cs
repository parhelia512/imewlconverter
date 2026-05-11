using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;

namespace Studyzy.IMEWLConverter.Services;

/// <summary>
/// Request DTO for a WinForms conversion operation.
/// </summary>
public sealed class WinConversionRequest
{
    public required IFormatImporter Importer { get; init; }
    public required IFormatExporter Exporter { get; init; }
    public required IReadOnlyList<string> InputFiles { get; init; }
    public required FilterConfig FilterConfig { get; init; }
    public ChineseConversionMode ChineseConversion { get; init; } = ChineseConversionMode.None;
    public IWordRankGenerator? WordRankGenerator { get; init; }
    public bool MergeToOneFile { get; init; } = true;
    public string? OutputDirectory { get; init; }
    public bool StreamExport { get; init; }
    public string? StreamExportPath { get; init; }
}

/// <summary>
/// Result DTO from a conversion operation.
/// </summary>
public sealed class WinConversionResult
{
    public int ConvertedCount { get; init; }
    public IReadOnlyList<string>? ExportLines { get; init; }
    public string ErrorMessages { get; init; } = "";
}

/// <summary>
/// Orchestrates the full conversion pipeline for the WinForms UI.
/// </summary>
public interface IConversionOrchestrator
{
    Task<WinConversionResult> ConvertAsync(
        WinConversionRequest request,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken ct = default);
}
