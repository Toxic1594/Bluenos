﻿/*
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
using System.Runtime.Serialization;

namespace OpenNos.Core.Networking.Communication.Scs.Communication
{
    /// <summary>
    /// This application is thrown in a communication error.
    /// </summary>
    [Serializable]
    public class CommunicationException : Exception
    {
        #region Instantiation

        /// <summary>
        /// Contstructor.
        /// </summary>
        public CommunicationException()
        {
        }

        /// <summary>
        /// Contstructor for serializing.
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="context"></param>
        protected CommunicationException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
        {
        }

        /// <summary>
        /// Contstructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public CommunicationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Contstructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public CommunicationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        #endregion
    }
}