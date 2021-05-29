/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of PRoCon Frostbite.

    PRoCon Frostbite is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    PRoCon Frostbite is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with PRoCon Frostbite.  If not, see <http://www.gnu.org/licenses/>.
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
    public class BasicMapZoneActions : PRoConPluginAPI, IPRoConPluginInterface
    {

        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo;
        private Dictionary<string, CPlayerInfo> m_dicPlayerInfo;

        private string m_strCurrentMapFileName;

        private string m_strEfaeProtectUsArmyKillMessage;
        private string m_strEfaeProtectRussianArmyKillMessage;
        private string m_strEfaeProtectNVArmyKillMessage;
        private string m_strEfaeProtectAttackersKillMessage;
        private string m_strEfaeAntiCamperSniperKillMessage;

        private float m_flMinimumTrespassError;
        private int m_iMaximumDistanceForBaseCamping;

        public BasicMapZoneActions()
        {
            this.m_strEfaeProtectUsArmyKillMessage = "[Automatic] You have been killed for killing %vn% within a protected zone.";
            this.m_strEfaeProtectRussianArmyKillMessage = "[Automatic] You have been killed for killing %vn% within a protected zone.";
            this.m_strEfaeProtectNVArmyKillMessage = "[Automatic] You have been killed for killing %vn% within a protected zone.";
            this.m_strEfaeProtectAttackersKillMessage = "[Automatic] You have been killed for killing %vn% within a protected zone.";
            this.m_strEfaeAntiCamperSniperKillMessage = "[Automatic] You have been killed for camping it up with a sniper rifle.  ATTACK DAMN YOU!";

            this.m_flMinimumTrespassError = 0.75F;

            this.m_iMaximumDistanceForBaseCamping = 80;

            this.m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
            this.m_dicPlayerInfo = new Dictionary<string, CPlayerInfo>();
        }

        public string GetPluginName()
        {
            return "Basic Mapzone Actions";
        }

        public string GetPluginVersion()
        {
            return "1.1.0.1";
        }

        public string GetPluginAuthor()
        {
            return "Phogue";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        // A note to plugin authors: DO NOT change how a tag works, instead make a whole new tag.
        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
    <p>Provides some very basic tags for base protections with map zones.  This plugin is provided as an example to other plugin authors, expect more complex tags at a later date.</p>

<h2>Settings</h2>
    <h3>Detections</h3>
        <blockquote><h4>Minimum trespass error</h4>How much of the circle displayed on the battlemap must be inside of the map zone.  The bfbc2 server does not send exact coordinates of players, but instead has +/- 10m to each coordinate.  Think of this as a ""How likely was the player actually in the zone"" variable</blockquote>
        <blockquote><h4>Maximum distance for base camping</h4>How far (in meters) must a player be from the opposite player to not be penalized for base camping.  Setting to 50m would mean a player is not base-camping if they are 50m's outside of the base.  This would mean players can still be killed in the base, but this would give them a lot more breathing room to push an offensive.  Set to a really high number if you would prefer zero-tolerance on base camping</blockquote>

<h2>Tags</h2>
    <h3>Base Protections</h3>
        <blockquote><h4>EFAE_PROTECT_U.SARMY</h4>Kills Russian soldiers if they killed a U.S soldier within the zone</blockquote>
        <blockquote><h4>EFAE_PROTECT_RUSSIANARMY</h4>Kills U.S soldiers if they killed a russian soldier within the zone</blockquote>
        <blockquote><h4>EFAE_PROTECT_NVA</h4>Kills U.S soldiers if they killed a NVA soldier within the zone</blockquote>
        <blockquote><h4>EFAE_PROTECT_ATTACKERS</h4>Kills defenders if they killed an attacking soldier within the zone</blockquote>
    <h3>Location/Kit Restriction</h3>
        <blockquote><h4>EFAE_ANTICAMPER_SNIPER</h4>Kills a player for sniping within a no-sniper zone (prevent half the freaking team from camping the hill on Port Valdez.. ATTACK YOU F*$%@RS)</blockquote>

<h2>Additional Information</h2>
    <ul>
        <li>EFAE - Eye for an Eye, kills the killer</li>
    </ul>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {

        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBasic Mapzone Tags ^2Enabled!");

            this.RegisterZoneTags("EFAE_PROTECT_U.SARMY", "EFAE_PROTECT_RUSSIANARMY", "EFAE_PROTECT_NVA", "EFAE_PROTECT_ATTACKERS", "EFAE_ANTICAMPER_SNIPER");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBasic Mapzone Tags ^1Disabled =(");

            this.UnregisterZoneTags("EFAE_PROTECT_U.SARMY", "EFAE_PROTECT_RUSSIANARMY", "EFAE_PROTECT_NVA", "EFAE_PROTECT_ATTACKERS", "EFAE_ANTICAMPER_SNIPER");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("EFAE_PROTECT_U.SARMY|EFAE U.S army protection kill message", typeof(string), this.m_strEfaeProtectUsArmyKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE_PROTECT_RUSSIANARMY|EFAE Russian army protection kill message", typeof(string), this.m_strEfaeProtectRussianArmyKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE_PROTECT_NVA|EFAE NVA army protection kill message", typeof(string), this.m_strEfaeProtectNVArmyKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE_PROTECT_ATTACKERS|EFAE Attackers protection kill message", typeof(string), this.m_strEfaeProtectAttackersKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE_ANTICAMPER_SNIPER|EFAE Sniper anti-camper kill message", typeof(string), this.m_strEfaeAntiCamperSniperKillMessage));

            lstReturn.Add(new CPluginVariable("Detection|Minimum trespass error (10% - 100%)", typeof(int), this.m_flMinimumTrespassError * 100));

            lstReturn.Add(new CPluginVariable("Detection|Maximum distance for base camping (meters)", typeof(int), this.m_iMaximumDistanceForBaseCamping));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("EFAE U.S army protection kill message", typeof(string), this.m_strEfaeProtectUsArmyKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE Russian army protection kill message", typeof(string), this.m_strEfaeProtectRussianArmyKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE NFA army protection kill message", typeof(string), this.m_strEfaeProtectNVArmyKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE Attackers protection kill message", typeof(string), this.m_strEfaeProtectAttackersKillMessage));
            lstReturn.Add(new CPluginVariable("EFAE Sniper anti-camper kill message", typeof(string), this.m_strEfaeAntiCamperSniperKillMessage));

            lstReturn.Add(new CPluginVariable("Minimum trespass error (10% - 100%)", typeof(int), this.m_flMinimumTrespassError * 100));

            lstReturn.Add(new CPluginVariable("Maximum distance for base camping (meters)", typeof(int), this.m_iMaximumDistanceForBaseCamping));

            return lstReturn;
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {
            float flTrespassError = 0.75F;
            int iMaximumDistanceForBaseCamping = 100;

            if (strVariable.CompareTo("EFAE U.S army protection kill message") == 0)
            {
                this.m_strEfaeProtectUsArmyKillMessage = strValue;
            }
            else if (strVariable.CompareTo("EFAE Russian army protection kill message") == 0)
            {
                this.m_strEfaeProtectRussianArmyKillMessage = strValue;
            }
            else if (strVariable.CompareTo("EFAE NVA army protection kill message") == 0)
            {
                this.m_strEfaeProtectNVArmyKillMessage = strValue;
            }
            else if (strVariable.CompareTo("EFAE Attackers protection kill message") == 0)
            {
                this.m_strEfaeProtectAttackersKillMessage = strValue;
            }
            else if (strVariable.CompareTo("EFAE Sniper anti-camper kill message") == 0)
            {
                this.m_strEfaeAntiCamperSniperKillMessage = strValue;
            }
            else if (strVariable.CompareTo("Minimum trespass error (10% - 100%)") == 0 && float.TryParse(strValue, out flTrespassError) == true)
            {
                flTrespassError /= 100;

                if (flTrespassError > 1.0F)
                {
                    this.m_flMinimumTrespassError = 1.0F;
                }
                else if (flTrespassError < 0.1F)
                {
                    this.m_flMinimumTrespassError = 0.1F;
                }
                else
                {
                    this.m_flMinimumTrespassError = flTrespassError;
                }
            }
            else if (strVariable.CompareTo("Maximum distance for base camping (meters)") == 0 && int.TryParse(strValue, out iMaximumDistanceForBaseCamping) == true)
            {

                if (iMaximumDistanceForBaseCamping < 0)
                {
                    this.m_iMaximumDistanceForBaseCamping = 0;
                }
                else
                {
                    this.m_iMaximumDistanceForBaseCamping = iMaximumDistanceForBaseCamping;
                }
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

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == false)
            {
                this.m_dicPlayerInfo.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
            }

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.m_dicPbInfo.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPbInfo.Remove(strSoldierName);
            }

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayerInfo.Remove(strSoldierName);
            }
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

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

            if (cpbiPlayer != null)
            {
                if (this.m_dicPbInfo.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.m_dicPbInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                }
                else
                {
                    this.m_dicPbInfo[cpbiPlayer.SoldierName] = cpbiPlayer;
                }
            }
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
            this.m_strCurrentMapFileName = csiServerInfo.Map;
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

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.m_dicPlayerInfo.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        this.m_dicPlayerInfo[cpiPlayer.SoldierName] = cpiPlayer;
                    }
                    else
                    {
                        this.m_dicPlayerInfo.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }
                }
            }

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

        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //
        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {

        }

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
            this.m_strCurrentMapFileName = mapFileName;
        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        #endregion

        #region IPRoConPluginInterface4

        private CMap GetMapByFilename(string mapFileName)
        {

            List<CMap> lstMaps = this.GetMapDefines();

            CMap returnMap = null;

            if (lstMaps != null)
            {
                foreach (CMap map in lstMaps)
                {
                    if (String.Compare(map.FileName, mapFileName, true) == 0)
                    {
                        returnMap = map;
                        break;
                    }
                }
            }

            return returnMap;
        }

        private string GetTeamLocalizationKeyByTeamId(int teamId)
        {

            string strReturnLocalizationKey = String.Empty;

            CMap map = this.GetMapByFilename(this.m_strCurrentMapFileName);

            if (map != null)
            {

                foreach (CTeamName teamName in map.TeamNames)
                {
                    if (teamName.TeamID == teamId)
                    {
                        strReturnLocalizationKey = teamName.LocalizationKey;
                        break;
                    }
                }
            }

            return strReturnLocalizationKey;
        }

        private DamageTypes GetWeaponDamageType(string weaponName)
        {
            WeaponDictionary weapons = this.GetWeaponDefines();
            DamageTypes returnDamageType = DamageTypes.None;

            if (weapons.Contains(weaponName) == true)
            {
                returnDamageType = weapons[weaponName].Damage;
            }

            return returnDamageType;
        }

        private void KillPlayerWithMessage(string soldierName, string message)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", soldierName);
            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", soldierName);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBasic Mapzone: action taken.");
        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState)
        {

            if (flTresspassPercentage >= this.m_flMinimumTrespassError)
            {

                if (trespassState is Kill)
                {

                    Kill trespassKill = (Kill)trespassState;

                    // If the trespasser died within the zone (the killer does not need to be inside the zone)
                    if (action == ZoneAction.Death)
                    {

                        if (trespassKill.IsSuicide == false)
                        {

                            // If the player was within the limit for base camping.
                            // AND the player is not on the same team.
                            if (trespassKill.Distance <= this.m_iMaximumDistanceForBaseCamping && trespassKill.Killer.TeamID != trespassKill.Victim.TeamID)
                            {

                                if (sender.Tags.Contains("EFAE_PROTECT_U.SARMY") == true)
                                {

                                    // If the player was on U.S side
                                    // References: PRoCon.Core.CMap, PRoCon.Core.CTeamName
                                    if (String.Compare(this.GetTeamLocalizationKeyByTeamId(cpiSoldier.TeamID), "global.conquest.us", true) == 0)
                                    {
                                        this.KillPlayerWithMessage(trespassKill.Killer.SoldierName, this.m_strEfaeProtectUsArmyKillMessage.Replace("%kn%", trespassKill.Killer.SoldierName).Replace("%vn%", trespassKill.Victim.SoldierName));
                                    }
                                }

                                if (sender.Tags.Contains("EFAE_PROTECT_RUSSIANARMY") == true)
                                {
                                    if (String.Compare(this.GetTeamLocalizationKeyByTeamId(cpiSoldier.TeamID), "global.conquest.ru", true) == 0)
                                    {
                                        this.KillPlayerWithMessage(trespassKill.Killer.SoldierName, this.m_strEfaeProtectRussianArmyKillMessage.Replace("%kn%", trespassKill.Killer.SoldierName).Replace("%vn%", trespassKill.Victim.SoldierName));
                                    }
                                }

                                if (sender.Tags.Contains("EFAE_PROTECT_NVA") == true)
                                {
                                    if (String.Compare(this.GetTeamLocalizationKeyByTeamId(cpiSoldier.TeamID), "global.conquest.nva", true) == 0)
                                    {
                                        this.KillPlayerWithMessage(trespassKill.Killer.SoldierName, this.m_strEfaeProtectNVArmyKillMessage.Replace("%kn%", trespassKill.Killer.SoldierName).Replace("%vn%", trespassKill.Victim.SoldierName));
                                    }
                                }

                                if (sender.Tags.Contains("EFAE_PROTECT_ATTACKERS") == true)
                                {
                                    if (String.Compare(this.GetTeamLocalizationKeyByTeamId(cpiSoldier.TeamID), "global.rush.attackers", true) == 0)
                                    {
                                        this.KillPlayerWithMessage(trespassKill.Killer.SoldierName, this.m_strEfaeProtectAttackersKillMessage.Replace("%kn%", trespassKill.Killer.SoldierName).Replace("%vn%", trespassKill.Victim.SoldierName));
                                    }
                                }
                            }
                        }
                    }
                    // If the trespasser killed another player while inside the zone (the victim does not need to be inside the zone)
                    else if (action == ZoneAction.Kill)
                    {

                        if (sender.Tags.Contains("EFAE_ANTICAMPER_SNIPER") == true)
                        {

                            // If they used a sniper rifle to kill.
                            // References: PRoCon.Core.Players.Items.Weapon, PRoCon.Core.Players.Items.DamageTypes, PRoCon.Core.Players.Items.WeaponDictionary
                            if (this.GetWeaponDamageType(trespassKill.DamageType) == DamageTypes.SniperRifle)
                            {
                                this.KillPlayerWithMessage(trespassKill.Killer.SoldierName, this.m_strEfaeAntiCamperSniperKillMessage.Replace("%kn%", trespassKill.Killer.SoldierName).Replace("%vn%", trespassKill.Victim.SoldierName));
                            }
                        }
                    }
                }
            }
        }

        #endregion

    }
}