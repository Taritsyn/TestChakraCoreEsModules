using System.Buffers;
using System.Text;

namespace TestChakraCoreEsModules.Helpers
{
	/// <summary>
	/// Encoding helpers
	/// </summary>
	internal static class EncodingHelpers
	{
		public static string UnicodeToAnsi(string value, out int byteCount)
		{
			if (string.IsNullOrEmpty(value))
			{
				byteCount = 0;
				return value;
			}

			string result;
			int valueLength = value.Length;
			Encoding utf8Encoding = Encoding.UTF8;
			Encoding ansiEncoding = Encoding.Default;

			var byteArrayPool = ArrayPool<byte>.Shared;
			int bufferLength = utf8Encoding.GetByteCount(value);
			byte[] buffer = byteArrayPool.Rent(bufferLength + 1);
			buffer[bufferLength] = 0;

			try
			{
				result = ConvertStringInternal(utf8Encoding, ansiEncoding, value, valueLength, buffer, bufferLength);
			}
			finally
			{
				byteArrayPool.Return(buffer);
			}

			byteCount = bufferLength;

			return result;
		}

		private static unsafe string ConvertStringInternal(Encoding srcEncoding, Encoding dstEncoding, string s,
			int charCount, byte[] bytes, int byteCount)
		{
			fixed (char* pString = s)
			fixed (byte* pBytes = bytes)
			{
				srcEncoding.GetBytes(pString, charCount, pBytes, byteCount);
				string result = dstEncoding.GetString(pBytes, byteCount);

				return result;
			}
		}
	}
}