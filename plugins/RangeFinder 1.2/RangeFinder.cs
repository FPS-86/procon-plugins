/*  Copyright 2010 [ATRS]Foxinabox (Michael Borden)

    http://www.clanatrs.com

    This file is part of [ATRS]Foxinabox's Plugins for BFBC2 PRoCon.

    [ATRS]Foxinabox's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [ATRS]Foxinabox's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with [ATRS]Foxinabox's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class RangeFinder : PRoConPluginAPI, IPRoConPluginInterface
    {
        private enumBoolYesNo m_ebynPlayerKillNotifications;
        private enumBoolYesNo m_ebynPlayerDeathNotification;

        private int m_iKillDistance;
        private int m_iHeadshotDistance;
        private int m_iWeaponUsed;

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        private string m_strHeadshot;
        private string m_strKill;
        private string m_strMessageNormalToKiller;
        private string m_strMessageHSToKiller;
        private string m_strMessageToVictim;

        public RangeFinder()
        {
            this.m_strMessageNormalToKiller = "<Kill> You Killed %vn% at %kd% meters.";
            this.m_strMessageHSToKiller = "<Headshot> You killed %vn% at %kd% meters.";
            this.m_strMessageToVictim = "<Death> Killed by %kn% from %kd% meters with the %wp%.";
            this.m_ebynPlayerKillNotifications = enumBoolYesNo.Yes;
            this.m_ebynPlayerDeathNotification = enumBoolYesNo.Yes;
        }

        public string GetPluginName()
        {
            return "Range Finder";
        }

        public string GetPluginVersion()
        {
            return "1.2";
        }

        public string GetPluginAuthor()
        {
            return "[ATRS]Foxinabox - http://www.clanatrs.com/ - mailto:foxinabox@clanatrs.com";
        }

        public string GetPluginWebsite()
        {
            return "http://phogue.net/forum/viewtopic.php?f=18&t=1788&p=13487#p13487";
        }

        public string GetPluginDescription()
        {
            return @"
<p>For support or to post comments regarding this plugin please contact [ATRS]Foxinabox.</p>
<p>I am new to coding, and starting by making simple plug-ins. Step-by-step, I am hoping to start creating more complex plug-ins in the near future.</p>
<p>This plugin works with PRoCon and falls under the GNU GPL, please read the included gpl.txt for details. However, if you would like to donate to support the development of PRoCon, click the link below:
<a href=""http://phogue.net"" target=""_new""><img src=""http://i398.photobucket.com/albums/pp65/scoop27585/phoguenetdonate.png"" width=""241"" height=""42"" border=""0"" alt=""PRoCon Donation"" /></a></p>
<p>Toward the right side of the page, there is a location to enter the amount you would like to donate and whether you want the donation to be made public. Your donations are greatly appreciated and will be sent to Phogue (original creator of PRoCon).</p>
<h2>Description</h2>
    <p>Shows the range in a private message of each kill with a customizable message. Also shows the range at which someone killed you from and the weapon used. Admins have the option to turn the kill notifications, and death notification on/off.</p>

<h2>Commands</h2>
	<p>No commands available with this plugin currently.</p>

<h2>Settings</h2>
    <h3>Miscellaneous</h3>
        <blockquote><h4>Custom Message</h4>The message you want to include with the kill.</blockquote>
        	<ul>             
                <li>Include killer: Use ""%kn%""</li>  
                <li>Include victim: Use ""%vn%""</li>   
                <li>Include distance: Use ""%kd%""</li>  
                <li>(Death Message Only) Include killer's weapon:  Use ""%wp%""</li>
            </ul>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnPlayerKilled", "OnPluginEnable", "OnPluginDisable");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bRange Finder Plugin ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bRange Finder Plugin ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Show player kill notifications?", typeof(enumBoolYesNo), this.m_ebynPlayerKillNotifications));
            if (m_ebynPlayerKillNotifications == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Custom Message Normal Kill", typeof(string), this.m_strMessageNormalToKiller));
                lstReturn.Add(new CPluginVariable("Custom Message Headshot", typeof(string), this.m_strMessageHSToKiller));
            }

            lstReturn.Add(new CPluginVariable("Show player death notification?", typeof(enumBoolYesNo), this.m_ebynPlayerDeathNotification));
            if (m_ebynPlayerDeathNotification == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Custom Message Death", typeof(string), this.m_strMessageToVictim));
            }
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Show player kill notifications?", typeof(enumBoolYesNo), this.m_ebynPlayerKillNotifications));
            lstReturn.Add(new CPluginVariable("Show player death notification?", typeof(enumBoolYesNo), this.m_ebynPlayerDeathNotification));

            return GetDisplayPluginVariables();
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Show player kill notifications?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPlayerKillNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Show player death notification?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPlayerDeathNotification = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Custom Message Normal Kill") == 0)
            {
                this.m_strMessageNormalToKiller = strValue;
            }
            else if (strVariable.CompareTo("Custom Message Headshot") == 0)
            {
                this.m_strMessageHSToKiller = strValue;
            }
            else if (strVariable.CompareTo("Custom Message Death") == 0)
            {
                this.m_strMessageToVictim = strValue;
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

            m_iHeadshotDistance = 0;
            m_iKillDistance = 0;

            Weapon weaponUsed = this.GetWeaponDefines()[kKillerVictimDetails.DamageType];
            string weaponUsedName = this.GetLocalized(weaponUsed.Name, String.Format("global.Weapons.{0}", kKillerVictimDetails.DamageType.ToLower()));

            if (kKillerVictimDetails.Headshot)
            {
                m_iHeadshotDistance = System.Convert.ToInt16(kKillerVictimDetails.Distance);
                string strMSG = this.m_strMessageHSToKiller;
                strMSG = strMSG.Replace("%vn%", Victim.SoldierName);
                strMSG = strMSG.Replace("%kd%", m_iHeadshotDistance.ToString());
                string strMSG2 = this.m_strMessageToVictim;
                strMSG2 = strMSG2.Replace("%kn%", Killer.SoldierName);
                strMSG2 = strMSG2.Replace("%kd%", m_iHeadshotDistance.ToString());
                strMSG2 = strMSG2.Replace("%wp%", weaponUsedName.ToString());
                this.ExecuteCommand("procon.protected.send", "admin.say", strMSG, "player", Killer.SoldierName);
                this.ExecuteCommand("procon.protected.send", "admin.say", strMSG2, "player", Victim.SoldierName);
            }
            else
            {
                m_iKillDistance = System.Convert.ToInt16(kKillerVictimDetails.Distance);
                string strMSG = this.m_strMessageNormalToKiller;
                strMSG = strMSG.Replace("%vn%", Victim.SoldierName);
                strMSG = strMSG.Replace("%kd%", m_iKillDistance.ToString());
                string strMSG2 = this.m_strMessageToVictim;
                strMSG2 = strMSG2.Replace("%kn%", Killer.SoldierName);
                strMSG2 = strMSG2.Replace("%kd%", m_iKillDistance.ToString());
                strMSG2 = strMSG2.Replace("%wp%", weaponUsedName.ToString());
                this.ExecuteCommand("procon.protected.send", "admin.say", strMSG, "player", Killer.SoldierName);
                this.ExecuteCommand("procon.protected.send", "admin.say", strMSG2, "player", Victim.SoldierName);
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

