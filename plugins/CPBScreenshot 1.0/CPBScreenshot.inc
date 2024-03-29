/*  Copyright 2010 Zaeed (Matt Green)

    http://aussieunderdogs.com

    This file is part of Zaeed's Plugins for BFBC2 PRoCon.

    Zaeed's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Zaeed's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Zaeed's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
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
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;
using PRoCon.Core.HttpServer;

namespace PRoConEvents {
    public class CPBScreenshot : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo;
        private Dictionary<string, CPlayerInfo> m_dicPlayerInfo;

        private string m_strConfirmCommand;
        private string m_strCommand;

        private bool m_isPluginEnabled;

        public CPBScreenshot()
        {

            this.m_strCommand = "ss";
            this.m_strConfirmCommand = "yes";


            this.m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
            this.m_dicPlayerInfo = new Dictionary<string, CPlayerInfo>();

            this.m_isPluginEnabled = false;
        }

        public string GetPluginName() {
            return "PB Screenshots";
        }

        public string GetPluginVersion() {
            return "1.0.0.0";
        }

        public string GetPluginAuthor() {
            return "Zaeed";
        }

        public string GetPluginWebsite() {
            return "aussieunderdogs.com";
        }

        public string GetPluginDescription() {
            return @"<p>If you find my plugins useful, please feel free to donate</p>
<blockquote>
<form action=""https://www.paypal.com/cgi-bin/webscr/"" method=""POST"" target=""_blank"">
<input type=""hidden"" name=""cmd"" value=""_s-xclick"">
<input type=""hidden"" name=""encrypted"" value=""-----BEGIN PKCS7-----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-----END PKCS7-----"">
<input type=""image"" src=""https://www.paypal.com/en_AU/i/btn/btn_donate_LG.gif"" border=""0"" name=""submit"" alt=""PayPal - The safer, easier way to pay online."">
<img alt="""" border=""0"" src=""https://www.paypal.com/en_AU/i/scr/pixel.gif"" width=""1"" height=""1"">
</form>
</blockquote>

<h2>Description</h2>
    <p>A very basic tool to allow for admins to take PB screenshots while ingame</p>

<h2>Player Commands</h2>
    <blockquote>
   	  <h4>ss</h4>
        This will take a screenshot of player.  Not including a player name will screenshot the entire server.
    </blockquote>
     ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnPunkbusterPlayerInfo");
        }

        public void OnPluginEnable() {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPB Screenshots ^2Enabled!");

            this.m_isPluginEnabled = true;
            this.RegisterAllCommands();
        }

        public void OnPluginDisable() {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPB Screenshots ^1Disabled =(");

            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
        }

        public List<CPluginVariable> GetDisplayPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Confirmation command", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            lstReturn.Add(new CPluginVariable("Screenshot Command", this.m_strCommand.GetType(), this.m_strCommand));
            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Confirmation command", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            lstReturn.Add(new CPluginVariable("Screenshot command", this.m_strCommand.GetType(), this.m_strCommand));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue) {
            int intOut;
            if (strVariable.CompareTo("Confirmation command") == 0)
            {
                this.m_strConfirmCommand = strValue;
            }
            else if (strVariable.CompareTo("Screenshot command") == 0)
            {
                this.m_strCommand = strValue;
            }
        

            this.RegisterAllCommands();
        }

        private void UnregisterAllCommands()
        {
            MatchCommand confirmationCommand = new MatchCommand(this.Listify<string>("@", "!", "#", "/"), this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

            this.UnregisterCommand(new MatchCommand("CPBScreenshot", "OnCommandPBScreenshot", this.Listify<string>("@", "!", "#", "/"), this.m_strCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, confirmationCommand, "You do not have the required permissions for this command."), "PB Screenshots players"));
        }

        private void RegisterAllCommands() {

            if (this.m_isPluginEnabled == true) {

                MatchCommand confirmationCommand = new MatchCommand(this.Listify<string>("@", "!", "#", "/"), this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

                this.RegisterCommand(new MatchCommand("CPBScreenshot", "OnCommandPBScreenshotEveryone", this.Listify<string>("@", "!", "#", "/"), this.m_strCommand, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account, 1, confirmationCommand, "You do not have the required permissions for this command."), "PB Screenshots players"));
                this.RegisterCommand(new MatchCommand("CPBScreenshot", "OnCommandPBScreenshot", this.Listify<string>("@", "!", "#", "/"), this.m_strCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, confirmationCommand, "You do not have the required permissions for this command."), "PB Screenshots players"));
            }
        }

        #region Server Events

        public override void OnPlayerJoin(string strSoldierName)
        {

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == false) {
                this.m_dicPlayerInfo.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
            }

        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            if (this.m_dicPbInfo.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.m_dicPbInfo.Remove(cpiPlayer.SoldierName);
                
            }

            if (this.m_dicPlayerInfo.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.m_dicPlayerInfo.Remove(cpiPlayer.SoldierName);
            }

            this.RegisterAllCommands();
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

            if (cpbiPlayer != null) {
                if (this.m_dicPbInfo.ContainsKey(cpbiPlayer.SoldierName) == false) {
                    this.m_dicPbInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                }
                else {
                    this.m_dicPbInfo[cpbiPlayer.SoldierName] = cpbiPlayer;
                }

                this.RegisterAllCommands();
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset) {

            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All) {
                foreach (CPlayerInfo cpiPlayer in lstPlayers) {
                    if (this.m_dicPlayerInfo.ContainsKey(cpiPlayer.SoldierName) == true) {
                        this.m_dicPlayerInfo[cpiPlayer.SoldierName] = cpiPlayer;
                    }
                    else {
                        this.m_dicPlayerInfo.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }
                }
            }

        }

        #endregion

        public void OnCommandPBScreenshot(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope) 
        {
            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true) 
            {
                this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_getss " + this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SlotID);
            }
            else 
            {
                // Invalid in BF3
                //this.ExecuteCommand("procon.protected.send", "admin.say", "Player not found", "player", strSpeaker);
            }
        }

        public void OnCommandPBScreenshotEveryone(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope) 
        {
            this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_getss");
        }

    }
}