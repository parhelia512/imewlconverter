using System.Collections.Generic;
using System.Linq;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Formats;
using Microsoft.Extensions.DependencyInjection;
using Studyzy.IMEWLConverter;

namespace ImeWlConverterMac.Adapters;

/// <summary>
/// Registers all known IME formats using the new format system and wraps them
/// in legacy interface adapters for GUI compatibility.
/// </summary>
public static class FormatRegistrar
{
    /// <summary>
    /// Registers all formats keyed by display name with sort index for GUI usage.
    /// Returns legacy interface dictionaries compatible with MainBody.
    /// </summary>
    public static (Dictionary<string, IWordLibraryImport> imports, Dictionary<string, IWordLibraryExport> exports, List<string> sortedImportNames, List<string> sortedExportNames)
        RegisterAllForGui()
    {
        var services = new ServiceCollection();
        services.AddAllFormats();
        var sp = services.BuildServiceProvider();

        var importers = sp.GetServices<IFormatImporter>().ToList();
        var exporters = sp.GetServices<IFormatExporter>().ToList();

        var imports = new Dictionary<string, IWordLibraryImport>();
        var exports = new Dictionary<string, IWordLibraryExport>();
        var importItems = new List<(string name, int index)>();
        var exportItems = new List<(string name, int index)>();

        foreach (var importer in importers)
        {
            var displayName = importer.Metadata.DisplayName;
            var adapter = new NewFormatImporterAdapter(importer);
            imports[displayName] = adapter;
            importItems.Add((displayName, importer.Metadata.SortOrder));
        }

        foreach (var exporter in exporters)
        {
            var displayName = exporter.Metadata.DisplayName;
            var adapter = new NewFormatExporterAdapter(exporter);
            exports[displayName] = adapter;
            exportItems.Add((displayName, exporter.Metadata.SortOrder));
        }

        var sortedImportNames = importItems.OrderBy(x => x.index).Select(x => x.name).ToList();
        var sortedExportNames = exportItems.OrderBy(x => x.index).Select(x => x.name).ToList();

        return (imports, exports, sortedImportNames, sortedExportNames);
    }
}
