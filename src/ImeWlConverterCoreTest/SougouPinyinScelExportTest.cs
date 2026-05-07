using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Studyzy.IMEWLConverter.Entities;
using Studyzy.IMEWLConverter.IME;

namespace Studyzy.IMEWLConverter.Test;

[TestFixture]
internal class SougouPinyinScelExportTest
{
    private SougouPinyinScel exporter;
    private string tempDir;

    [OneTimeSetUp]
    public void Setup()
    {
        exporter = new SougouPinyinScel();
        tempDir = Path.Combine(Path.GetTempPath(), "scel_export_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    [Test]
    public void TestExportBasicScel()
    {
        var wlList = new WordLibraryList
        {
            new WordLibrary { Word = "测试", PinYin = new[] { "ce", "shi" }, Rank = 1 },
            new WordLibrary { Word = "你好", PinYin = new[] { "ni", "hao" }, Rank = 2 },
            new WordLibrary { Word = "世界", PinYin = new[] { "shi", "jie" }, Rank = 3 }
        };

        var outputPath = Path.Combine(tempDir, "basic.scel");
        exporter.ExportToBinary(wlList, outputPath);

        Assert.That(File.Exists(outputPath), Is.True);
        var data = File.ReadAllBytes(outputPath);

        // 验证 magic number
        Assert.That(data[0], Is.EqualTo(0x40));
        Assert.That(data[1], Is.EqualTo(0x15));
        Assert.That(data[2], Is.EqualTo(0x00));
        Assert.That(data[3], Is.EqualTo(0x00));
        Assert.That(data[4], Is.EqualTo(0x44)); // 'D'
        Assert.That(data[5], Is.EqualTo(0x43)); // 'C'
        Assert.That(data[6], Is.EqualTo(0x53)); // 'S'
        Assert.That(data[7], Is.EqualTo(0x01));

        // 验证词组数
        var groupCount = BitConverter.ToInt32(data, 0x120);
        Assert.That(groupCount, Is.EqualTo(3));

        // 验证词条总数
        var wordCount = BitConverter.ToInt32(data, 0x124);
        Assert.That(wordCount, Is.EqualTo(3));

        // 验证拼音表条目数
        var pyCount = BitConverter.ToInt32(data, 0x1540);
        Assert.That(pyCount, Is.EqualTo(413));
    }

    [Test]
    public void TestExportWithSamePinyin()
    {
        // 同音词测试
        var wlList = new WordLibraryList
        {
            new WordLibrary { Word = "世界", PinYin = new[] { "shi", "jie" }, Rank = 1 },
            new WordLibrary { Word = "实际", PinYin = new[] { "shi", "ji" }, Rank = 2 },
            new WordLibrary { Word = "石阶", PinYin = new[] { "shi", "jie" }, Rank = 3 }
        };

        var outputPath = Path.Combine(tempDir, "same_pinyin.scel");
        exporter.ExportToBinary(wlList, outputPath);

        var data = File.ReadAllBytes(outputPath);

        // "世界"和"石阶"拼音相同(shi'jie)，应归为一组
        // "实际"拼音不同(shi'ji)，独立一组
        var groupCount = BitConverter.ToInt32(data, 0x120);
        Assert.That(groupCount, Is.EqualTo(2)); // 2个词组

        var wordCount = BitConverter.ToInt32(data, 0x124);
        Assert.That(wordCount, Is.EqualTo(3)); // 3个词条
    }

    [Test]
    public void TestExportMetaInfo()
    {
        var wlList = new WordLibraryList
        {
            new WordLibrary { Word = "深蓝", PinYin = new[] { "shen", "lan" }, Rank = 1 }
        };

        var outputPath = Path.Combine(tempDir, "meta.scel");
        exporter.ExportToBinary(wlList, outputPath);

        var data = File.ReadAllBytes(outputPath);

        // 验证名称
        var nameBytes = new byte[520];
        Array.Copy(data, 0x130, nameBytes, 0, 520);
        var name = Encoding.Unicode.GetString(nameBytes);
        var nameEnd = name.IndexOf('\0');
        name = name[..nameEnd];
        Assert.That(name, Is.EqualTo("深蓝词库转换"));

        // 验证描述
        var infoBytes = new byte[2048];
        Array.Copy(data, 0x540, infoBytes, 0, 2048);
        var info = Encoding.Unicode.GetString(infoBytes);
        var infoEnd = info.IndexOf('\0');
        info = info[..infoEnd];
        Assert.That(info, Is.EqualTo("由深蓝词库转换工具生成"));
    }

    [Test]
    public void TestRoundTrip()
    {
        // 往返测试：导出后再导入，验证数据一致
        var wlList = new WordLibraryList
        {
            new WordLibrary { Word = "深蓝测试", PinYin = new[] { "shen", "lan", "ce", "shi" }, Rank = 1 },
            new WordLibrary { Word = "词库转换", PinYin = new[] { "ci", "ku", "zhuan", "huan" }, Rank = 2 },
            new WordLibrary { Word = "你好世界", PinYin = new[] { "ni", "hao", "shi", "jie" }, Rank = 3 }
        };

        var outputPath = Path.Combine(tempDir, "roundtrip.scel");
        exporter.ExportToBinary(wlList, outputPath);

        // 重新导入
        var importer = new SougouPinyinScel();
        var imported = importer.Import(outputPath);

        Assert.That(imported.Count, Is.EqualTo(3));

        // 验证词条和拼音（顺序可能因拼音排序而变化）
        var wordSet = new System.Collections.Generic.HashSet<string>();
        foreach (var wl in imported)
        {
            wordSet.Add(wl.Word + "|" + wl.PinYinString);
        }

        Assert.That(wordSet.Contains("深蓝测试|shen'lan'ce'shi"), Is.True);
        Assert.That(wordSet.Contains("词库转换|ci'ku'zhuan'huan"), Is.True);
        Assert.That(wordSet.Contains("你好世界|ni'hao'shi'jie"), Is.True);
    }

    [Test]
    public void TestRoundTripWithRealFile()
    {
        // 使用真实 scel 文件进行往返测试
        var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "Test", "唐诗300首【官方推荐】.scel");
        if (!File.Exists(testFile))
        {
            Assert.Ignore("测试文件不存在，跳过");
            return;
        }

        var importer = new SougouPinyinScel();
        var original = importer.Import(testFile);

        var outputPath = Path.Combine(tempDir, "roundtrip_real.scel");
        exporter.ExportToBinary(original, outputPath);

        // 重新导入
        var reimported = importer.Import(outputPath);

        Assert.That(reimported.Count, Is.EqualTo(original.Count));

        // 验证所有词条和拼音都保持一致
        var originalSet = new System.Collections.Generic.HashSet<string>();
        foreach (var wl in original)
            originalSet.Add(wl.Word + "|" + wl.PinYinString);

        var reimportedSet = new System.Collections.Generic.HashSet<string>();
        foreach (var wl in reimported)
            reimportedSet.Add(wl.Word + "|" + wl.PinYinString);

        Assert.That(reimportedSet, Is.EqualTo(originalSet));
    }

    [Test]
    public void TestExportLineThrows()
    {
        Assert.Throws<Exception>(() => exporter.ExportLine(new WordLibrary()));
    }

    [Test]
    public void TestSkipWordsWithInvalidPinyin()
    {
        var wlList = new WordLibraryList
        {
            new WordLibrary { Word = "有拼音", PinYin = new[] { "you", "pin", "yin" }, Rank = 1 },
            new WordLibrary { Word = "非标拼音", PinYin = new[] { "xxx", "yyy" }, Rank = 3 }
        };

        var outputPath = Path.Combine(tempDir, "skip_invalid_pinyin.scel");
        exporter.ExportToBinary(wlList, outputPath);

        var data = File.ReadAllBytes(outputPath);
        var wordCount = BitConverter.ToInt32(data, 0x124);
        // "非标拼音"的拼音 xxx/yyy 不在标准拼音表中，应被跳过
        Assert.That(wordCount, Is.EqualTo(1));
    }
}
