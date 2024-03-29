/*  Copyright 2012 Geoffrey 'Phogue' Green, [NER]Zirconium[HQ] & EBassie

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents {
    public class CMixedSpambot : PRoConPluginAPI, IPRoConPluginInterface {

        private List<string> m_lstMessages;
        //private int m_iDisplayTime;
		private int m_iYellDisplayTime;		
        private int m_iIntervalBetweenMessages;

        // Status
        private string m_strServerGameType;
        private string m_strGameMod;
        private string m_strServerVersion;
        private string m_strPRoConVersion;

        private int m_iYellDivider;

        private bool m_blPluginEnabled = false;
        private bool m_blServerTypeChecked = false;

        private enumBoolYesNo m_enYellResponses;

        public CMixedSpambot() {
            this.m_lstMessages = new List<string>();
			this.m_lstMessages.Add("@Yelled Example message 1");
            this.m_lstMessages.Add("Example message 2");

            this.m_enYellResponses = enumBoolYesNo.No;

            //this.m_iDisplayTime = 8000;
			this.m_iYellDisplayTime = 8000;
            this.m_iIntervalBetweenMessages = 60;

            this.m_strServerGameType = "none";
            this.m_iYellDivider = 1;
        }

        public string GetPluginName() {
            return "Mixed Spambot";
        }

        public string GetPluginVersion() {
            return "1.1.0.1";
        }

        public string GetPluginAuthor() {
            return "EBassie (based on Phogue's Spambot & [NER]Zirconium[HQ] Mixed Spambot for BC2)";
        }

        public string GetPluginWebsite() {
            return "www.11-clan.eu";
        }

        public string GetPluginDescription() {
            return @"
<h2>Description</h2>			
<p>Spams a chat or YELL message to all the players at a set interval.</p>

<h2>Usage</h2>
When you want a line to be YELLED, place a @-sign in front of that line.
";
        }

        public void OnPluginLoadingEnv(List<string> lstPluginEnv) {
            Version PRoConVersion = new Version(lstPluginEnv[0]);
            this.m_strPRoConVersion = PRoConVersion.ToString();
            this.m_strServerGameType = lstPluginEnv[1].ToLower();
            this.m_strGameMod = lstPluginEnv[2];
            this.m_strServerVersion = lstPluginEnv[3];

            if (this.m_strServerGameType == "bf3") {
                this.m_iYellDivider = 1000;
            }
        }
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
			// This is just so procon knows this plugin wants to override the default
			// "fire every event" setting when no events are registered.
			this.RegisterEvents(this.GetType().Name, "OnPluginLoaded");
        }

        public void OnPluginEnable() {
            this.m_blPluginEnabled = true;
            this.UpdateMixedSpamBot();

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedSpambot ^2Enabled!" );
        }

        public void OnPluginDisable() {
            this.m_blPluginEnabled = false;
            this.ExecuteCommand("procon.protected.tasks.remove", "CMixedSpambot");

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedSpambot ^1Disabled =(");
        }

        private void UpdateMixedSpamBot() {

            this.ExecuteCommand("procon.protected.tasks.remove", "CMixedSpambot");

            if (this.m_blPluginEnabled == true) {

                int iDelay = 0;
                foreach (string orgStrMessage in this.m_lstMessages) {
                    string strMessage = "";
        			// Looking for @ in front of message or not
                    if (orgStrMessage.Substring(0, 1) == "@")  {
                        strMessage = orgStrMessage.Substring(1, orgStrMessage.Length - 1);
                    } else {
                        strMessage = orgStrMessage;
                    }				
                    if (this.m_enYellResponses != enumBoolYesNo.Yes && orgStrMessage.Substring(0, 1) == "@") {
                        this.ExecuteCommand("procon.protected.tasks.add", "CMixedSpambot", iDelay.ToString(), (this.m_lstMessages.Count * this.m_iIntervalBetweenMessages).ToString(), "-1", "procon.protected.send", "admin.yell", strMessage,(this.m_iYellDisplayTime / this.m_iYellDivider).ToString(), "all");
                    }					
                    else {
                        this.ExecuteCommand("procon.protected.tasks.add", "CMixedSpambot", iDelay.ToString(), (this.m_lstMessages.Count * this.m_iIntervalBetweenMessages).ToString(), "-1", "procon.protected.send", "admin.say", strMessage, "all");
                    }

                    iDelay += this.m_iIntervalBetweenMessages;
                }
            }
        }

		// GetDisplayPluginVariables and GetPluginVariables
		#include "CMixedSpambot.PluginVariables.inc"

        public void SetPluginVariable(string strVariable, string strValue) {
            int iTimeSeconds = 8;

            if (strVariable.CompareTo("Messages") == 0) {
                this.m_lstMessages = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Interval between messages (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true) {
                this.m_iIntervalBetweenMessages = iTimeSeconds;
            }
 			else if (strVariable.CompareTo("Yell duration (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true) {
                this.m_iYellDisplayTime = iTimeSeconds * 1000;

                if (iTimeSeconds <= 0) {
                    this.m_iYellDisplayTime = 1 * 1000;
                }
                else if (iTimeSeconds > 60) {
                    this.m_iYellDisplayTime = 59 * 1000;
                }
            }			

            this.UpdateMixedSpamBot();
        }
    }
}