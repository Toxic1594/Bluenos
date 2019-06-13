using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Battle;
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Packets.ServerPackets;
using OpenNos.Master.Library.Client;

namespace Toxics_Console
{
    public partial class Form1 : Form
    {
        string channelpacket = CommunicationServiceClient.Instance.RetrieveRegisteredWorldServers("Toxic12", 2, true);
        private void timer1_Tick(object sender, EventArgs e)
        {
            /*int i = 0;
            foreach (string message in CommunicationServiceClient.Instance.RetrieveServerStatistics())
            {
                i++;
            }*/

                string[] sessionlist = { "Count: " + channelpacket};
            listBox1.BeginUpdate();

            listBox1.DataSource = sessionlist;

            listBox1.EndUpdate();
        }
        public Form1()
        {

            InitializeComponent();
        }
    }
}
