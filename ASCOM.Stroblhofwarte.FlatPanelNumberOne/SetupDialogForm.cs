using ASCOM.Stroblhof.FlatPanelNumberOne;
using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.Stroblhof.FlatPanelNumberOne
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            CoverCalibrator.mqttHost = textBoxHost.Text;
            CoverCalibrator.mqttTopic = textBoxTopic.Text;
            CoverCalibrator.mqttOnMsg = textBoxOn.Text;
            CoverCalibrator.mqttOffMsg = textBoxOff.Text;

            CoverCalibrator.mqttSubscription = textBoxSub.Text;
            CoverCalibrator.mqttOnSub = textBoxOnState.Text;
            CoverCalibrator.mqttOffSub = textBoxOffState.Text;
            tl.Enabled = chkTrace.Checked;
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            chkTrace.Checked = tl.Enabled;
            // set the list of com ports to those that are currently available
            textBoxHost.Text = CoverCalibrator.mqttHost;
            textBoxTopic.Text = CoverCalibrator.mqttTopic;
            textBoxOn.Text = CoverCalibrator.mqttOnMsg;
            textBoxOff.Text = CoverCalibrator.mqttOffMsg;

            textBoxSub.Text = CoverCalibrator.mqttSubscription;
            textBoxOnState.Text = CoverCalibrator.mqttOnSub;
            textBoxOffState.Text = CoverCalibrator.mqttOffSub;
        }

        private void buttonTestOn_Click(object sender, EventArgs e)
        {
            uPLibrary.Networking.M2Mqtt.MqttClient testClient = new uPLibrary.Networking.M2Mqtt.MqttClient(IPAddress.Parse(textBoxHost.Text));
            testClient.Connect("FlatPanelTest");
            if(testClient.IsConnected)
            {
                labelInfo.Text = "Connected to " + textBoxHost.Text;
                testClient.Publish(textBoxTopic.Text, Encoding.UTF8.GetBytes(textBoxOn.Text));
            }
            else
            {
                labelInfo.Text = "Can not connect to " + textBoxHost.Text;
            }
        }

        private void buttonTestOff_Click(object sender, EventArgs e)
        {
            uPLibrary.Networking.M2Mqtt.MqttClient testClient = new uPLibrary.Networking.M2Mqtt.MqttClient(IPAddress.Parse(textBoxHost.Text));
            testClient.Connect("FlatPanelTest");
            if (testClient.IsConnected)
            {
                labelInfo.Text = "Connected to " + textBoxHost.Text;
                testClient.Publish(textBoxTopic.Text, Encoding.UTF8.GetBytes(textBoxOff.Text));
            }
            else
            {
                labelInfo.Text = "Can not connect to " + textBoxHost.Text;
            }
        }
    }
}