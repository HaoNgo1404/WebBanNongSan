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
            var data = new StringBuilder();
            var query = new StringBuilder();

            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            if (data.Length > 0)
                data.Length--;

            if (query.Length > 0)
                query.Length--;

            string secureHash = HmacSha512(vnpHashSecret, data.ToString());

            return $"{baseUrl}?{query}&vnp_SecureHashType=HMACSHA512&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Key) && !kv.Key.StartsWith("vnp_SecureHash"))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key));
                    data.Append("=");
                    data.Append(WebUtility.UrlEncode(kv.Value));
                    data.Append("&");
                }
            }
            
            string rawData = data.ToString();
            if (rawData.EndsWith("&")) rawData = rawData.Remove(rawData.Length - 1);

            string checkSum = HmacSha512(secretKey, rawData);
            return checkSum.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSha512(string key, string inputData)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                return BitConverter.ToString(hmac.ComputeHash(inputBytes))
                    .Replace("-", "")
                    .ToLower();
            }
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