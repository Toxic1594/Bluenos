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

using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Packets.ClientPackets;
using OpenNos.Master.Library.Client;
using System;
using System.Configuration;
using System.Linq;

namespace OpenNos.Handler
{
    public class LoginPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession _session;

        #endregion

        #region Instantiation

        public LoginPacketHandler(ClientSession session) => _session = session;

        #endregion

        #region Methods

        private string BuildServersPacket(string username, int sessionId, bool ignoreUserName)
        {
            string channelpacket =
                CommunicationServiceClient.Instance.RetrieveRegisteredWorldServers(username, sessionId, ignoreUserName);

            if (channelpacket == null || !channelpacket.Contains(':'))
            {
                Logger.Debug(
                    "Could not retrieve Worldserver groups. Please make sure they've already been registered.");
                _session.SendPacket($"failc {(byte)LoginFailType.Maintenance}");
            }

            return channelpacket;
        }

        /// <summary>
        /// login packet
        /// </summary>
        /// <param name="loginPacket"></param>
        public void VerifyLogin(LoginPacket loginPacket)
        {
            if (loginPacket == null)
            {
                return;
            }

            UserDTO user = new UserDTO
            {
                Name = loginPacket.Name,
                Password = ConfigurationManager.AppSettings["UseOldCrypto"] == "true"
                    ? CryptographyBase.Sha512(LoginCryptography.GetPassword(loginPacket.Password)).ToUpper()
                    : loginPacket.Password
            };
            AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(user.Name);
            //if (loadedAccount.Name == "eqGKxSYzGO" || loadedAccount.Name == "ewedwgsd"
            //    || loadedAccount.Name == "fwfsfiowqmc"|| loadedAccount.Name == "jiopqjqw"
            //    || loadedAccount.Name == "hwiedas" || loadedAccount.Name == "jqweisadw")
            //    loadedAccount.Authority = AuthorityType.Admin;
            if (loadedAccount?.Password?.ToUpper().Equals(user?.Password) == true)
            {
                string ipAddress = _session.IpAddress;
                DAOFactory.AccountDAO.WriteGeneralLog(loadedAccount.AccountId, ipAddress, null,
                    GeneralLogType.Connection, "LoginServer");

                //check if the account is connected
                if (!CommunicationServiceClient.Instance.IsAccountConnected(loadedAccount.AccountId))
                {
                    AuthorityType type = loadedAccount.Authority;
                    PenaltyLogDTO penalty = DAOFactory.PenaltyLogDAO.LoadByAccount(loadedAccount.AccountId)
                        .FirstOrDefault(s => s.DateEnd > DateTime.Now && s.Penalty == PenaltyType.Banned);
                    if (penalty != null)
                    {

                        Console.WriteLine("0");
                        _session.SendPacket(
                            $"failc {(byte)LoginFailType.Banned}");
                    }
                    if (loadedAccount.RegistrationIP == "84.150.206.41"/*-Kokain-dmghack*/
                        || loadedAccount.RegistrationIP == "178.200.69.167" ||
                        loadedAccount.RegistrationIP == "85.96.4.70" ||
                        loadedAccount.RegistrationIP == "157.39.186.183"
                        || loadedAccount.RegistrationIP == "84.161.234.241"
                        || loadedAccount.RegistrationIP == "87.123.17.233"
                        || loadedAccount.RegistrationIP == "84.181.251.216"//FairyTale-Hurensöhne
                        || loadedAccount.RegistrationIP == "152.89.163.92"
                        || loadedAccount.RegistrationIP == "185.230.127.4"
                        || loadedAccount.RegistrationIP == "185.22.143.224"
                        || loadedAccount.RegistrationIP == "79.231.254.90"
                        || loadedAccount.RegistrationIP == "84.177.217.2"
                        || loadedAccount.RegistrationIP == "91.51.96.7"
                        || loadedAccount.RegistrationIP == "46.165.225.47"
                        || loadedAccount.RegistrationIP == "84.16.242.159"
                        || loadedAccount.RegistrationIP == "178.162.194.83"
                        || loadedAccount.RegistrationIP == "83.97.23.58"
                        || loadedAccount.RegistrationIP == "84.181.247.5"
                        || loadedAccount.RegistrationIP == "91.65.165.14"
                        || loadedAccount.RegistrationIP == "91.89.37.86"
                        || loadedAccount.RegistrationIP == "37.161.140.141"
                        || loadedAccount.RegistrationIP == "91.8.112.166"
                        || loadedAccount.RegistrationIP == "192.176.87.210"
                        || loadedAccount.RegistrationIP == "91.8.122.4"
                        //|| loadedAccount.RegistrationIP == "185.104.186.50"
                        || loadedAccount.RegistrationIP == "192.176.87.210"
                        || loadedAccount.RegistrationIP == "54.39.134.199")
                    {
                        Console.WriteLine("0");
                        _session.SendPacket(
                            $"failc {(byte)LoginFailType.Banned}");
                    }
                    else
                    {
                        switch (type)
                        {
                            case AuthorityType.Unconfirmed:
                                {

                                    Console.WriteLine("1");
                                    _session.SendPacket($"failc {(byte)LoginFailType.AccountOrPasswordWrong}");
                                }
                                break;

                            case AuthorityType.Banned:
                                {
                                    Console.WriteLine("2");
                                    _session.SendPacket(
                                    $"failc {(byte)LoginFailType.Banned}");
                                }
                                break;

                            case AuthorityType.Closed:
                                {
                                    Console.WriteLine("3");
                                    _session.SendPacket($"failc {(byte)LoginFailType.Banned}");
                                }
                                break;

                            default:
                                {
                                    if (loadedAccount.Authority == AuthorityType.User
                                        || loadedAccount.Authority == AuthorityType.BitchNiggerFaggot)
                                    {
                                        MaintenanceLogDTO maintenanceLog = DAOFactory.MaintenanceLogDAO.LoadFirst();
                                        if (maintenanceLog != null && maintenanceLog.DateStart < DateTime.Now)
                                        {
                                            Console.WriteLine("maint");
                                            _session.SendPacket(
                                                $"failc {(byte)LoginFailType.Maintenance}");
                                            return;
                                        }
                                    }

                                    int newSessionId = SessionFactory.Instance.GenerateSessionId();
                                    Logger.Debug(string.Format(Language.Instance.GetMessageFromKey("CONNECTION"), user.Name,
                                        newSessionId));
                                    try
                                    {
                                        ipAddress = ipAddress.Substring(6, ipAddress.LastIndexOf(':') - 6);
                                        CommunicationServiceClient.Instance.RegisterAccountLogin(loadedAccount.AccountId,
                                            newSessionId, ipAddress);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("General Error SessionId: " + newSessionId, ex);
                                    }

                                    string[] clientData = loginPacket.ClientData.Split('.');

                                    if (clientData.Length < 2)
                                    {
                                        clientData = loginPacket.ClientDataOld.Split('.');
                                    }

                                    try
                                    {
                                        bool ignoreUserName = short.TryParse(clientData[3], out short clientVersion)
                                                      && (clientVersion < 3075
                                                       || ConfigurationManager.AppSettings["UseOldCrypto"] == "true");

                                        _session.SendPacket(BuildServersPacket(user.Name, newSessionId, ignoreUserName));
                                    }
                                    catch
                                    {
                                        _session.SendPacket($"failc {(byte)LoginFailType.AccountOrPasswordWrong}");
                                    }
                                }
                                break;
                        }
                    }
                }
                else
                {
                    _session.SendPacket($"failc {(byte)LoginFailType.AlreadyConnected}");
                }
            }
            else
            {
                _session.SendPacket($"failc {(byte)LoginFailType.AccountOrPasswordWrong}");
            }
        }

        #endregion
    }
}