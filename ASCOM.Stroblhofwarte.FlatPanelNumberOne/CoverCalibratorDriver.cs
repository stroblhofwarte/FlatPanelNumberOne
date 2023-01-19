//tabs=4
// --------------------------------------------------------------------------------
// This file is part of the Stroblhofwarte.FlatPanelNumberOne project 
// (https://github.com/stroblhofwarte/FlatPanelNumberOne.git).
// Copyright (c) 2022, Othmar Ehrhardt, https://astro.stroblhof-oberrohrbach.de
//
// This program is free software: you can redistribute it and/or modify  
// it under the terms of the GNU General Public License as published by  
// the Free Software Foundation, version 3.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
// General Public License for more details.
//
// You should have received a copy of the GNU General Public License 
// along with this program. If not, see <http://www.gnu.org/licenses/>.
// ASCOM CoverCalibrator driver for Stroblhof.FlatPanelNumberOne
//
// Description:	This driver is related to the arduino firmware in this repo
//              to drive a simple 20 EUR drawing pad via a Motor Shield to
//              do an automatic flat frame creation for your astronomical
//              imagin session.
// Implements:	ASCOM CoverCalibrator interface version: 6.6
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 15-05-2022	Othmar Ehrhardt	0.0.1	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.
#define CoverCalibrator

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ASCOM.Stroblhof.FlatPanelNumberOne
{
    //
    // DeviceID is ASCOM.Stroblhof.FlatPanelNumberOne.CoverCalibrator
    //
    // The Guid attribute sets the CLSID for ASCOM.Stroblhof.FlatPanelNumberOne.CoverCalibrator
    // The ClassInterface/None attribute prevents an empty interface called
    // _Stroblhof.FlatPanelNumberOne from being created and used as the [default] interface
    //
   
    /// <summary>
    /// ASCOM CoverCalibrator Driver for Stroblhof.FlatPanelNumberOne.
    /// </summary>
    [Guid("62ae66ca-6c39-4be3-86ed-ad8995abd6c5")]
    [ClassInterface(ClassInterfaceType.None)]
    public class CoverCalibrator : ICoverCalibratorV1
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.Stroblhof.FlatPanelNumberOne.CoverCalibrator";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "ASCOM CoverCalibrator Driver for Stroblhof.FlatPanelNumberOne.";

        internal static string mqttHostProfileName = "MQTTBroker"; // Constants used for Profile persistence
        internal static string mqttHostDefault = "192.168.0.1";
        internal static string mqttTopicProfileName = "MQTTTopic"; // Constants used for Profile persistence
        internal static string mqttTopicDefault = "my/relay/topic";
        internal static string mqttOnProfileName = "MQTTOnMsg"; // Constants used for Profile persistence
        internal static string mqttOnDefault = "on";
        internal static string mqttOffProfileName = "MQTTOffMsg"; // Constants used for Profile persistence
        internal static string mqttOffDefault = "off";

        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        internal static string mqttHost;
        internal static string mqttTopic;
        internal static string mqttOnMsg;
        internal static string mqttOffMsg;

        private bool _calibratorOn = false;
        
        private ASCOM.Utilities.Serial _serial;
        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool connectedState;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal TraceLogger tl;

        private object _lock = new object();
        private uPLibrary.Networking.M2Mqtt.MqttClient _mqttClient = null;
        private string _mqttClientId = "Stroblhof.FlatPanelNumberOne";

        /// <summary>
        /// Initializes a new instance of the <see cref="Stroblhof.FlatPanelNumberOne"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public CoverCalibrator()
        {
            tl = new TraceLogger("", "Stroblhof.FlatPanelNumberOne");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl.LogMessage("CoverCalibrator", "Starting initialisation");

            connectedState = false; // Initialise connected to false
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro-utilities object
            //TODO: Implement your additional construction here

            tl.LogMessage("CoverCalibrator", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE ICoverCalibratorV1 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(tl))
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            this.CommandString(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            return true;
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            string ret = CommandString(command, raw);
            return ret;
        }

        public void Dispose()
        {
            // Clean up the trace logger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
        }


        private string GetInfoString()
        {
            if (!connectedState) return "Not connected.";
            lock (_lock)
            {
                return "Connected to:" + mqttHost + ":1883";
            }
        }
        public bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;
                if (value)
                {
                    LogMessage("Connected Set", "Connecting to address {0}", mqttHost);
                    try
                    {
                        _mqttClient = new uPLibrary.Networking.M2Mqtt.MqttClient(IPAddress.Parse(mqttHost));
                        _mqttClient.Connect(_mqttClientId);
                        connectedState = _mqttClient.IsConnected;
                    }
                    catch (Exception ex)
                    {
                        _mqttClient = null;
                        connectedState = false;
                        LogMessage("Connected Set", ex.ToString());
                    }
                }
                else
                {
                    _mqttClient = null;
                    connectedState = false;
                    LogMessage("Connected Set", "Disconnecting from adress {0}", mqttHost);
                }

            }
        }

        public string Description
        {
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "Stroblhowarte FlatPanel: " + GetInfoString();
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "1");
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                string name = "Stroblhofwarte.FlatPanelNumberOne";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ICoverCalibrator Implementation

        /// <summary>
        /// Returns the state of the device cover, if present, otherwise returns "NotPresent"
        /// </summary>
        public CoverStatus CoverState
        {
            get
            {
                return CoverStatus.NotPresent;
            }
        }

        /// <summary>
        /// Initiates cover opening if a cover is present
        /// </summary>
        public void OpenCover()
        {
            return;
        }

        /// <summary>
        /// Initiates cover closing if a cover is present
        /// </summary>
        public void CloseCover()
        {
            return;
        }

        /// <summary>
        /// Stops any cover movement that may be in progress if a cover is present and cover movement can be interrupted.
        /// </summary>
        public void HaltCover()
        {
            return;
        }

        /// <summary>
        /// Returns the state of the calibration device, if present, otherwise returns "NotPresent"
        /// </summary>
        public CalibratorStatus CalibratorState
        {
            get
            {
                if(!_calibratorOn)
                    return CalibratorStatus.Off;
                return CalibratorStatus.Ready;
            }
        }

        /// <summary>
        /// Returns the current calibrator brightness in the range 0 (completely off) to <see cref="MaxBrightness"/> (fully on)
        /// </summary>
        public int Brightness
        {
            get
            {
                if (_calibratorOn)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// The Brightness value that makes the calibrator deliver its maximum illumination.
        /// </summary>
        public int MaxBrightness
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Turns the calibrator on at the specified brightness if the device has calibration capability
        /// </summary>
        /// <param name="Brightness"></param>
        public void CalibratorOn(int Brightness)
        {
            if (Brightness > 0)
            {
                _mqttClient.Publish(mqttTopic, Encoding.UTF8.GetBytes(mqttOnMsg));
                _calibratorOn = true;
            }
            else
            {
                _mqttClient.Publish(mqttTopic, Encoding.UTF8.GetBytes(mqttOffMsg));
                _calibratorOn = false;
            }
        }

        /// <summary>
        /// Turns the calibrator off if the device has calibration capability
        /// </summary>
        public void CalibratorOff()
        {
            _mqttClient.Publish(mqttTopic, Encoding.UTF8.GetBytes(mqttOffMsg));
            _calibratorOn = false;
            
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "CoverCalibrator";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "CoverCalibrator";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                mqttHost = driverProfile.GetValue(driverID, mqttHostProfileName, string.Empty, mqttHostDefault);
                mqttTopic = driverProfile.GetValue(driverID, mqttTopicProfileName, string.Empty, mqttTopicDefault);
                mqttOnMsg = driverProfile.GetValue(driverID, mqttOnProfileName, string.Empty, mqttOnDefault);
                mqttOffMsg = driverProfile.GetValue(driverID, mqttOffProfileName, string.Empty, mqttOffDefault);
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "CoverCalibrator";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, mqttHostProfileName, mqttHost);
                driverProfile.WriteValue(driverID, mqttTopicProfileName, mqttTopic);
                driverProfile.WriteValue(driverID, mqttOnProfileName, mqttOnMsg);
                driverProfile.WriteValue(driverID, mqttOffProfileName, mqttOffMsg);
            }
    }


    /// <summary>
    /// Log helper function that takes formatted strings and arguments
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    internal void LogMessage(string identifier, string message, params object[] args)
    {
        var msg = string.Format(message, args);
        tl.LogMessage(identifier, msg);
    }
    #endregion
    }
}
