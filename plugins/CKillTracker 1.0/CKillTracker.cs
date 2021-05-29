/*  Copyright 2010 [LAB]HeliMagnet (Gerry Wohlrab)

    http://www.luckyatbingo.net

    This file is part of [LAB]HeliMagnet's Plugins for BFBC2 PRoCon.

    [LAB]HeliMagnet's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [LAB]HeliMagnet's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with [LAB]HeliMagnet's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
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

namespace PRoConEvents
{
    public class CKillTracker : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private int m_iDisplayTime;
        private enumBoolYesNo m_enYellResponses;
        private string m_strSelectedFullWeaponList;
        private string m_strDisplay;
        private List<string> m_weaponAndMessages;                                           // User defined list of kills and messages
        private string m_strSelectedConfirmList;

        public CKillTracker()
        {
            this.m_iDisplayTime = 8000;
            this.m_enYellResponses = enumBoolYesNo.No;
            this.m_strSelectedFullWeaponList = "";
            this.m_strDisplay = "%pk% embarrassed %pv% with a %wp%!";
            this.m_weaponAndMessages = new List<string>();
            this.m_weaponAndMessages.Add("Combat Knife//%pk% embarrassed %pv% with a %wp%!");
            this.m_strSelectedConfirmList = "";
        }

        public string GetPluginName()
        {
            return "Kill Tracker";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.0";
        }

        public string GetPluginAuthor()
        {
            return "HeliMagnet and Zaeed";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<p>For support or to post comments regarding this plugin please visit <a href=""http://phogue.net/forum/viewtopic.php?f=13&t=935"" target=""_blank"">Plugin Thread</a></p>

<p>This plugin works with PRoCon and falls under the GNU GPL, please read the included gpl.txt for details.
I have decided to work on PRoCon plugins without compensation. However, if you would like to donate to support the development of PRoCon, click the link below:
<a href=""http://phogue.net"" target=""_new""><img src=""http://i398.photobucket.com/albums/pp65/scoop27585/phoguenetdonate.png"" width=""482"" height=""84"" border=""0"" alt=""PRoCon Donation"" /></a></p>
<p>Toward the right side of the page, there is a location to enter the amount you would like to donate and whether you want the donation to be made public. Your donations are greatly appreciated and will be sent to Phogue (original creator of PRoCon).</p>
<h2>Description</h2>
    <p>Shows kills of specified weapons.</p>

<h2>Commands</h2>
	<p>No commands available with this plugin.</p>

<h2>Settings</h2>
    <h3>Miscellaneous</h3>
        <blockquote><h4>Yell Kills</h4>Setting to true will yell to all players (center flashing text) or setting to false will display in chat.</blockquote>
        <blockquote><h4>Full Weapon List</h4>Select the weapon you want to add or remove to tracking list.</blockquote>
        <blockquote><h4>Custom Message</h4>The message you want to include with the kill.</blockquote>
        	<ul>
                <li>Include killer: Use ""%pk%""</li>
                <li>Include victim: Use ""%pv%""</li>
                <li>Include weapon used in kill: Use ""%wp%""</li>
                <li>An example: %pk% embarrassed %pv% with a %wp%! Which could mean: HeliMagnet embarrassed Phogue with a Combat Knife!</li>
            </ul>
        <blockquote><h4>Add or Remove Weapon in List</h4>Add or remove weapon and custom message to tracking list.</blockquote>
        <blockquote><h4>Weapon and Message List</h4>Editable list of weapons tracked, along with their messages when a kill occurs.</blockquote>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bKill Tracker Plugin ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bKill Tracker Plugin ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Yell Kills", typeof(enumBoolYesNo), this.m_enYellResponses));

            lstReturn.Add(this.GetWeaponListPluginVariable("Full Weapon List", "KillStreakWeaponList", this.m_strSelectedFullWeaponList, DamageTypes.None));
            lstReturn.Add(new CPluginVariable("Custom Message", typeof(string), this.m_strDisplay));
            lstReturn.Add(new CPluginVariable("Add or Remove Weapon in List", "enum.AddRemoveWeaponTracker(Add|Remove)", this.m_strSelectedConfirmList));
            lstReturn.Add(new CPluginVariable("Weapon and Message List", typeof(string[]), this.m_weaponAndMessages.ToArray()));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Yell Kills") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellResponses = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Full Weapon List") == 0)
            {
                this.m_strSelectedFullWeaponList = strValue;
            }
            else if (strVariable.CompareTo("Custom Message") == 0)
            {
                this.m_strDisplay = strValue;
            }
            else if (strVariable.CompareTo("Add or Remove Weapon in List") == 0)
            {
                if (this.m_strSelectedFullWeaponList.Equals(string.Empty) || this.m_strDisplay.Equals(string.Empty))
                {
                    // We don't want to do anything
                }
                else
                {
                    string killTracked = this.m_strSelectedFullWeaponList + "//" + this.m_strDisplay;
                    if (strValue.Equals("Add"))
                    {
                        if (this.m_weaponAndMessages.Contains(killTracked) == false)
                        {
                            this.m_weaponAndMessages.Add(killTracked);
                        }
                    }
                    else if (strValue.Equals("Remove"))
                    {
                        if (this.m_weaponAndMessages.Contains(killTracked) == true)
                        {
                            this.m_weaponAndMessages.Remove(killTracked);
                        }
                    }
                    this.m_weaponAndMessages.RemoveAll(string.IsNullOrEmpty);
                }
            }
            else if (strVariable.CompareTo("Weapon and Message List") == 0)
            {
                this.m_weaponAndMessages = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
        }

        // Account created
        public void OnAccountCreated(string strUsername)
        {

        }

        public void OnAccountDeleted(string strUsername)
        {

        }

        public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges cpPrivs)
        {

        }

        public void OnReceiveProconVariable(string strVariableName, string strValue)
        {

        }

        // Connection
        public void OnConnectionClosed()
        {

        }

        // Player events
        public void OnPlayerJoin(string strSoldierName)
        {

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            CPlayerInfo Killer = kKillerVictimDetails.Killer;
            CPlayerInfo Victim = kKillerVictimDetails.Victim;
            /*Weapon theWeaponUsed = this.GetWeaponDefines()["HG-2"];				// HG-2 --> Hand Grenade	
			this.ExecuteCommand("procon.protected.pluginconsole.write", "Weapon: " + theWeaponUsed.Name);
			string weaponName = this.GetLocalized(theWeaponUsed.Name, String.Format("global.Weapons.{0}", theWeaponUsed.Name.ToLower()));
			this.ExecuteCommand("procon.protected.pluginconsole.write", "Weapon: " + weaponName);*/

            Weapon weaponUsed = this.GetWeaponDefines()[kKillerVictimDetails.DamageType];
            string weaponUsedName = this.GetLocalized(weaponUsed.Name, String.Format("global.Weapons.{0}", kKillerVictimDetails.DamageType.ToLower()));
            bool foundWeapon = false;
            string[] weaponAndMessage = new string[2];
            string strMessage = "";
            foreach (string weapAndMsg in this.m_weaponAndMessages)
            {
                weaponAndMessage = Regex.Split(weapAndMsg, "//");
                if (weaponAndMessage[0].Equals(weaponUsedName))
                {
                    foundWeapon = true;
                    strMessage = weaponAndMessage[1];
                    break;
                }
            }
            if (foundWeapon)
            {
                if (this.m_enYellResponses == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillTracker", "0", "1", "1", "procon.protected.send", "admin.yell", strMessage.Replace("%pk%", Killer.SoldierName).Replace("%wp%", weaponUsedName).Replace("%pv%", Victim.SoldierName), this.m_iDisplayTime.ToString(), "all");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillTracker", "0", "1", "1", "procon.protected.send", "admin.say", strMessage.Replace("%pk%", Killer.SoldierName).Replace("%wp%", weaponUsedName).Replace("%pv%", Victim.SoldierName), "all");
                }
            }
        }



        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {

        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {

        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {

        }

        public void OnLoadingLevel(string strMapFileName)
        {

        }

        #region IPRoConPluginInterface1

        public void OnLevelStarted()
        {

        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {

        }

        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {

        }

        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

        }

        // Global or misc..
        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

        }

        // Login events
        public void OnLogin()
        {

        }

        public void OnLogout()
        {

        }

        public void OnQuit()
        {

        }

        public void OnVersion(string strServerType, string strVersion)
        {

        }

        public void OnHelp(List<string> lstCommands)
        {

        }

        public void OnRunScript(string strScriptFileName)
        {

        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {

        }

        // Query Events
        public void OnServerInfo(CServerInfo csiServerInfo)
        {

        }

        // Communication Events
        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }

        // Level Events
        public void OnRunNextLevel()
        {

        }

        public void OnCurrentLevel(string strCurrentLevel)
        {

        }

        public void OnSetNextLevel(string strNextLevel)
        {

        }

        public void OnRestartLevel()
        {

        }

        // Does not work in R3, never called for now.
        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }


        // Player Kick/List Events
        public void OnPlayerKicked(string strSoldierName, string strReason)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        private int GetPlayerTeamID(string strSoldierName)
        {
            return 0;
        }
        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        // Banning and Banlist Events
        public void OnBanList(List<CBanInfo> lstBans)
        {

        }

        public void OnBanAdded(CBanInfo cbiBan)
        {

        }

        public void OnBanRemoved(CBanInfo cbiUnban)
        {

        }

        public void OnBanListClear()
        {

        }

        public void OnBanListLoad()
        {

        }

        public void OnBanListSave()
        {

        }

        // Reserved Slots Events
        public void OnReservedSlotsConfigFile(string strConfigFilename)
        {

        }

        public void OnReservedSlotsLoad()
        {

        }

        public void OnReservedSlotsSave()
        {

        }

        public void OnReservedSlotsPlayerAdded(string strSoldierName)
        {

        }

        public void OnReservedSlotsPlayerRemoved(string strSoldierName)
        {

        }

        public void OnReservedSlotsCleared()
        {

        }

        public void OnReservedSlotsList(List<string> lstSoldierNames)
        {

        }

        // Maplist Events
        public void OnMaplistConfigFile(string strConfigFilename)
        {

        }

        public void OnMaplistLoad()
        {

        }

        public void OnMaplistSave()
        {

        }

        public void OnMaplistMapAppended(string strMapFileName)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }

        // Vars
        public void OnGamePassword(string strGamePassword)
        {

        }

        public void OnPunkbuster(bool blEnabled)
        {

        }

        public void OnHardcore(bool blEnabled)
        {

        }

        public void OnRanked(bool blEnabled)
        {

        }

        public void OnRankLimit(int iRankLimit)
        {

        }

        public void OnTeamBalance(bool blEnabled)
        {

        }

        public void OnFriendlyFire(bool blEnabled)
        {

        }

        public void OnMaxPlayerLimit(int iMaxPlayerLimit)
        {

        }

        public void OnCurrentPlayerLimit(int iCurrentPlayerLimit)
        {

        }

        public void OnPlayerLimit(int iPlayerLimit)
        {

        }

        public void OnBannerURL(string strURL)
        {

        }

        public void OnServerDescription(string strServerDescription)
        {

        }

        public void OnKillCam(bool blEnabled)
        {

        }

        public void OnMiniMap(bool blEnabled)
        {

        }

        public void OnCrossHair(bool blEnabled)
        {

        }

        public void On3dSpotting(bool blEnabled)
        {

        }

        public void OnMiniMapSpotting(bool blEnabled)
        {

        }

        public void OnThirdPersonVehicleCameras(bool blEnabled)
        {

        }

        #endregion IPRoConPluginInterface1

        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //

        public void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {

        }

        public void OnServerName(string strServerName)
        {

        }

        public void OnTeamKillCountForKick(int iLimit)
        {

        }

        public void OnTeamKillValueIncrease(int iLimit)
        {

        }

        public void OnTeamKillValueDecreasePerSecond(int iLimit)
        {

        }

        public void OnTeamKillValueForKick(int iLimit)
        {

        }

        public void OnIdleTimeout(int iLimit)
        {

        }

        public void OnProfanityFilter(bool isEnabled)
        {

        }

        public void OnEndRound(int iWinningTeamID)
        {

        }

        public void OnRoundOverTeamScores(List<TeamScore> lstTeamScores)
        {

        }

        public void OnRoundOverPlayers(List<string> lstPlayers)
        {

        }

        public void OnRoundOver(int iWinningTeamID)
        {

        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {

        }

        public void OnLevelVariablesList(LevelVariable lvRequestedContext, List<LevelVariable> lstReturnedValues)
        {

        }

        public void OnLevelVariablesEvaluate(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        public void OnLevelVariablesClear(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesSet(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesGet(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        #endregion

        #region IPRoConPluginInterface3

        //
        // IPRoConPluginInterface3
        //
        public void OnAnyMatchRegisteredCommand(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage)
        {

        }

        public void OnRegisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnUnregisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {

        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        #endregion

        #region IPRoConPluginInterface4

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState)
        {

        }

        #endregion

    }
}