/*  Copyright 2010 Imisnew2

    http://www.TeamPlayerGaming.com/members/Imisnew2.html

    This file is part of Imisnew2's Simple Scramble Plugin for BFBC2 PRoCon.

    Imisnew2's Simple Scramble Plugin for BFBC2 PRoCon is free software:
    you can redistribute it and/or modify it under the terms of the GNU
    General Public License as published by the Free Software Foundation,
    either version 3 of the License, or (at your option) any later version.

    Imisnew2's Simple Scramble Plugin for BFBC2 PRoCon is distributed in
    the hope that it will be useful, but WITHOUT ANY WARRANTY; without even
    the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Imisnew2's Simple Scramble Plugin for BFBC2 PRoCon.
    If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin;


namespace PRoConEvents
{
    /// <summary>
    /// The plugin scrambles the teams during a round/map change after the 
    /// specified number of rounds/maps have passed.  The plugin can also be 
    /// forced to scramble teams after the current round is over by using 
    /// the @sscramble command.
    /// </summary


    /// class SimpleScramble : CPRoConMarshalByRefObject, IPRoConPluginInterface
    public class SimpleScramble : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Plugin Information

        /// <summary>Allows PRoCon to get the name of this plugin.</summary>
        /// <returns>The name of this plugin.</returns>
        public string GetPluginName() { return "Simple Scramble"; }

        /// <summary>Allows PRoCon to get the version of this plugin.</summary>
        /// <returns>The version of this plugin.</returns>
        public string GetPluginVersion() { return "0.3"; }

        /// <summary>Allows PRoCon to get the author's name of this plugin.</summary>
        /// <returns>The author's name of this plugin.</returns>
        public string GetPluginAuthor() { return "Imisnew2"; }

        /// <summary>Allows PRoCon to get the website for this plugin.</summary>
        /// <returns>The website for this plugin.</returns>
        public string GetPluginWebsite() { return "www.TeamPlayerGaming.com/members/Imisnew2.html"; }

        /// <summary>Allows ProCon to get a description of this plugin.</summary>
        /// <returns>The description of this plugin.</returnsd>
        public string GetPluginDescription()
        {
            return "<h2>Description</h2>" +
                       "<p>Simple Scramble simply scrambles the teams on your Bad Company 2 server." +
                          "<br/>" +
                          "The plugin scrambles the teams during a round/map change after the specified number of rounds/maps have passed.  The plugin can also be forced to scramble teams instantly, after the current round is over, or after the current map is over by using the <i>sscramble</i> command.<br/>" +
                          "<br/>" +
                          "To use the <i>sscramble</i> command, see the following:<br/>" +
                          "[@!/]sscramble &lt;now/round/map&gt;<br/>" +
                          "Examples:<br/>" +
                          "To scramble the teams instantly, use: <i>@sscramble</i> or <i>@sscramble now</i>.<br/>" +
                          "To scramble the teams when the round ends, use: <i>@sscramble round</i>.<br/>" +
                          "To scramble the teams when the map ends, use: <i>@sscramble map</i>.<br/>" +
                          "To cancel a scramble on round/map end, use: <i>@sscramble cancel</i>.<br/>" +
                          "<br/>" +
                          "To vote in game for scrambling type <i>!scramble</i> command into chat! <br/>" +

                     "<h2>Settings</h2>" +
                       "<h3>Scramble Settings</h3>" +

                         "<h4>Admins</h4>" +
                         "<blockquote style=\"margin-left: 0px; margin-right:0px; margin-top:0px;\">This is a list of player's names whom are allowed to use the @sscramble command." +
                           "<br/>" +
                           "<br/><u>Values</u>:" +
                           "<br/><i>Array</i>: Admin1, Admin2, Bob, John, Dillan.</blockquote>" +

                         "<h4>Scramble Type</h4>" +
                         "<blockquote style=\"margin-left: 0px; margin-right:0px; margin-top:0px;\">Controls when the plugin will scramble the teams, either after a number of rounds or a number of maps." +
                           "<br/>" +
                           "<br/><u>Values</u>:" +
                           "<br/><i>Yes</i>: Scramble after a set number of MAPS." +
                           "<br/><i>No</i>: Scramble after a set number of ROUNDS.</blockquote>" +

                         "<h4>Scramble After</h4>" +
                         "<blockquote style=\"margin-left: 0px; margin-right:0px; margin-top:0px;\">Controls how many rounds/maps must past before the plugin will scramble the teams." +
                           "<br/>" +
                           "<br/><u>Values</u>:" +
                           "<br/><i>Number</i>: Some numerical value above 0.</blockquote>" +

                         "<h4>Scramble Voting</h4>" +
                         "<blockquote style=\"margin-left: 0px; margin-right:0px; margin-top:0px;\">Enabled, then Players can vote for Scrambling. Typing <b><i>!scramble</i></b> into chat, will start the Voting." +
                          "<br/>" +
                          "<br/><u>Values</u>:" +
                          "<br/><i>Number of needed Votes</i>: Some numerical value above 0.</blockquote>";
        }

        #endregion
        #region Plugin PRoCon Variables

        #region UI Variables

        // Scramble Settings
        enumBoolYesNo uiScrMap = enumBoolYesNo.No;
        Int32 uiScrMapNum = 1;
        enumBoolYesNo uiScrRound = enumBoolYesNo.No;
        Int32 uiScrRoundNum = 2;
        String[] uiScrAdmins = new String[] { "Imisnew2", "Admin2", "Admin3", "Admin4" };
        enumBoolYesNo uiScrVoting = enumBoolYesNo.No;
        Int32 uiScrVotesNeeded = 3;


        #endregion

        /// <summary>Allows PRoCon to figure out what fields to display to the user.</summary>
        /// <returns>A list of variables to display.</returns>
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            // Scramble Settings
            lstReturn.Add(new CPluginVariable("Scramble Settings|Admins", typeof(String[]), uiScrAdmins));

            lstReturn.Add(new CPluginVariable("Scramble Settings|On Map Change", typeof(enumBoolYesNo), uiScrMap));
            if (uiScrMap == enumBoolYesNo.Yes)
                lstReturn.Add(new CPluginVariable("Scramble Settings|After ... Maps", typeof(Int32), uiScrMapNum));

            lstReturn.Add(new CPluginVariable("Scramble Settings|On Round Change", typeof(enumBoolYesNo), uiScrRound));
            if (uiScrRound == enumBoolYesNo.Yes)
                lstReturn.Add(new CPluginVariable("Scramble Settings|After ... Rounds", typeof(Int32), uiScrRoundNum));

            lstReturn.Add(new CPluginVariable("Scramble Settings|Allow Scramble Voting", typeof(enumBoolYesNo), uiScrVoting));
            if (uiScrVoting == enumBoolYesNo.Yes)
                lstReturn.Add(new CPluginVariable("Scramble Settings|Number of Votes needed for Scrambling", typeof(Int32), uiScrVotesNeeded));


            return lstReturn;
        }

        /// <summary>Allows PRoCon to save the variables for persistence across sessions.</summary>
        /// <returns>A list of variables to save.</returns>
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            // Scramble Settings
            lstReturn.Add(new CPluginVariable("Admins", typeof(String[]), uiScrAdmins));
            lstReturn.Add(new CPluginVariable("On Map Change", typeof(enumBoolYesNo), uiScrMap));
            lstReturn.Add(new CPluginVariable("After ... Maps", typeof(Int32), uiScrMapNum));
            lstReturn.Add(new CPluginVariable("On Round Change", typeof(enumBoolYesNo), uiScrRound));
            lstReturn.Add(new CPluginVariable("After ... Rounds", typeof(Int32), uiScrRoundNum));
            lstReturn.Add(new CPluginVariable("Allow Scramble Voting", typeof(enumBoolYesNo), uiScrVoting));
            lstReturn.Add(new CPluginVariable("Number of Votes needed for Scrambling", typeof(Int32), uiScrVotesNeeded));

            return lstReturn;
        }

        /// <summary>Allows PRoCon to load the variables from a previous session.  Also allows the user to change the variable as well.</summary>
        /// <param name="strVariable">The name of the variable we're loading.</param>
        /// <param name="strValue">The value of the variable we're loading.</param>
        public void SetPluginVariable(string strVariable, string strValue)
        {
            // Scramble Settings
            Int32 intOut = 0;

            if (strVariable == "Admins")
                scrAdmins = uiScrAdmins = CPluginVariable.DecodeStringArray(strValue);
            else if (strVariable == "On Map Change" && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                scrMap = (uiScrMap = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue)) == enumBoolYesNo.Yes;
                scrMapCounter = 0;
            }
            else if (strVariable == "After ... Maps" && Int32.TryParse(strValue, out intOut))
                scrMapNum = uiScrMapNum = (intOut > 0) ? intOut : 1;
            if (strVariable == "On Round Change" && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                scrRound = (uiScrRound = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue)) == enumBoolYesNo.Yes;
                scrRoundCounter = 0;
            }
            else if (strVariable == "After ... Rounds" && Int32.TryParse(strValue, out intOut))
                scrRoundNum = uiScrRoundNum = (intOut > 0) ? intOut : 1;
            if (strVariable == "Allow Scramble Voting" && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                scrVoting = (uiScrVoting = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue)) == enumBoolYesNo.Yes;
            }
            else if (strVariable == "Number of Votes needed for Scrambling" && Int32.TryParse(strValue, out intOut))
                scrVotesNeeded = uiScrVotesNeeded = (intOut > 0) ? intOut : 1;
        }

        #endregion
        #region Plugin Variables

        // --- Visual Settings
        // [0] Whether to scramble after the specified number of maps.
        // [1] Whether to scramble after the specified number of rounds.
        // [2] The number of maps that must pass before the plugin scrambles the teams.
        // [3] The number of rounds that must pass before the plugin scrambles the teams.
        // [4] The players allowed to use the scramble command.
        Boolean scrMap;
        Boolean scrRound;
        Int32 scrMapNum;
        Int32 scrRoundNum;
        String[] scrAdmins;
        List<string> scrVoters = new List<string>();
        Boolean scrVoting;
        Int32 scrActualVotes;
        string scrStrActualVotes;
        Int32 scrVotesNeeded;
        string scrStrVotesNeeded;
        // --- Behind the scenes Settings
        // [0] Random number generator used to select the players randomly.
        // [1] Whether it was designated to scramble the players on round end.
        // [2] Whether it was designated to scramble the players on map end.
        // [3] The number of maps that have passed.
        // [4] The number of rounds that have passed.
        // [5] The players on the server.
        Random scrRandom;
        Boolean scrOnRoundEnd;
        Boolean scrOnMapEnd;
        Int32 scrMapCounter;
        Int32 scrRoundCounter;
        List<CPlayerInfo> scrPlayerInfo;
        String scrCurrentMap;

        #endregion
        #region Plugin Loaded/Enable/Disable

        /// <summary>Is called when the plugin is successfully loaded.</summary>
        public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
        {
            // Register Events.
            this.RegisterEvents("OnLoadingLevel", /*"OnRoundOver",*/ "OnListPlayers", "OnGlobalChat", "OnTeamChat", "OnSquadChat");
        }

        /// <summary>Is called when the plugin is turned on.</summary>
        public void OnPluginEnable()
        {
            scrMapCounter = 0;
            scrRoundCounter = 0;
            scrPlayerInfo.Clear();
            scrVoters.Clear();
            scrActualVotes = 0;
            forceBcListing();

            consoleWrite("Enabled!");
        }

        /// <summary>Is called when the plugin is turned off.</summary>
        public void OnPluginDisable()
        {
            consoleWrite("Disabled. =(");
        }

        #endregion
        #region Plugin Methods

        public void setPluginState(Boolean state)
        { this.ExecuteCommand("procon.protected.plugins.enable", "SimpleScramble", state.ToString()); }
        public void consoleWrite(String message)
        { this.ExecuteCommand("procon.protected.pluginconsole.write", "Simple Scramble: " + message); }
        public void forceBcListing()
        { this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all"); }
        public void movePlayer(String player, Int32 teamId)
        { this.ExecuteCommand("procon.protected.send", "admin.movePlayer", player, teamId.ToString(), "0", "true"); }
        public void say(String message)
        { this.ExecuteCommand("procon.protected.send", "admin.say", message, "all"); }
        public void yell(String message)
        { this.ExecuteCommand("procon.protected.send", "admin.yell", message, "6000", "all"); }

        #endregion
        #region Plugin Events

        /// <summary>Is called when the next level is being loaded.</summary>
        /// <param name="strMapFileName">The name of the level being loaded.</param>
        public void OnLoadingLevel(String strMapFileName, Int32 currentRound, Int32 maxRounds)
        {
            Boolean scramble = false;
            scrVoters.Clear();
            scrActualVotes = 0;


            // Set/Check Round-Based stuff.
            scrRoundCounter++;
            if (scrOnRoundEnd)
            {
                scramble = true;
                scrOnRoundEnd = false;
            }
            // Set/Check Map-Based stuff.
            if (scrCurrentMap != strMapFileName)
            {
                scrMapCounter++;
                scrCurrentMap = strMapFileName;
                if (scrOnMapEnd)
                {
                    scramble = true;
                    scrOnMapEnd = false;
                }
            }


            // Check 'After X Maps' stuff.
            if (scrMapCounter >= scrMapNum)
            {
                if (scrRound)
                    scramble = true;
                scrMapCounter = 0;
            }
            // Check 'After X Rounds' stuff.
            if (scrRoundCounter >= scrRoundNum)
            {
                if (scrMap)
                    scramble = true;
                scrRoundCounter = 0;
            }


            // Scramble teams if necessary.
            if (scramble)
                scrambleTeams();
        }

        /// <summary>Is called when a player list is received.</summary>
        /// <param name="lstPlayers">The list of players.</param>
        /// <param name="cpsSubset">The subset the list of players is of.</param>
        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            scrPlayerInfo = lstPlayers;
        }

        /// <summary>Receives all chat messages sent to everyone.</summary>
        /// <param name="strSpeaker">The person who chatted.</param>
        /// <param name="strMessage">The message they sent.</param>
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            scrStrVotesNeeded = scrVotesNeeded.ToString();

            if (scrVoting == true && strMessage == "!scramble")
            {
                if (scrVoters.Contains(strSpeaker) == false)
                {
                    scrActualVotes = scrActualVotes + 1;
                    scrStrActualVotes = scrActualVotes.ToString();
                    scrVoters.Add(strSpeaker);
                    say(strSpeaker + " voted for scrambling teams, type  !scramble  to vote too.");
                    say(scrStrVotesNeeded + " votes needed & " + scrStrActualVotes + " players voted so far.");
                    yell(strSpeaker + " voted for scrambling teams!");

                    if (scrVoters.Count >= scrVotesNeeded)
                    {
                        scrOnMapEnd = false;
                        scrOnRoundEnd = true;
                        say("The Teams will be scrambled when this round ends.");
                        yell("Scrambling Teams when this round ends!!!");
                    }
                }
                else
                {
                    say(strSpeaker + ", you already voted for scrambling teams.");
                    say(scrStrVotesNeeded + " votes needed & " + scrStrActualVotes + " players voted so far.");
                }
            }

            if (isAdmin(strSpeaker) && isScrambleCommand(strMessage))
                switch (getScrambleParameter(strMessage))
                {
                    case null:
                    case "NOW":
                        scrMapCounter = 0;
                        scrRoundCounter = 0;
                        scrambleTeams();
                        break;

                    case "ROUND":
                        scrOnMapEnd = false;
                        scrOnRoundEnd = true;
                        consoleWrite(strSpeaker + " Set Scramble On Round End.");
                        say("The Teams will be scrambled when this round ends.");
                        break;

                    case "MAP":
                        scrOnRoundEnd = false;
                        scrOnMapEnd = true;
                        consoleWrite(strSpeaker + " Set Scramble On Map End.");
                        say("The Teams will be scrambled when this map ends.");
                        break;

                    case "CANCEL":
                        if (scrOnRoundEnd)
                        {
                            consoleWrite(strSpeaker + " Canceled Scramble On Round End.");
                            say("The Teams will no longer be scrambled when this round ends.");
                        }
                        if (scrOnMapEnd)
                        {
                            consoleWrite(strSpeaker + " Canceled Scramble On Map End.");
                            say("The Teams will no longer be scrambled when this map ends.");
                        }
                        scrOnRoundEnd = false;
                        scrOnMapEnd = false;
                        break;

                    default:
                        consoleWrite("Invalid Scramble Command.");
                        say("Invalid Scramble Command.");
                        break;
                }
        }

        /// <summary>Receives all chat messages sent to a specific team.</summary>
        /// <param name="strSpeaker">The person who chatted.</param>
        /// <param name="strMessage">The message they sent.</param>
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            OnGlobalChat(strSpeaker, strMessage);
        }

        /// <summary>Receives all chat messages sent to a specific squad.</summary>
        /// <param name="strSpeaker">The person who chatted.</param>
        /// <param name="strMessage">The message they sent.</param>
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            OnGlobalChat(strSpeaker, strMessage);
        }

        #endregion

        /// <summary>Constructor.</summary>
        public SimpleScramble()
        {
            scrMap = uiScrMap == enumBoolYesNo.Yes;
            scrRound = uiScrRound == enumBoolYesNo.Yes;
            scrMapNum = uiScrMapNum;
            scrRoundNum = uiScrRoundNum;
            scrAdmins = uiScrAdmins;

            scrRandom = new Random();
            scrOnMapEnd = false;
            scrOnRoundEnd = false;
            scrMapCounter = 0;
            scrRoundCounter = 0;
            scrCurrentMap = "None";
        }

        /// <summary>Checks to see if the name specified is an admin.</summary>
        /// <param name="name">The name.</param>
        public Boolean isAdmin(String name)
        {
            foreach (String admin in scrAdmins)
                if (admin == name)
                    return true;
            return false;
        }

        /// <summary>Checks to see if the message specified is a scramble command.</summary>
        /// <param name="name">The message.</param>
        public Boolean isScrambleCommand(String message)
        {
            return Regex.IsMatch(message, @"^\s*[@/!]sscramble", RegexOptions.IgnoreCase);
        }

        /// <summary>Gets the next word after the scramble command.</summary>
        /// <param name="message">The scramble message.</param>
        public String getScrambleParameter(String message)
        {
            Match match = Regex.Match(message, @"^\s*[@/!]sscramble\s*(.*?)\s*$", RegexOptions.IgnoreCase);
            if (match.Groups.Count > 1 && match.Groups[1].Captures.Count > 0)
                return match.Groups[1].Captures[0].Value.ToUpper();
            return null;
        }

        /// <summary>Scrambles the teams if the requirements are met.</summary>
        public void scrambleTeams()
        {
            // Exit if requirements aren't met.
            if (scrPlayerInfo == null)
                return;
            consoleWrite("Scrambling the Teams.");
            say("Scrambling the Teams.");

            // Pool the players.
            ///consoleWrite("Pooling the players.");
            List<CPlayerInfo> playerPool = new List<CPlayerInfo>();
            playerPool.AddRange(scrPlayerInfo);

            // Move Players to Team 0.
            foreach (CPlayerInfo player in playerPool)
            {
                movePlayer(player.SoldierName, 0);
                ///consoleWrite(String.Format("Moving Player {0} to team 0", player.SoldierName));
            }

            // Divide the players up into teams.
            Int32 playerIndex = 0;
            while (playerPool.Count > 0)
            {
                playerIndex = scrRandom.Next(1, playerPool.Count) - 1;
                ///consoleWrite(String.Format("Random Number: {0}", playerIndex));
                movePlayer(playerPool[playerIndex].SoldierName, 1);
                ///consoleWrite(String.Format("Moving player {0} to team {1}", playerPool[playerIndex].SoldierName, 1));
                playerPool.RemoveAt(playerIndex);

                if (playerPool.Count > 0)
                {
                    playerIndex = scrRandom.Next(1, playerPool.Count) - 1;
                    ///consoleWrite(String.Format("Random Number: {0}", playerIndex));
                    movePlayer(playerPool[playerIndex].SoldierName, 2);
                    ///consoleWrite(String.Format("Moving player {0} to team {1}", playerPool[playerIndex].SoldierName, 2));
                    playerPool.RemoveAt(playerIndex);
                }
            }
        }
    }
}
