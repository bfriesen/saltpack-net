using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Saltpack
{
    public static partial class Saltpack
    {
        private const string _alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public static string Armor(byte[] inputBytes, string messageType = "SALTPACK MESSAGE")
        {
            const int blockSize = 32;
            const int charsPerWord = 15;
            const int wordsPerSentence = 200;

            var chunks = GetChunks(inputBytes, blockSize);
            var output = new StringBuilder();
            foreach (var chunk in chunks)
                output.Append(EncodeBlock(chunk));
            for (int i = output.Length - 1; i > 0; i--)
            {
                if (i % (charsPerWord * wordsPerSentence) == 0) output.Insert(i, '\n');
                else if (i % charsPerWord == 0) output.Insert(i, ' ');
            }
            var header = $"BEGIN {messageType}. ";
            var footer = $". END {messageType}.";
            output.Insert(0, header).Append(footer);
            return output.ToString();
        }

        private static IEnumerable<byte[]> GetChunks(byte[] b, int size)
        {
            var chunk = new byte[size];
            for (int i = 0; i < b.Length; i += size)
            {
                if (i + size > b.Length) chunk = new byte[b.Length - i];
                for (int j = 0; j < chunk.Length; j++) chunk[j] = b[i + j];
                yield return chunk;
            }
        }

        private static string EncodeBlock(byte[] bytesBlock)
        {
            // Figure out how wide the chars block needs to be, and how many extra bits
            // we have.
            var charsSize = minCharsSize(_alphabet.Length, bytesBlock.Length);
            // Convert the bytes into an integer, big-endian.
            Array.Reverse(bytesBlock); // BigInteger is little-endian, so reverse the array.
            var temp = new byte[33];
            bytesBlock.CopyTo(temp, 0);
            var bytesInt = new BigInteger(temp);
            // Convert the result into our base.
            var places = new List<byte>();
            for (int place = 0; place < charsSize; place++)
            {
                var rem = (byte)(bytesInt % _alphabet.Length);
                places.Insert(0, rem);
                bytesInt /= _alphabet.Length;
            }
            return string.Join("", places.Select(p => _alphabet[p]));
        }

        public static byte[] Dearmor(string inputChars)
        {
            const int char_block_size = 43;
            var firstPeriod = inputChars.IndexOf('.');
            if (firstPeriod == -1) throw new InvalidOperationException("No period found in input.");
            var secondPeriod = inputChars.IndexOf('.', firstPeriod + 1);
            if (secondPeriod == -1) throw new InvalidOperationException("No second period found in input.");
            inputChars = inputChars.Substring(firstPeriod + 1, secondPeriod - firstPeriod - 1);
            var output = new List<byte>();
            var chunks = GetChunksIgnoringWhitespace(inputChars, char_block_size);
            foreach (var chunk in chunks)
                output.AddRange(DecodeBlock(chunk));
            return output.ToArray();
        }

        private static IEnumerable<string> GetChunksIgnoringWhitespace(string s, int size)
        {
            var chunk = new StringBuilder();
            foreach (var c in s)
            {
                if (char.IsWhiteSpace(c)) continue;
                chunk.Append(c);
                if (chunk.Length == size)
                {
                    yield return chunk.ToString();
                    chunk.Clear();
                }
            }
            if (chunk.Length > 0) yield return chunk.ToString();
        }

        private static byte[] DecodeBlock(string charsBlock)
        {
            // Figure out how many bytes we have, and how many extra bits they'll have
            // been shifted by.
            var bytesSize = max_bytes_size(_alphabet.Length, charsBlock.Length);
            var expectedCharSize = minCharsSize(_alphabet.Length, bytesSize);
            if (charsBlock.Length != expectedCharSize) throw new InvalidOperationException($"illegal chars size {charsBlock.Length}, expected {expectedCharSize}");
            // Convert the chars to an integer.
            var bytesInt = new BigInteger(0);
            foreach (var c in charsBlock)
            {
                bytesInt *= _alphabet.Length;
                bytesInt += _alphabet.IndexOf(c);
            }
            var bytes = bytesInt.ToByteArray();
            if (bytes.Length > bytesSize)
            {
                var temp = new byte[bytesSize];
                Array.Copy(bytes, 0, temp, 0, bytesSize);
                bytes = temp;
            }
            // BigInteger is little-endian, but saltblock is big-endian, so reverse the array.
            Array.Reverse(bytes);
            return bytes;
        }

        private static int max_bytes_size(int alphabet_size, int chars_size)
        {
            // The most bytes we can represent satisfies this:
            //     256 ^ bytes_size <= alphabet_size ^ chars_size
            // Take the log_2 of both sides:
            //     8 * bytes_size <= log_2(alphabet_size) * chars_size
            //  Solve for the maximum bytes_size:
            // 
            return (int)Math.Floor(Math.Log(alphabet_size, 2) / 8 * chars_size);
        }

        private static int minCharsSize(int alphabet_size, int bytes_size)
        {
            // The most bytes we can represent satisfies this:
            //     256 ^ bytes_size <= alphabet_size ^ chars_size
            // Take the log_2 of both sides:
            //     8 * bytes_size <= log_2(alphabet_size) * chars_size
            //  Solve for the minimum chars_size:
            // 
            return (int)Math.Ceiling(8 * bytes_size / Math.Log(alphabet_size, 2));
        }
    }
}
