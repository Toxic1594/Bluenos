﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenNos.Master.Library.Client;
using System.Linq;
using System.Threading.Tasks;

namespace OpenNos.Test
{
    [TestClass]
    public class WebApiTest
    {
        #region Methods

        [TestMethod]
        public async Task TestParelellConnectionsAsync()
        {
            CommunicationServiceClient.Instance.Cleanup();

            foreach (int x in Enumerable.Range(1, 50000))
            {
                await Task.Factory.StartNew(() =>
                {
                    CommunicationServiceClient.Instance.RegisterAccountLogin(x, x, "127.0.0.1");
                    bool hasRegisteredAccountLogin = CommunicationServiceClient.Instance.IsLoginPermitted(x, x);
                    Assert.IsTrue(hasRegisteredAccountLogin);
                }).ConfigureAwait(false);
            }
        }

        #endregion
    }
}