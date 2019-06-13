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
using System.Collections.Generic;
using System.Text;

namespace OpenNos.Core
{
    public class WorldCryptography : CryptographyBase
    {
        #region Instantiation

        public WorldCryptography() : base(true)
        {
        }

        #endregion

        #region Methods

        public static string Decrypt2(string str)
        {
            List<byte> receiveData = new List<byte>();
            char[] table = { ' ', '-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'n' };
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] <= 0x7A)
                {
                    int len = str[i];

                    for (int j = 0; j < len; j++)
                    {
                        i++;

                        try
                        {
                            receiveData.Add(unchecked((byte)(str[i] ^ 0xFF)));
                        }
                        catch (Exception)
                        {
                            receiveData.Add(255);
                        }
                    }
                }
                else
                {
                    int len = str[i];
                    len &= 0x7F;

                    for (int j = 0; j < len; j++)
                    {
                        i++;
                        int highbyte;
                        try
                        {
                            highbyte = str[i];
                        }
                        catch (Exception)
                        {
                            highbyte = 0;
                        }
                        highbyte &= 0xF0;
                        highbyte >>= 0x4;

                        int lowbyte;
                        try
                        {
                            lowbyte = str[i];
                        }
                        catch (Exception)
                        {
                            lowbyte = 0;
                        }
                        lowbyte &= 0x0F;

                        if (highbyte != 0x0 && highbyte != 0xF)
                        {
                            receiveData.Add(unchecked((byte)table[highbyte - 1]));
                            j++;
                        }

                        if (lowbyte != 0x0 && lowbyte != 0xF)
                        {
                            receiveData.Add(unchecked((byte)table[lowbyte - 1]));
                        }
                    }
                }
            }
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, receiveData.ToArray()));
        }

        public override string Decrypt(byte[] data, int sessionId = 0)
        {
            int sessionKey = sessionId & 0xFF;
            byte sessionNumber = unchecked((byte)(sessionId >> 6));
            sessionNumber &= 0xFF;
            sessionNumber &= unchecked((byte)0x80000003);

            StringBuilder decryptPart = new StringBuilder();
            switch (sessionNumber)
            {
                case 0:

                    foreach (byte character in data)
                    {
                        byte firstbyte = unchecked((byte)(sessionKey + 0x40));
                        byte highbyte = unchecked((byte)(character - firstbyte));
                        decryptPart.Append((char)highbyte);
                    }
                    break;

                case 1:
                    foreach (byte character in data)
                    {
                        byte firstbyte = unchecked((byte)(sessionKey + 0x40));
                        byte highbyte = unchecked((byte)(character + firstbyte));
                        decryptPart.Append((char)highbyte);
                    }
                    break;

                case 2:
                    foreach (byte character in data)
                    {
                        byte firstbyte = unchecked((byte)(sessionKey + 0x40));
                        byte highbyte = unchecked((byte)(character - firstbyte ^ 0xC3));
                        decryptPart.Append((char)highbyte);
                    }
                    break;

                case 3:
                    foreach (byte character in data)
                    {
                        byte firstbyte = unchecked((byte)(sessionKey + 0x40));
                        byte highbyte = unchecked((byte)(character + firstbyte ^ 0xC3));
                        decryptPart.Append((char)highbyte);
                    }
                    break;

                default:
                    decryptPart.Append((char)0xF);
                    break;
            }

            StringBuilder decrypted = new StringBuilder();

            string[] encryptedSplit = decryptPart.ToString().Split((char)0xFF);
            for (int i = 0; i < encryptedSplit.Length; i++)
            {
                decrypted.Append(Decrypt2(encryptedSplit[i]));
                if (i < encryptedSplit.Length - 2)
                {
                    decrypted.Append((char)0xFF);
                }
            }

            return decrypted.ToString();
        }

        public override string DecryptCustomParameter(byte[] data)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 1; i < data.Length; i++)
                {
                    if (Convert.ToChar(data[i]) == 0xE)
                    {
                        return builder.ToString();
                    }

                    int firstByte = Convert.ToInt32(data[i] - 0xF);
                    int secondByte = firstByte;
                    secondByte &= 0xF0;
                    firstByte = Convert.ToInt32(firstByte - secondByte);
                    secondByte >>= 0x4;

                    switch (secondByte)
                    {
                        case 0:
                        case 1:
                            builder.Append(' ');
                            break;

                        case 2:
                            builder.Append('-');
                            break;

                        case 3:
                            builder.Append('.');
                            break;

                        default:
                            secondByte += 0x2C;
                            builder.Append(Convert.ToChar(secondByte));
                            break;
                    }

                    switch (firstByte)
                    {
                        case 0:
                        case 1:
                            builder.Append(' ');
                            break;

                        case 2:
                            builder.Append('-');
                            break;

                        case 3:
                            builder.Append('.');
                            break;

                        default:
                            firstByte += 0x2C;
                            builder.Append(Convert.ToChar(firstByte));
                            break;
                    }
                }

                return builder.ToString();
            }
            catch (OverflowException)
            {
                return string.Empty;
            }
        }

        public override byte[] Encrypt(string data)
        {
            byte[] dataBytes = Encoding.Default.GetBytes(data);
            byte[] encryptedData = new byte[dataBytes.Length + (int)Math.Ceiling((decimal)dataBytes.Length / 0x7E) + 1];
            for (int i = 0, j = 0; i < dataBytes.Length; i++)
            {
                if (i % 0x7E == 0)
                {
                    encryptedData[i + j] = (byte)(dataBytes.Length - i > 0x7E ? 0x7E : dataBytes.Length - i);
                    j++;
                }
                encryptedData[i + j] = (byte)~dataBytes[i];
            }
            encryptedData[encryptedData.Length - 1] = 0xFF;
            return encryptedData;
        }

        #endregion
    }
}