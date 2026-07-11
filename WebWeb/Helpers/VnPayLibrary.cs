using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WebWeb.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            if (_responseData.TryGetValue(key, out string value))
            {
                return value;
            }
            return string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            StringBuilder data = new StringBuilder();
            StringBuilder query = new StringBuilder();

            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // VNPAY v2.1.0 Quy định: Chuỗi băm ký (data) PHẢI dùng WebUtility.UrlEncode nhưng KHÔNG mã hóa các kí tự tên tham số
                    data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                    
                    // Chuỗi Query URL bám vào trình duyệt
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string searchUrl = query.ToString();
            string rawData = data.ToString();
            
            if (searchUrl.EndsWith("&")) searchUrl = searchUrl.Remove(searchUrl.Length - 1);
            if (rawData.EndsWith("&")) rawData = rawData.Remove(rawData.Length - 1);

            // Xử lý đồng bộ dấu cộng (+) thành %20 cho cả chuỗi băm để khớp chữ ký Server VNPAY
            rawData = rawData.Replace("+", "%20");
            searchUrl = searchUrl.Replace("+", "%20");

            string vnpSecureHash = HmacSha256(vnpHashSecret, rawData);
            return baseUrl + "?" + searchUrl + "&vnp_SecureHash=" + vnpSecureHash;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Key) && !kv.Key.StartsWith("vnp_SecureHash"))
                {
                    data.Append(kv.Key + "=" + kv.Value + "&");
                }
            }
            
            string rawData = data.ToString();
            if (rawData.EndsWith("&")) rawData = rawData.Remove(rawData.Length - 1);

            string checkSum = HmacSha256(secretKey, rawData);
            return checkSum.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSha256(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString().ToUpper();
        }
    }

    // Bộ so sánh bắt buộc của VNPAY SDK dùng để sắp xếp Alphabet chuẩn nhị phân
    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}