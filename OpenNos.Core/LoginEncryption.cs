/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;
using System.Text;

namespace OpenNos.Core
{
    public class LoginEncryption : EncryptionBase
    {
        #region Instantiation

        public LoginEncryption() : base(false)
        {
            // do nothing
        }

        #endregion

        #region Methods

        public static string GetPassword(string password)
        {
            bool equal = password.Length % 2 == 0;
            string str = equal ? password.Remove(0, 3) : password.Remove(0, 4);
            string decpass = string.Empty;
            for (int i = 0; i < str.Length; i += 2)
            {
                decpass += str[i];
            }
            if (decpass.Length % 2 != 0)
            {
                str = password.Remove(0, 2);
                decpass = string.Empty;
                for (int i = 0; i < str.Length; i += 2)
                {
                    decpass += str[i];
                }
            }
            StringBuilder temp = new StringBuilder();
            for (int i = 0; i < decpass.Length; i += 2)
            {
                temp.Append(Convert.ToChar(Convert.ToUInt32(decpass.Substring(i, 2), 16)));
            }
            decpass = temp.ToString();
            return decpass;
        }

        public override string Decrypt(byte[] packet, int sessionId = 0)
        {
            try
            {
                string decryptedPacket = string.Empty;

                foreach (byte character in packet)
                {
                    if (character > 14)
                    {
                        decryptedPacket += Convert.ToChar((character - 15) ^ 195);
                    }
                    else
                    {
                        decryptedPacket += Convert.ToChar((256 - (15 - character)) ^ 195);
                    }
                }

                return decryptedPacket;
            }
            catch
            {
                return string.Empty;
            }
        }

        public override string DecryptCustomParameter(byte[] data)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encrypt(string packet)
        {
            try
            {
                packet += " ";
                byte[] tmp = Encoding.Default.GetBytes(packet);
                for (int i = 0; i < packet.Length; i++)
                {
                    tmp[i] = Convert.ToByte(tmp[i] + 15);
                }
                tmp[tmp.Length - 1] = 25;
                return tmp;
            }
            catch
            {
                return new byte[0];
            }
        }

        #endregion
    }
}
