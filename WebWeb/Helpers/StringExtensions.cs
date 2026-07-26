using System.Text.RegularExpressions;
using System.Text;

namespace WebWeb.Helpers;

public static class StringExtensions
{
    public static string ToSlug(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Chuyển về chữ thường
        string str = text.ToLowerInvariant().Trim();

        // 2. Chuyển ký tự có dấu thành không dấu (Đặc trị Tiếng Việt)
        string normalized = str.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        str = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // 3. Thay đ/Đ thành d
        str = Regex.Replace(str, @"[đĐ]", "d");

        // 4. Xóa ký tự đặc biệt, chỉ giữ lại chữ cái, số và khoảng trắng
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");

        // 5. Thay khoảng trắng trùng lặp thành 1 gạch ngang
        str = Regex.Replace(str, @"\s+", "-").Trim('-');

        return str;
    }
}