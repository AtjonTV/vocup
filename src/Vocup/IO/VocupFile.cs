using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Vocup.IO.Internal
{
    /// <summary>
    ///     Represents an encrypted, Vocup specific file.
    /// </summary>
    internal abstract class VocupFile
    {
        private static readonly DES csp = new DESCryptoServiceProvider
        {
            Key = new byte[8] {1, 8, 3, 4, 3, 2, 7, 1},
            IV = new byte[8] {3, 1, 3, 4, 6, 3, 4, 8}
        };

        /// <summary>
        ///     Decrypts Vocup specific file using DES and a hard-coded key.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The UTF8 encoded plaintext.</returns>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="NotSupportedException" />
        /// <exception cref="FormatException" />
        /// <exception cref="CryptographicException" />
        protected string ReadFile(string path)
        {
            byte[] ciphertext;
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (file.ReadByte() == 0x50 && file.ReadByte() == 0x4B && file.ReadByte() == 0x03 &&
                    file.ReadByte() == 0x04)
                    throw new NotSupportedException(
                        $"The file {path} cannot be opened by Vocup. A later version might be required.");

                file.Position = 0;

                using (var reader = new StreamReader(file, Encoding.UTF8))
                {
                    ciphertext = Convert.FromBase64String(reader.ReadToEnd());
                }
            }

            using (var plainstream = new MemoryStream())
            using (var transform = csp.CreateDecryptor())
            using (var cipherstream = new CryptoStream(plainstream, transform, CryptoStreamMode.Write))
            {
                cipherstream.Write(ciphertext, 0, ciphertext.Length);
                cipherstream.FlushFinalBlock();
                return Encoding.UTF8.GetString(plainstream.ToArray());
            }
        }

        /// <summary>
        ///     Encrypts a Vocup specific file using UTF-8 and DES with a hard-coded key.
        /// </summary>
        /// <param name="path">The path where to save the encrypted file.</param>
        /// <param name="content">The context text that will be encoded with UTF-8 before encryption.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="IOException" />
        protected void WriteFile(string path, string content)
        {
            var buffer = Encoding.UTF8.GetBytes(content);

            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            using (var cipherstream = new MemoryStream())
            using (var transform = csp.CreateEncryptor())
            using (var plainstream = new CryptoStream(cipherstream, transform, CryptoStreamMode.Write))
            {
                plainstream.Write(buffer, 0, buffer.Length);
                plainstream.FlushFinalBlock();
                writer.Write(Convert.ToBase64String(cipherstream.ToArray()));
            }
        }
    }
}