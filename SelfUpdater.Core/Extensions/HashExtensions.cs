namespace SelfUpdater.Core.Extensions
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public enum HashType
    {
        MD5,
        SHA256
    }

    public static class HashExtensions
    {
        public static string GetChecksum(this string filePath, HashType hashType = HashType.MD5)
        {
            string result = string.Empty;
            using (Stream stream = new BufferedStream(File.OpenRead(filePath), 1048576))
            {
                switch (hashType)
                {
                    case HashType.SHA256:
                        SHA256Managed sha = new SHA256Managed();
                        byte[] checksum = sha.ComputeHash(stream);
                        result = BitConverter.ToString(checksum).Replace("-", String.Empty);
                        break;
                    case HashType.MD5:
                    default:
                        var md5Check = System.Security.Cryptography.MD5.Create();
                        md5Check.ComputeHash(stream);
                        // Get Hash Value
                        byte[] hashBytes = md5Check.Hash;
                        result = Convert.ToBase64String(hashBytes);
                        break;
                }
            }
            return result;
        }
    }
}
