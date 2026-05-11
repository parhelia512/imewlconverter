using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Core.CodeGeneration;
using ImeWlConverter.Core.Filters;
using ImeWlConverter.Core.Pipeline;

namespace Studyzy.IMEWLConverter.Services;

/// <summary>
/// Orchestrates the full conversion pipeline for the WinForms UI.
/// Supports IProgress for real-time progress and CancellationToken for cancellation.
/// </summary>
public sealed class ConversionService : IConversionOrchestrator
{
    private readonly IChineseConverter _chineseConverter;
    private readonly CodeGenerationService _codeGenerationService;

    public ConversionService(
        IChineseConverter chineseConverter,
        CodeGenerationService codeGenerationService)
    {
        _chineseConverter = chineseConverter;
        _codeGenerationService = codeGenerationService;
    }

    public async Task<WinConversionResult> ConvertAsync(
        WinConversionRequest request,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken ct = default)
    {
        var errors = new StringBuilder();
        var files = request.InputFiles;
        var importer = request.Importer;
        var exporter = request.Exporter;

        // Build filter pipeline from FilterConfig
        var filters = BuildFilters(request.FilterConfig);
        var transforms = BuildTransforms(request.FilterConfig);
        var batchFilters = BuildBatchFilters(request.FilterConfig);
        var filterPipeline = new FilterPipeline(filters, transforms, batchFilters);

        if (request.MergeToOneFile)
        {
            return await ConvertMergedAsync(
                files, importer, exporter, filterPipeline, request, progress, errors, ct);
        }
        else
        {
            return await ConvertPerFileAsync(
                files, importer, exporter, filterPipeline, request, progress, errors, ct);
        }
    }

    private async Task<WinConversionResult> ConvertMergedAsync(
        IReadOnlyList<string> files,
        IFormatImporter importer,
        IFormatExporter exporter,
        FilterPipeline filterPipeline,
        WinConversionRequest request,
        IProgress<ProgressInfo>? progress,
        StringBuilder errors,
        CancellationToken ct)
    {
        var totalFiles = files.Count;

        // Phase 1: Import all files
        var allEntries = new List<WordEntry>();
        for (var i = 0; i < totalFiles; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = files[i];
            var fileName = Path.GetFileName(file);
            progress?.Report(new ProgressInfo(i + 1, totalFiles, $"正在导入文件 {i + 1}/{totalFiles}: {fileName}"));

            try
            {
                using var stream = File.OpenRead(file);
                var importResult = await importer.ImportAsync(stream, new ImportOptions(), ct);
                allEntries.AddRange(importResult.Entries);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                errors.AppendLine($"导入 {file} 失败: {ex.Message}");
            }
        }

        // Phase 2: Filter
        ct.ThrowIfCancellationRequested();
        progress?.Report(new ProgressInfo(0, allEntries.Count, "正在过滤..."));
        IReadOnlyList<WordEntry> entries = filterPipeline.Apply(allEntries);

        // Phase 3: Chinese conversion
        if (request.ChineseConversion != ChineseConversionMode.None)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new ProgressInfo(0, entries.Count, "正在转换简繁体..."));
            entries = ApplyChineseConversion(entries, request.ChineseConversion);
        }

        // Phase 4: Word rank generation
        if (request.WordRankGenerator != null)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new ProgressInfo(0, entries.Count, "正在生成词频..."));
            entries = await request.WordRankGenerator.GenerateRanksAsync(entries, ct);
        }

        // Phase 5: Code generation
        ct.ThrowIfCancellationRequested();
        progress?.Report(new ProgressInfo(0, entries.Count, "正在生成编码..."));
        var targetCodeType = CodeType.Pinyin;
        entries = _codeGenerationService.GenerateCodes(entries, targetCodeType, progress);

        var convertedCount = entries.Count;

        // Phase 6: Export
        ct.ThrowIfCancellationRequested();
        progress?.Report(new ProgressInfo(convertedCount, convertedCount, $"正在导出 {convertedCount} 条词条..."));

        using var outputStream = new MemoryStream();
        await exporter.ExportAsync(entries, outputStream, new ExportOptions(), ct);
        outputStream.Position = 0;
        using var reader = new StreamReader(outputStream);
        var content = await reader.ReadToEndAsync(ct);
        var exportLines = content.Split(["\r\n", "\n"], StringSplitOptions.None);

        return new WinConversionResult
        {
            ConvertedCount = convertedCount,
            ExportLines = exportLines,
            ErrorMessages = errors.ToString()
        };
    }

    private async Task<WinConversionResult> ConvertPerFileAsync(
        IReadOnlyList<string> files,
        IFormatImporter importer,
        IFormatExporter exporter,
        FilterPipeline filterPipeline,
        WinConversionRequest request,
        IProgress<ProgressInfo>? progress,
        StringBuilder errors,
        CancellationToken ct)
    {
        var totalFiles = files.Count;
        var totalConverted = 0;

        for (var i = 0; i < totalFiles; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = files[i];
            var fileName = Path.GetFileName(file);
            progress?.Report(new ProgressInfo(i + 1, totalFiles, $"正在处理文件 {i + 1}/{totalFiles}: {fileName}"));

            try
            {
                using var stream = File.OpenRead(file);
                var importResult = await importer.ImportAsync(stream, new ImportOptions(), ct);
                IReadOnlyList<WordEntry> fileEntries = filterPipeline.Apply(importResult.Entries.ToList());

                if (request.ChineseConversion != ChineseConversionMode.None)
                    fileEntries = ApplyChineseConversion(fileEntries, request.ChineseConversion);

                if (request.WordRankGenerator != null)
                    fileEntries = await request.WordRankGenerator.GenerateRanksAsync(fileEntries, ct);

                var outputFile = Path.Combine(
                    request.OutputDirectory ?? ".",
                    Path.GetFileNameWithoutExtension(file) + ".txt");
                using var outStream = File.Create(outputFile);
                await exporter.ExportAsync(fileEntries, outStream, new ExportOptions(), ct);

                totalConverted += fileEntries.Count;
                progress?.Report(new ProgressInfo(i + 1, totalFiles, $"已导出: {outputFile}"));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                errors.AppendLine($"处理 {file} 失败: {ex.Message}");
            }
        }

        return new WinConversionResult
        {
            ConvertedCount = totalConverted,
            ExportLines = null,
            ErrorMessages = errors.ToString()
        };
    }

    private IReadOnlyList<WordEntry> ApplyChineseConversion(
        IReadOnlyList<WordEntry> entries, ChineseConversionMode mode)
    {
        var result = new List<WordEntry>(entries.Count);
        foreach (var entry in entries)
        {
            var converted = mode switch
            {
                ChineseConversionMode.SimplifiedToTraditional =>
                    entry with { Word = _chineseConverter.ToTraditional(entry.Word) },
                ChineseConversionMode.TraditionalToSimplified =>
                    entry with { Word = _chineseConverter.ToSimplified(entry.Word) },
                _ => entry
            };
            result.Add(converted);
        }
        return result;
    }

    #region Filter Builders

    private static IList<IWordFilter> BuildFilters(FilterConfig config)
    {
        var filters = new List<IWordFilter>();
        if (config.NoFilter) return filters;
        if (config.IgnoreEnglish) filters.Add(new EnglishFilter());
        if (config.IgnoreFirstCJK) filters.Add(new FirstCJKFilter());

        if (config.WordLengthFrom > 1 || config.WordLengthTo < 9999)
            filters.Add(new LengthFilter { MinLength = config.WordLengthFrom, MaxLength = config.WordLengthTo });

        if (config.WordRankFrom > 1 || config.WordRankTo < 999999)
            filters.Add(new RankFilter { MinRank = config.WordRankFrom, MaxRank = config.WordRankTo });

        if (config.IgnoreSpace) filters.Add(new SpaceFilter());
        if (config.IgnorePunctuation)
        {
            filters.Add(new ChinesePunctuationFilter());
            filters.Add(new EnglishPunctuationFilter());
        }
        if (config.IgnoreNumber) filters.Add(new NumberFilter());
        if (config.IgnoreNoAlphabetCode) filters.Add(new NoAlphabetCodeFilter());
        return filters;
    }

    private static IList<IWordTransform> BuildTransforms(FilterConfig config)
    {
        var transforms = new List<IWordTransform>();
        if (config.NoFilter) return transforms;
        if (config.ReplaceEnglish) transforms.Add(new EnglishRemoveTransform());
        if (config.ReplacePunctuation)
        {
            transforms.Add(new EnglishPunctuationRemoveTransform());
            transforms.Add(new ChinesePunctuationRemoveTransform());
        }
        if (config.ReplaceSpace) transforms.Add(new SpaceRemoveTransform());
        if (config.ReplaceNumber) transforms.Add(new NumberRemoveTransform());
        return transforms;
    }

    private static IList<IBatchFilter> BuildBatchFilters(FilterConfig config)
    {
        var filters = new List<IBatchFilter>();
        if (config.NoFilter) return filters;
        if (config.WordRankPercentage < 100)
        {
            filters.Add(new RankPercentageFilter { Percentage = config.WordRankPercentage });
        }
        return filters;
    }

    #endregion
}
