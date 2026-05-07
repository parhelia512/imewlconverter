/*
 *   Copyright © 2009-2020 studyzy(深蓝,曾毅)

 *   This program "IME WL Converter(深蓝词库转换)" is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.

 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.

 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Studyzy.IMEWLConverter.Entities;
using Studyzy.IMEWLConverter.Helpers;

namespace Studyzy.IMEWLConverter.IME;

/// <summary>
///     搜狗细胞词库
/// </summary>
[ComboBoxShow(ConstantString.SOUGOU_XIBAO_SCEL, ConstantString.SOUGOU_XIBAO_SCEL_C, 20)]
public class SougouPinyinScel : BaseImport, IWordLibraryImport, IWordLibraryExport, IBinaryWordLibraryExport
{
    private Dictionary<int, string> pyDic = new();

    /// <summary>
    ///     搜狗 scel 文件标准拼音表（413个音节）
    /// </summary>
    private static readonly string[] StandardPinyinTable =
    {
        "a", "ai", "an", "ang", "ao", "ba", "bai", "ban", "bang", "bao", "bei", "ben", "beng", "bi",
        "bian", "biao", "bie", "bin", "bing", "bo", "bu", "ca", "cai", "can", "cang", "cao", "ce", "cen",
        "ceng", "cha", "chai", "chan", "chang", "chao", "che", "chen", "cheng", "chi", "chong", "chou",
        "chu", "chua", "chuai", "chuan", "chuang", "chui", "chun", "chuo", "ci", "cong", "cou", "cu",
        "cuan", "cui", "cun", "cuo", "da", "dai", "dan", "dang", "dao", "de", "dei", "den", "deng", "di",
        "dia", "dian", "diao", "die", "ding", "diu", "dong", "dou", "du", "duan", "dui", "dun", "duo", "e",
        "ei", "en", "eng", "er", "fa", "fan", "fang", "fei", "fen", "feng", "fiao", "fo", "fou", "fu",
        "ga", "gai", "gan", "gang", "gao", "ge", "gei", "gen", "geng", "gong", "gou", "gu", "gua", "guai",
        "guan", "guang", "gui", "gun", "guo", "ha", "hai", "han", "hang", "hao", "he", "hei", "hen",
        "heng", "hong", "hou", "hu", "hua", "huai", "huan", "huang", "hui", "hun", "huo", "ji", "jia",
        "jian", "jiang", "jiao", "jie", "jin", "jing", "jiong", "jiu", "ju", "juan", "jue", "jun", "ka",
        "kai", "kan", "kang", "kao", "ke", "kei", "ken", "keng", "kong", "kou", "ku", "kua", "kuai",
        "kuan", "kuang", "kui", "kun", "kuo", "la", "lai", "lan", "lang", "lao", "le", "lei", "leng", "li",
        "lia", "lian", "liang", "liao", "lie", "lin", "ling", "liu", "lo", "long", "lou", "lu", "luan",
        "lue", "lun", "luo", "lv", "ma", "mai", "man", "mang", "mao", "me", "mei", "men", "meng", "mi",
        "mian", "miao", "mie", "min", "ming", "miu", "mo", "mou", "mu", "na", "nai", "nan", "nang", "nao",
        "ne", "nei", "nen", "neng", "ni", "nian", "niang", "niao", "nie", "nin", "ning", "niu", "nong",
        "nou", "nu", "nuan", "nue", "nun", "nuo", "nv", "o", "ou", "pa", "pai", "pan", "pang", "pao",
        "pei", "pen", "peng", "pi", "pian", "piao", "pie", "pin", "ping", "po", "pou", "pu", "qi", "qia",
        "qian", "qiang", "qiao", "qie", "qin", "qing", "qiong", "qiu", "qu", "quan", "que", "qun", "ran",
        "rang", "rao", "re", "ren", "reng", "ri", "rong", "rou", "ru", "rua", "ruan", "rui", "run", "ruo",
        "sa", "sai", "san", "sang", "sao", "se", "sen", "seng", "sha", "shai", "shan", "shang", "shao",
        "she", "shei", "shen", "sheng", "shi", "shou", "shu", "shua", "shuai", "shuan", "shuang", "shui",
        "shun", "shuo", "si", "song", "sou", "su", "suan", "sui", "sun", "suo", "ta", "tai", "tan", "tang",
        "tao", "te", "tei", "teng", "ti", "tian", "tiao", "tie", "ting", "tong", "tou", "tu", "tuan",
        "tui", "tun", "tuo", "wa", "wai", "wan", "wang", "wei", "wen", "weng", "wo", "wu", "xi", "xia",
        "xian", "xiang", "xiao", "xie", "xin", "xing", "xiong", "xiu", "xu", "xuan", "xue", "xun", "ya",
        "yan", "yang", "yao", "ye", "yi", "yin", "ying", "yo", "yong", "you", "yu", "yuan", "yue", "yun",
        "za", "zai", "zan", "zang", "zao", "ze", "zei", "zen", "zeng", "zha", "zhai", "zhan", "zhang",
        "zhao", "zhe", "zhei", "zhen", "zheng", "zhi", "zhong", "zhou", "zhu", "zhua", "zhuai", "zhuan",
        "zhuang", "zhui", "zhun", "zhuo", "zi", "zong", "zou", "zu", "zuan", "zui", "zun", "zuo"
    };

    #region IWordLibraryImport 成员

    //public bool OnlySinglePinyin { get; set; }

    public WordLibraryList Import(string path)
    {
        return ReadScel(path);
    }

    #endregion

    #region IWordLibraryImport Members

    public override bool IsText => false;

    #endregion

    public WordLibraryList ImportLine(string line)
    {
        throw new Exception("Scel格式是二进制文件，不支持流转换");
    }

    #region IWordLibraryExport 成员

    Encoding IWordLibraryExport.Encoding => Encoding.Unicode;

    CodeType IWordLibraryExport.CodeType => CodeType.Pinyin;

    public IList<string> Export(WordLibraryList wlList)
    {
        // scel 是二进制格式，不支持文本导出，请使用 IBinaryWordLibraryExport.ExportToBinary
        return new List<string> { "" };
    }

    public string ExportLine(WordLibrary wl)
    {
        throw new Exception("Scel格式是二进制文件，不支持逐行导出");
    }

    #endregion

    #region IBinaryWordLibraryExport 成员

    public void ExportToBinary(WordLibraryList wlList, string outputPath)
    {
        // 构建拼音索引映射
        var pinyinToIndex = BuildPinyinIndex();

        // 将词条按拼音序列分组（同音词归组）
        var groups = GroupByPinyin(wlList, pinyinToIndex);

        // 计算统计信息
        var groupCount = groups.Count;
        var totalWordCount = groups.Sum(g => g.Words.Count);

        // 计算校验值: 总汉字字符数 + 总拼音音节数(展开) + 词组数*2
        var totalChars = groups.Sum(g => g.Words.Sum(w => w.Length));
        var totalSyllables = groups.Sum(g => g.PinyinIndices.Length * g.Words.Count);
        var checksumValue = totalChars + totalSyllables + groupCount * 2;

        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        // 写入文件头
        WriteHeader(fs);

        // 写入统计信息
        WriteStatistics(fs, groupCount, totalWordCount, checksumValue);

        // 写入元信息
        WriteMetaInfo(fs, wlList);

        // 写入拼音表
        WritePinyinTable(fs);

        // 写入词条数据区
        WriteWordData(fs, groups);
    }

    #endregion

    #region 导出辅助方法

    private Dictionary<string, int> BuildPinyinIndex()
    {
        var dict = new Dictionary<string, int>();
        for (var i = 0; i < StandardPinyinTable.Length; i++)
        {
            dict[StandardPinyinTable[i]] = i;
        }
        return dict;
    }

    private List<PinyinWordGroup> GroupByPinyin(WordLibraryList wlList, Dictionary<string, int> pinyinToIndex)
    {
        var groupDict = new Dictionary<string, PinyinWordGroup>();

        foreach (var wl in wlList)
        {
            if (wl.PinYin == null || wl.PinYin.Length == 0)
            {
                SendExportErrorNotice($"词条「{wl.Word}」无拼音信息，已跳过");
                continue;
            }

            // 标准化拼音
            var normalizedPinyin = NormalizePinyin(wl.PinYin);

            // 验证所有拼音音节都在标准表中
            var allValid = true;
            foreach (var py in normalizedPinyin)
            {
                if (!pinyinToIndex.ContainsKey(py))
                {
                    SendExportErrorNotice($"词条「{wl.Word}」包含非标准拼音「{py}」，已跳过");
                    allValid = false;
                    break;
                }
            }
            if (!allValid) continue;

            // 用拼音序列作为分组 key
            var key = string.Join("'", normalizedPinyin);
            if (!groupDict.TryGetValue(key, out var group))
            {
                group = new PinyinWordGroup
                {
                    PinyinIndices = normalizedPinyin.Select(py => pinyinToIndex[py]).ToArray()
                };
                groupDict[key] = group;
            }
            group.Words.Add(wl.Word);
        }

        return groupDict.Values.OrderBy(g => string.Join(",", g.PinyinIndices.Select(i => i.ToString("D4")))).ToList();
    }

    private string[] NormalizePinyin(string[] pinyin)
    {
        var result = new string[pinyin.Length];
        for (var i = 0; i < pinyin.Length; i++)
        {
            // 去除声调数字，转小写
            var py = pinyin[i].ToLower().TrimEnd('0', '1', '2', '3', '4', '5');
            // 处理 ü → lv/nv 的情况（搜狗用 lv/nv 表示）
            result[i] = py;
        }
        return result;
    }

    private void WriteHeader(FileStream fs)
    {
        // 文件签名: 40 15 00 00 44 43 53 01
        fs.Write(new byte[] { 0x40, 0x15, 0x00, 0x00, 0x44, 0x43, 0x53, 0x01 });

        // 标志位
        fs.Write(new byte[] { 0x01, 0x00, 0x00, 0x00 });

        // 0x000C-0x0027: 文件标识区域（搜狗校验此区域非零）
        fs.Write(new byte[]
        {
            0x2A, 0x5F, 0x1A, 0x3B, 0x39, 0xBB, 0xF1, 0x99,
            0xF7, 0x87, 0x93, 0x12, 0xA1, 0xED, 0xD8, 0x5D,
            0x31, 0x00, 0x37, 0x00, 0x36, 0x00, 0x32, 0x00,
            0x37, 0x00, 0x31, 0x00, 0x00, 0x00
        });

        // 填充至 0x011C
        var padding1 = 0x11C - (int)fs.Position;
        fs.Write(new byte[padding1]);

        // 0x011C: 校验标识
        fs.Write(new byte[] { 0xEA, 0x72, 0x68, 0x69 });
    }

    private void WriteStatistics(FileStream fs, int groupCount, int totalWordCount, int checksumValue)
    {
        // 0x0120: 词组数
        fs.Write(BitConverter.GetBytes(groupCount));
        // 0x0124: 词条总数
        fs.Write(BitConverter.GetBytes(totalWordCount));
        // 0x0128: 校验值 = 总汉字字符数 + 总拼音音节数 + 词组数*2
        fs.Write(BitConverter.GetBytes(checksumValue));
        // 0x012C: 同 0x0128
        fs.Write(BitConverter.GetBytes(checksumValue));
    }

    private void WriteMetaInfo(FileStream fs, WordLibraryList wlList)
    {
        // 0x0130: 名称（520字节）
        WriteScelField(fs, "深蓝词库转换", 520);

        // 0x0338: 类型（520字节）
        WriteScelField(fs, "自定义", 520);

        // 0x0540: 描述（2048字节）
        WriteScelField(fs, "由深蓝词库转换工具生成", 2048);

        // 0x0D40: 示例词（2048字节）
        var sample = string.Join(" ", wlList.Take(5).Select(w => w.Word));
        WriteScelField(fs, sample, 2048);
    }

    private void WriteScelField(FileStream fs, string text, int fieldSize)
    {
        var bytes = Encoding.Unicode.GetBytes(text + "\0");
        if (bytes.Length > fieldSize)
        {
            bytes = bytes[..fieldSize];
            // 确保以 \0\0 结尾
            bytes[fieldSize - 2] = 0;
            bytes[fieldSize - 1] = 0;
        }
        fs.Write(bytes);
        // 填充剩余空间
        var padding = fieldSize - bytes.Length;
        if (padding > 0)
        {
            fs.Write(new byte[padding]);
        }
    }

    private void WritePinyinTable(FileStream fs)
    {
        // 写入拼音条目数
        fs.Write(BitConverter.GetBytes(StandardPinyinTable.Length));

        // 逐条写入拼音
        for (var i = 0; i < StandardPinyinTable.Length; i++)
        {
            var pyBytes = Encoding.Unicode.GetBytes(StandardPinyinTable[i]);
            // 索引号 (Int16 LE)
            fs.Write(BitConverter.GetBytes((short)i));
            // 字节长度 (Int16 LE)
            fs.Write(BitConverter.GetBytes((short)pyBytes.Length));
            // 拼音字符串 (UTF-16LE)
            fs.Write(pyBytes);
        }
    }

    private void WriteWordData(FileStream fs, List<PinyinWordGroup> groups)
    {
        var wordIndex = 1;
        foreach (var group in groups)
        {
            var samePyCount = (short)group.Words.Count;
            var pinyinByteLen = (short)(group.PinyinIndices.Length * 2);

            // 词组头部: same_py_count (2) + pinyin_byte_len (2)
            fs.Write(BitConverter.GetBytes(samePyCount));
            fs.Write(BitConverter.GetBytes(pinyinByteLen));

            // 拼音索引数组
            foreach (var idx in group.PinyinIndices)
            {
                fs.Write(BitConverter.GetBytes((short)idx));
            }

            // 词条数据
            foreach (var word in group.Words)
            {
                var wordBytes = Encoding.Unicode.GetBytes(word);
                // 汉字字节数 (UInt16 LE)
                fs.Write(BitConverter.GetBytes((short)wordBytes.Length));
                // 汉字内容 (UTF-16LE)
                fs.Write(wordBytes);
                // unknown1: 固定值 10
                fs.Write(BitConverter.GetBytes((short)10));
                // unknown2: 词条序号
                fs.Write(BitConverter.GetBytes(wordIndex));
                // extra: 6字节全零
                fs.Write(new byte[6]);
                wordIndex++;
            }
        }
    }

    private class PinyinWordGroup
    {
        public int[] PinyinIndices { get; set; }
        public List<string> Words { get; } = new();
    }

    #endregion

    #region 导入辅助方法

    public static Dictionary<string, string> ReadScelInfo(string path)
    {
        var info = new Dictionary<string, string>();
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

        fs.Position = 0x124;
        var CountWord = BinFileHelper.ReadInt32(fs);
        info.Add("CountWord", CountWord.ToString());

        info.Add("Name", readScelFieldText(fs, 0x130));
        info.Add("Type", readScelFieldText(fs, 0x338));
        info.Add("Info", readScelFieldText(fs, 0x540, 1024));
        info.Add("Sample", readScelFieldText(fs, 0xd40, 1024));

        fs.Close();
        return info;
    }

    private static string readScelFieldText(FileStream fs, long seek, int length = 64)
    {
        var oldSeek = fs.Position;
        if (seek > fs.Length)
            throw new ArgumentException("地址超过文件长度");
        fs.Seek(seek, SeekOrigin.Begin);
        var bytes = new byte[length];
        fs.ReadExactly(bytes, 0, length);
        var value = Encoding.Unicode.GetString(bytes);
        var end = value.IndexOf('\0');
        if (end < 0)
            throw new ArgumentException("未找到\\0，可能索求长度不足");
        var text = value.Substring(0, end);
        fs.Position = oldSeek;
        return text;
    }

    private WordLibraryList ReadScel(string path)
    {
        pyDic = new Dictionary<int, string>();
        var pyAndWord = new WordLibraryList();
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var str = new byte[128];
        var outstr = new byte[128];

        // 未展开的词条数（同音词算1个
        fs.Position = 0x120;
        var dictLen = BinFileHelper.ReadInt32(fs);

        // 拼音表的长度
        fs.Position = 0x1540;
        var pyDicLen = BinFileHelper.ReadInt32(fs);

        str = new byte[4];
        for (var i = 0; i < pyDicLen; i++)
        {
            var idx = BinFileHelper.ReadInt16(fs);
            var size = BinFileHelper.ReadInt16(fs);
            str = new byte[size];
            fs.ReadExactly(str, 0, size);
            var py = Encoding.Unicode.GetString(str);
            pyDic.Add(idx, py);
        }

        var s = new StringBuilder();
        foreach (var value in pyDic.Values) s.Append(value + "\",\"");
        Debug.WriteLine(s.ToString());

        for (var i = 0; i < dictLen; i++)
            try
            {
                pyAndWord.AddRange(ReadAPinyinWord(fs));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        return pyAndWord;
    }

    private IList<WordLibrary> ReadAPinyinWord(FileStream fs)
    {
        var num = new byte[4];
        fs.ReadExactly(num, 0, 4);
        var samePYcount = num[0] + num[1] * 256;
        var count = num[2] + num[3] * 256;
        //接下来读拼音
        var str = new byte[256];
        for (var i = 0; i < count; i++) str[i] = (byte)fs.ReadByte();
        var wordPY = new List<string>();
        for (var i = 0; i < count / 2; i++)
        {
            var key = str[i * 2] + str[i * 2 + 1] * 256;
            if (key < pyDic.Count)
                wordPY.Add(pyDic[key]);
            else
                wordPY.Add(((char)(key - pyDic.Count + 97)).ToString());
        }

        //接下来读词语
        var pyAndWord = new List<WordLibrary>();
        for (var s = 0; s < samePYcount; s++) //同音词，使用前面相同的拼音
        {
            num = new byte[2];
            fs.ReadExactly(num, 0, 2);
            var hzBytecount = num[0] + num[1] * 256;
            str = new byte[hzBytecount];
            fs.ReadExactly(str, 0, hzBytecount);
            var word = Encoding.Unicode.GetString(str);
            var unknown1 = BinFileHelper.ReadInt16(fs); //全部是10,肯定不是词频，具体是什么不知道
            var unknown2 = BinFileHelper.ReadInt32(fs); //每个字对应的数字不一样，不知道是不是词频
            pyAndWord.Add(
                new WordLibrary
                {
                    Word = word,
                    PinYin = wordPY.ToArray(),
                    Rank = DefaultRank
                }
            );
            CurrentStatus++;
            //接下来10个字节什么意思呢？暂时先忽略了
            var temp = new byte[6];
            for (var i = 0; i < 6; i++) temp[i] = (byte)fs.ReadByte();
        }

        return pyAndWord;
    }

    #endregion
}
