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
    public class LoginCryptography : CryptographyBase
    {
        #region Instantiation

        public LoginCryptography() : base(false)
        {
        }

        #endregion

        #region Methods

        public static string GetPassword(string password)
        {
            bool equal = password.Length % 2 == 0;
            string str = equal ? password.Remove(0, 3) : password.Remove(0, 4);
            StringBuilder decryptpass = new StringBuilder();

            for (int i = 0; i < str.Length; i += 2)
            {
                decryptpass.Append(str[i]);
            }
            if (decryptpass.Length % 2 != 0)
            {
                str = password.Remove(0, 2);
                decryptpass = decryptpass.Clear();
                for (int i = 0; i < str.Length; i += 2)
                {
                    decryptpass.Append(str[i]);
                }
            }

            StringBuilder passwd = new StringBuilder();
            for (int i = 0; i < decryptpass.Length; i += 2)
            {
                passwd.Append(Convert.ToChar(Convert.ToUInt32(decryptpass.ToString().Substring(i, 2), 16)));
            }
            return passwd.ToString();
        }

        public override string Decrypt(byte[] data, int sessionId = 0)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                foreach (byte character in data)
                {
                    if (character > 14)
                    {
                        builder.Append(Convert.ToChar((character - 15) ^ 195));
                    }
                    else
                    {
                        builder.Append(Convert.ToChar((256 - (15 - character)) ^ 195));
                    }
                }
                return builder.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public override string DecryptCustomParameter(byte[] data) => throw new NotImplementedException();

        public override byte[] Encrypt(string data)
        {
            try
            {
                data += " ";
                byte[] dataBytes = Encoding.Default.GetBytes(data);
                for (int i = 0; i < dataBytes.Length; i++)
                {
                    dataBytes[i] = Convert.ToByte(dataBytes[i] + 15);
                }
                dataBytes[dataBytes.Length - 1] = 25;
                return dataBytes;
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        #endregion
    }
}