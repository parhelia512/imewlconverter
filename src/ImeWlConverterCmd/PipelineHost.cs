#nullable enable

using System;
using System.Collections.Generic;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Core.Adapters;
using ImeWlConverter.Core.Pipeline;

namespace Studyzy.IMEWLConverter;

/// <summary>
/// Assembles the conversion pipeline by bridging legacy format registrations
/// into the new pipeline architecture via adapters.
/// </summary>
internal static class PipelineHost
{
    /// <summary>
    /// Build a configured ConversionPipeline using legacy format implementations
    /// wrapped in adapters.
    /// </summary>
    public static ConversionPipeline BuildPipeline(
        IDictionary<string, IWordLibraryImport> imports,
        IDictionary<string, IWordLibraryExport> exports,
        IDictionary<string, string> names,
        IProgress<ProgressInfo>? progress = null,
        FilterPipeline? filterPipeline = null)
    {
        var importers = new List<IFormatImporter>();
        foreach (var kvp in imports)
        {
            var displayName = names.TryGetValue(kvp.Key, out var n) ? n : kvp.Key;
            var metadata = new FormatMetadata(
                kvp.Key,
                displayName,
                0,
                SupportsImport: true,
                SupportsExport: exports.ContainsKey(kvp.Key));
            importers.Add(new LegacyImporterAdapter(kvp.Value, metadata));
        }

        var exporterList = new List<IFormatExporter>();
        foreach (var kvp in exports)
        {
            var displayName = names.TryGetValue(kvp.Key, out var n) ? n : kvp.Key;
            var metadata = new FormatMetadata(
                kvp.Key,
                displayName,
                0,
                SupportsImport: imports.ContainsKey(kvp.Key),
                SupportsExport: true);
            exporterList.Add(new LegacyExporterAdapter(kvp.Value, metadata));
        }

        return new ConversionPipeline(importers, exporterList, progress, filterPipeline);
    }
}
