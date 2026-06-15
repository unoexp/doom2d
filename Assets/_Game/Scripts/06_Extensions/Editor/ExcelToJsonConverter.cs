// 📁 Assets/_Game/Scripts/06_Extensions/Editor/ExcelToJsonConverter.cs
// ─────────────────────────────────────────────────────────────────────
// Excel → JSON 转换工具（Editor）
// 使用 .NET 内置的 ZipFile + Xml.Linq 解析 .xlsx（OpenXML），
// 无需 EPPlus 等第三方库。
// 输出到 StreamingAssets/Data/，运行时通过 ResourceManager 加载。
// ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Assets/GameData/Excel/ 下的 .xlsx 文件批量转换为 JSON，
/// 输出到 Assets/StreamingAssets/Data/。
/// </summary>
public static class ExcelToJsonConverter
{
    // ═══════════════════════════════════════════════════════════════════
    // 路径常量
    // ═══════════════════════════════════════════════════════════════════
    private const string EXCEL_DIR = "Assets/GameData/Excel";
    private const string OUTPUT_DIR = "Assets/StreamingAssets/Data";

    // ✂️────────────────────────────────────────────────────────────────
    // 菜单项
    // ✂️────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Convert Excel to JSON")]
    public static void ConvertAll()
    {
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder(EXCEL_DIR))
        {
            AssetDatabase.CreateFolder("Assets/GameData", "Excel");
            Debug.LogWarning($"[ExcelToJson] 已创建目录 {EXCEL_DIR}，请放入 .xlsx 文件后重试。");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
        {
            // StreamingAssets 应默认存在，但 Data 子目录可能不存在
            if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            {
                AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            }
            AssetDatabase.CreateFolder("Assets/StreamingAssets", "Data");
        }

        // 获取所有 .xlsx 文件
        string fullExcelDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", EXCEL_DIR));
        string fullOutputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../", OUTPUT_DIR));

        string[] xlsxFiles = Directory.GetFiles(fullExcelDir, "*.xlsx", SearchOption.TopDirectoryOnly);

        if (xlsxFiles.Length == 0)
        {
            Debug.LogWarning($"[ExcelToJson] {EXCEL_DIR} 下没有找到 .xlsx 文件。");
            return;
        }

        int totalFiles = 0;
        int totalRows = 0;
        var summaryBuilder = new StringBuilder();

        foreach (string xlsxPath in xlsxFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(xlsxPath);
            try
            {
                List<JObject> rows = ParseXlsx(xlsxPath);
                if (rows.Count == 0)
                {
                    Debug.LogWarning($"[ExcelToJson] {fileName}.xlsx 没有数据行（第3行起）。");
                }

                // 写入 JSON
                string jsonPath = Path.Combine(fullOutputDir, fileName + ".json");
                WriteJsonArray(jsonPath, rows);

                totalFiles++;
                totalRows += rows.Count;
                summaryBuilder.AppendLine($"  {fileName}.json — {rows.Count} 条数据");

                Debug.Log($"[ExcelToJson] ✓ {fileName}.xlsx → {fileName}.json ({rows.Count} 行)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExcelToJson] ✗ 转换 {fileName}.xlsx 时出错: {ex.Message}");
            }
        }

        // 刷新 AssetDatabase
        AssetDatabase.Refresh();

        // 输出汇总
        Debug.Log(
            $"[ExcelToJson] ═══════════════════════════\n" +
            $"[ExcelToJson] 转换完成！共 {totalFiles} 个文件，{totalRows} 条数据：\n" +
            $"{summaryBuilder}" +
            $"[ExcelToJson] ═══════════════════════════"
        );
    }

    // ✂️────────────────────────────────────────────────────────────────
    // 核心解析逻辑
    // ✂️────────────────────────────────────────────────────────────────

    /// <summary>
    /// 解析一个 .xlsx 文件，返回 JSON 对象列表。
    /// </summary>
    private static List<JObject> ParseXlsx(string filePath)
    {
        var rows = new List<JObject>();

        using (FileStream fs = File.OpenRead(filePath))
        using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            // ── 1. 解析共享字符串表 ──
            ZipArchiveEntry sstEntry = archive.GetEntry("xl/sharedStrings.xml");
            List<string> sharedStrings = sstEntry != null
                ? ParseSharedStrings(sstEntry)
                : new List<string>();

            // ── 2. 解析工作表 ──
            ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
            if (sheetEntry == null)
            {
                throw new InvalidDataException("找不到 xl/worksheets/sheet1.xml，请确认 Excel 文件格式正确。");
            }

            XDocument sheetDoc;
            using (Stream sheetStream = sheetEntry.Open())
            {
                sheetDoc = XDocument.Load(sheetStream);
            }

            XElement sheetData = sheetDoc.Root?
                .Element(XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"));

            if (sheetData == null)
            {
                throw new InvalidDataException("工作表中没有 sheetData 节点。");
            }

            var rawRows = sheetData.Elements(XName.Get("row", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))
                .ToList();

            if (rawRows.Count < 3)
            {
                // 至少需要表头行 + 注释行 + 1 行数据
                return rows;
            }

            // ── 3. 解析表头（第 1 行） ──
            Dictionary<int, string> columnIndexToFieldName = ParseHeaderRow(rawRows[0], sharedStrings);

            // ── 4. 跳过第 2 行（中文注释），从第 3 行起解析数据 ──
            for (int i = 2; i < rawRows.Count; i++)
            {
                JObject obj = ParseDataRow(rawRows[i], sharedStrings, columnIndexToFieldName);
                if (obj != null && obj.Count > 0)
                {
                    rows.Add(obj);
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// 解析共享字符串表。
    /// </summary>
    private static List<string> ParseSharedStrings(ZipArchiveEntry sstEntry)
    {
        var strings = new List<string>();

        using (Stream stream = sstEntry.Open())
        {
            XDocument doc = XDocument.Load(stream);
            XElement sst = doc.Root;
            if (sst == null) return strings;

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (XElement si in sst.Elements(ns + "si"))
            {
                // 简单文本：<si><t>text</t></si>
                XElement t = si.Element(ns + "t");
                if (t != null)
                {
                    // 注意：共享字符串可能包含换行符等空白
                    strings.Add(t.Value);
                }
                else
                {
                    // 富文本：<si><r><t>part1</t></r><r><t>part2</t></r></si>
                    var richTextParts = si.Elements(ns + "r")
                        .SelectMany(r => r.Elements(ns + "t"))
                        .Select(rt => rt.Value);
                    strings.Add(string.Concat(richTextParts));
                }
            }
        }

        return strings;
    }

    /// <summary>
    /// 解析表头行（第 1 行），返回 列索引 → 字段名 的映射。
    /// </summary>
    private static Dictionary<int, string> ParseHeaderRow(XElement headerRow, List<string> sharedStrings)
    {
        var map = new Dictionary<int, string>();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        foreach (XElement c in headerRow.Elements(ns + "c"))
        {
            string cellRef = (string)c.Attribute("r");
            if (string.IsNullOrEmpty(cellRef)) continue;

            int colIndex = ColumnRefToIndex(cellRef);
            string value = GetCellValue(c, sharedStrings, ns);

            if (!string.IsNullOrWhiteSpace(value))
            {
                // 转换为 camelCase：首字母小写，移除空格
                string fieldName = ToCamelCase(value.Trim());
                map[colIndex] = fieldName;
            }
        }

        return map;
    }

    /// <summary>
    /// 解析一行数据，返回 JSON 对象。
    /// 只包含在表头中定义了字段名且值非空的单元格。
    /// </summary>
    private static JObject ParseDataRow(
        XElement rowElement,
        List<string> sharedStrings,
        Dictionary<int, string> columnIndexToFieldName)
    {
        var obj = new JObject();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        foreach (XElement c in rowElement.Elements(ns + "c"))
        {
            string cellRef = (string)c.Attribute("r");
            if (string.IsNullOrEmpty(cellRef)) continue;

            int colIndex = ColumnRefToIndex(cellRef);

            // 该列没有对应的表头字段，跳过
            if (!columnIndexToFieldName.TryGetValue(colIndex, out string fieldName))
                continue;

            string rawValue = GetCellValue(c, sharedStrings, ns);

            // 空值跳过
            if (string.IsNullOrWhiteSpace(rawValue))
                continue;

            // 解析并赋值
            JToken parsed = ParseCellValue(rawValue.Trim());
            obj[fieldName] = parsed;
        }

        return obj;
    }

    /// <summary>
    /// 获取单元格的原始字符串值。
    /// </summary>
    private static string GetCellValue(XElement c, List<string> sharedStrings, XNamespace ns)
    {
        string cellType = (string)c.Attribute("t");

        XElement v = c.Element(ns + "v");
        string rawValue = v?.Value;

        if (cellType == "s")
        {
            // 共享字符串引用
            if (int.TryParse(rawValue, out int sstIndex) && sstIndex >= 0 && sstIndex < sharedStrings.Count)
                return sharedStrings[sstIndex];
            return string.Empty;
        }

        if (cellType == "b")
        {
            // 布尔值
            return rawValue == "1" ? "TRUE" : "FALSE";
        }

        if (cellType == "inlineStr")
        {
            // 内联字符串（OpenXML 标准格式）：<is><t>text</t></is>
            XElement isElement = c.Element(ns + "is");
            if (isElement != null)
            {
                XElement t = isElement.Element(ns + "t");
                return t?.Value ?? string.Empty;
            }
            return string.Empty;
        }

        if (cellType == "str")
        {
            // 公式结果字符串：值直接在 <v> 中（如用户示例的简化格式）
            // 也兼容某些 Excel 版本将内联字符串放在 <is> 中的情况
            XElement isElement = c.Element(ns + "is");
            if (isElement != null)
            {
                XElement t = isElement.Element(ns + "t");
                return t?.Value ?? string.Empty;
            }
            return rawValue ?? string.Empty;
        }

        // 数字 / 空类型 → 原样返回（值在 <v> 中）
        return rawValue ?? string.Empty;
    }

    /// <summary>
    /// 根据单元格值的内容，智能解析为 JSON 值：
    /// - 纯数字 → JSON number
    /// - TRUE/FALSE → JSON bool
    /// - pipe 分隔的 key:value 对 → JSON 数组 of 对象
    /// - pipe 分隔的普通值 → JSON 数组 of 字符串
    /// - 其他 → JSON string
    /// </summary>
    private static JToken ParseCellValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return JValue.CreateNull();

        // ── 布尔值 ──
        if (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase))
            return new JValue(true);
        if (string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase))
            return new JValue(false);

        // ── 纯数字（整数或浮点数） ──
        if (IsNumeric(value, out double numValue))
        {
            // 整数用 long，浮点用 double
            if (numValue == Math.Floor(numValue) && !value.Contains(".") && !value.Contains("e") && !value.Contains("E"))
                return new JValue((long)numValue);
            return new JValue(numValue);
        }

        // ── Pipe 分隔值 ──
        if (value.Contains("|"))
        {
            string[] parts = value.Split('|');

            // 判断是否为 key:value 对（每个分段都包含冒号）
            bool allKeyValue = parts.All(p => p.Contains(":"));
            if (allKeyValue)
            {
                var arr = new JArray();
                foreach (string part in parts)
                {
                    int colonIndex = part.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < part.Length - 1)
                    {
                        string key = part.Substring(0, colonIndex).Trim();
                        string val = part.Substring(colonIndex + 1).Trim();
                        var kvObj = new JObject { [key] = ParseCellValue(val) };
                        arr.Add(kvObj);
                    }
                }
                return arr;
            }
            else
            {
                // 普通字符串数组
                var arr = new JArray();
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        arr.Add(ParseCellValue(trimmed));
                    }
                }
                return arr;
            }
        }

        // ── 默认：字符串 ──
        return new JValue(value);
    }

    /// <summary>
    /// 判断字符串是否为有效数字（支持整数、浮点数、科学计数法）。
    /// </summary>
    private static bool IsNumeric(string value, out double result)
    {
        return double.TryParse(
            value,
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out result);
    }

    /// <summary>
    /// 将 Excel 列引用（如 A, B, AA, AB）转换为零基索引。
    /// A=0, B=1, ..., Z=25, AA=26, AB=27, ...
    /// </summary>
    private static int ColumnRefToIndex(string cellRef)
    {
        // 从单元格引用中提取列字母部分（如 "A3" → "A", "AB12" → "AB"）
        string colLetters = new string(cellRef.TakeWhile(c => char.IsLetter(c)).ToArray());

        int index = 0;
        for (int i = 0; i < colLetters.Length; i++)
        {
            index = index * 26 + (char.ToUpper(colLetters[i]) - 'A' + 1);
        }
        return index - 1; // 转换为零基索引
    }

    /// <summary>
    /// 将中文字段名（或任意字符串）转换为 camelCase。
    /// 简单策略：如果是中文则生成 field00 形式的占位名；
    /// 如果是英文则直接转换为 camelCase。
    /// </summary>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // 如果包含中文字符，尝试提取英文部分，否则使用拼音格式占位
        bool hasChinese = name.Any(c => c >= 0x4E00 && c <= 0x9FFF);
        if (hasChinese)
        {
            // 检查是否有英文/数字部分（如 "物品ID" → "id"）
            string englishPart = new string(name.Where(c => char.IsLetterOrDigit(c) && c < 0x4E00).ToArray());
            if (!string.IsNullOrEmpty(englishPart))
            {
                return char.ToLower(englishPart[0]) + englishPart.Substring(1);
            }
            // 纯中文：直接用中文字段名（JSON 允许中文 key）
            return name;
        }

        // 英文名称 → camelCase
        // 先清理：移除非字母数字字符，按单词拆分
        string[] words = name.Split(new[] { ' ', '_', '-', '(', ')', '[', ']', '.', '/' },
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return name.ToLower();

        var sb = new StringBuilder();
        sb.Append(words[0].ToLower());
        for (int i = 1; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                sb.Append(char.ToUpper(words[i][0]));
                if (words[i].Length > 1)
                    sb.Append(words[i].Substring(1).ToLower());
            }
        }

        return sb.ToString();
    }

    // ✂️────────────────────────────────────────────────────────────────
    // JSON 写入
    // ✂️────────────────────────────────────────────────────────────────

    /// <summary>
    /// 将 JSON 对象数组写入文件（格式化的 JSON 数组）。
    /// </summary>
    private static void WriteJsonArray(string filePath, List<JObject> rows)
    {
        var array = new JArray(rows);

        // 格式化写入
        using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        using (var jsonWriter = new JsonTextWriter(writer))
        {
            jsonWriter.Formatting = Formatting.Indented;
            jsonWriter.Indentation = 2;
            jsonWriter.IndentChar = ' ';
            array.WriteTo(jsonWriter);
        }
    }
}
#endif
