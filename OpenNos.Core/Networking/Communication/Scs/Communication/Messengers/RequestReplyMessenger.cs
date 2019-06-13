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

using OpenNos.Core.Networking.Communication.Scs.Communication.Messages;
using OpenNos.Core.Networking.Communication.Scs.Communication.Protocols;
using OpenNos.Core.Threading;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.Core.Networking.Communication.Scs.Communication.Messengers
{
    /// <summary>
    /// This class adds SendMessageAndWaitForResponse(...) and SendAndReceiveMessage methods to a
    /// IMessenger for synchronous request/response style messaging. It also adds queued processing
    /// of incoming messages.
    /// </summary>
    /// <typeparam name="T">Type of IMessenger object to use as underlying communication</typeparam>
    public class RequestReplyMessenger<T> : IMessenger, IDisposable where T : IMessenger
    {
        #region Members

        /// <summary>
        /// Default Timeout value.
        /// </summary>
        private const int DEFAULT_TIMEOUT = 60000;

        /// <summary>
        /// This object is used to process incoming messages sequentially.
        /// </summary>
        private readonly SequentialItemProcessor<IScsMessage> _incomingMessageProcessor;

        /// <summary>
        /// This object is used for thread synchronization.
        /// </summary>
        private readonly object _syncObj = new object();

        /// <summary>
        /// This messages are waiting for a response those are used when
        /// SendMessageAndWaitForResponse is called.
        /// Key: MessageID of waiting request message.
        /// Value: A WaitingMessage instance.
        /// </summary>
        private readonly SortedList<string, WaitingMessage> _waitingMessages;

        private bool _disposed;

        #endregion

        #region Instantiation

        /// <summary>
        /// Creates a new RequestReplyMessenger.
        /// </summary>
        /// <param name="messenger">IMessenger object to use as underlying communication</param>
        public RequestReplyMessenger(T messenger)
        {
            Messenger = messenger;
            messenger.MessageReceived += Messenger_MessageReceived;
            messenger.MessageSent += Messenger_MessageSent;
            _incomingMessageProcessor = new SequentialItemProcessor<IScsMessage>(OnMessageReceived);
            _waitingMessages = new SortedList<string, WaitingMessage>();
            Timeout = DEFAULT_TIMEOUT;
        }

        #endregion

        #region Events

        /// <summary>
        /// This event is raised when a new message is received from underlying messenger.
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// This event is raised when a new message is sent without any error. It does not guaranties
        /// that message is properly handled and processed by remote application.
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageSent;

        #endregion

        #region Enums

        /// <summary>
        /// This enum is used to store the state of a waiting message.
        /// </summary>
        private enum WaitingMessageStates
        {
            /// <summary>
            /// Still waiting for response.
            /// </summary>
            WaitingForResponse,

            /// <summary>
            /// Message sending is cancelled.
            /// </summary>
            Cancelled,

            /// <summary>
            /// Response is properly received.
            /// </summary>
            ResponseReceived
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the time of the last succesfully received message.
        /// </summary>
        public DateTime LastReceivedMessageTime => Messenger.LastReceivedMessageTime;

        /// <summary>
        /// Gets the time of the last succesfully received message.
        /// </summary>
        public DateTime LastSentMessageTime => Messenger.LastSentMessageTime;

        /// <summary>
        /// Gets the underlying IMessenger object.
        /// </summary>
        public T Messenger { get; }

        /// <summary>
        /// Timeout value as milliseconds to wait for a receiving message on
        /// SendMessageAndWaitForResponse and SendAndReceiveMessage methods. Default value: 60000 (1 minute).
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Gets/sets wire protocol that is used while reading and writing messages.
        /// </summary>
        public IScsWireProtocol WireProtocol
        {
            get => Messenger.WireProtocol;
            set => Messenger.WireProtocol = value;
        }

        #endregion

        #region Methods

        public async Task ClearLowPriorityQueueAsync() => await Task.CompletedTask;

        /// <summary>
        /// Calls Stop method of this object.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        /// <param name="priority">Message priority to send</param>
        public void SendMessage(IScsMessage message, byte priority) => Messenger.SendMessage(message, priority);

        /// <summary>
        /// Sends a message and waits a response for that message.
        /// </summary>
        /// <remarks>
        /// Response message is matched with RepliedMessageId property, so if any other message (that
        /// is not reply for sent message) is received from remote application, it is not considered
        /// as a reply and is not returned as return value of this method.
        ///
        /// MessageReceived event is not raised for response messages.
        /// </remarks>
        /// <param name="message">message to send</param>
        /// <param name="priority">Message priority to send</param>
        /// <returns>Response message</returns>
        public IScsMessage SendMessageAndWaitForResponse(IScsMessage message, byte priority) => SendMessageAndWaitForResponse(message, Timeout, priority);

        /// <summary>
        /// Sends a message and waits a response for that message.
        /// </summary>
        /// <remarks>
        /// Response message is matched with RepliedMessageId property, so if any other message (that
        /// is not reply for sent message) is received from remote application, it is not considered
        /// as a reply and is not returned as return value of this method.
        ///
        /// MessageReceived event is not raised for response messages.
        /// </remarks>
        /// <param name="message">message to send</param>
        /// <param name="timeoutMilliseconds">Timeout duration as milliseconds.</param>
        /// <param name="priority">Message priority to send</param>
        /// <returns>Response message</returns>
        /// <exception cref="TimeoutException">
        /// Throws TimeoutException if can not receive reply message in timeout value
        /// </exception>
        /// <exception cref="CommunicationException">
        /// Throws CommunicationException if communication fails before reply message.
        /// </exception>
        public IScsMessage SendMessageAndWaitForResponse(IScsMessage message, int timeoutMilliseconds, byte priority)
        {
            // Create a waiting message record and add to list
            WaitingMessage waitingMessage = new WaitingMessage();
            lock (_syncObj)
            {
                _waitingMessages[message.MessageId] = waitingMessage;
            }

            try
            {
                // Send message
                Messenger.SendMessage(message, priority);

                // Wait for response
                waitingMessage.WaitEvent.Wait(timeoutMilliseconds);

                // Check for exceptions
                switch (waitingMessage.State)
                {
                    case WaitingMessageStates.WaitingForResponse:
                        throw new TimeoutException("Timeout occured. Can not received response.");

                    case WaitingMessageStates.Cancelled:
                        throw new CommunicationException("Disconnected before response received.");
                }

                // return response message
                return waitingMessage.ResponseMessage;
            }
            finally
            {
                // Remove message from waiting messages
                lock (_syncObj)
                {
                    if (_waitingMessages.ContainsKey(message.MessageId))
                    {
                        _waitingMessages.Remove(message.MessageId);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the messenger.
        /// </summary>
        public virtual void Start() => _incomingMessageProcessor.Start();

        /// <summary>
        /// Stops the messenger. Cancels all waiting threads in SendMessageAndWaitForResponse method
        /// and stops message queue. SendMessageAndWaitForResponse method throws exception if there
        /// is a thread that is waiting for response message. Also stops incoming message processing
        /// and deletes all messages in incoming message queue.
        /// </summary>
        public virtual void Stop()
        {
            _incomingMessageProcessor.Stop();

            // Pulse waiting threads for incoming messages, since underlying messenger is
            // disconnected and can not receive messages anymore.
            lock (_syncObj)
            {
                foreach (WaitingMessage waitingMessage in _waitingMessages.Values)
                {
                    waitingMessage.State = WaitingMessageStates.Cancelled;
                    waitingMessage.WaitEvent.Set();
                }

                _waitingMessages.Clear();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        /// <summary>
        /// Raises MessageReceived event.
        /// </summary>
        /// <param name="message">Received message</param>
        protected virtual void OnMessageReceived(IScsMessage message) => MessageReceived?.Invoke(this, new MessageEventArgs(message, DateTime.Now));

        /// <summary>
        /// Raises MessageSent event.
        /// </summary>
        /// <param name="message">Received message</param>
        protected virtual void OnMessageSent(IScsMessage message) => MessageSent?.Invoke(this, new MessageEventArgs(message, DateTime.Now));

        /// <summary>
        /// Handles MessageReceived event of Messenger object.
        /// </summary>
        /// <param name="sender">Source of event</param>
        /// <param name="e">Event arguments</param>
        private void Messenger_MessageReceived(object sender, MessageEventArgs e)
        {
            // Check if there is a waiting thread for this message in SendMessageAndWaitForResponse method
            if (!string.IsNullOrEmpty(e.Message.RepliedMessageId))
            {
                WaitingMessage waitingMessage = null;
                lock (_syncObj)
                {
                    if (_waitingMessages.ContainsKey(e.Message.RepliedMessageId))
                    {
                        waitingMessage = _waitingMessages[e.Message.RepliedMessageId];
                    }
                }

                // If there is a thread waiting for this response message, pulse it
                if (waitingMessage != null)
                {
                    waitingMessage.ResponseMessage = e.Message;
                    waitingMessage.State = WaitingMessageStates.ResponseReceived;
                    waitingMessage.WaitEvent.Set();
                    return;
                }
            }

            _incomingMessageProcessor.EnqueueMessage(e.Message);
        }

        /// <summary>
        /// Handles MessageSent event of Messenger object.
        /// </summary>
        /// <param name="sender">Source of event</param>
        /// <param name="e">Event arguments</param>
        private void Messenger_MessageSent(object sender, MessageEventArgs e) => OnMessageSent(e.Message);

        #endregion

        #region Classes

        /// <summary>
        /// This class is used to store messaging context for a request message until response is received.
        /// </summary>
        private sealed class WaitingMessage : IDisposable
        {
            #region Instantiation

            /// <summary>
            /// Creates a new WaitingMessage object.
            /// </summary>
            public WaitingMessage()
            {
                WaitEvent = new ManualResetEventSlim(false);
                State = WaitingMessageStates.WaitingForResponse;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Response message for request message (null if response is not received yet).
            /// </summary>
            public IScsMessage ResponseMessage { get; set; }

            /// <summary>
            /// State of the request message.
            /// </summary>
            public WaitingMessageStates State { get; set; }

            /// <summary>
            /// ManualResetEvent to block thread until response is received.
            /// </summary>
            public ManualResetEventSlim WaitEvent { get; }

            #endregion

            #region Methods

            public void Dispose() => throw new NotImplementedException();

            #endregion
        }

        #endregion
    }
}