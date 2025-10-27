using System.Globalization;
using System.Text;

namespace WebBanHang.Helpers
{
    public static class TextHelper
    {
        public static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // ✅ Chuẩn hóa Unicode FormD để tách dấu
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                // Bỏ các ký tự dấu (NonSpacingMark)
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // ✅ Trả về chữ không dấu, viết thường
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLower();
        }
    }
}
