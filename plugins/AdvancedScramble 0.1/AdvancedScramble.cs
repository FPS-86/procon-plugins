/*  Copyright 2010 Imisnew2

    http://www.TeamPlayerGaming.com/members/Imisnew2.html

    This file is part of Imisnew2's Advanced Scramble Plugin for BFBC2 PRoCon.

    Imisnew2's Advanced Scramble Plugin for BFBC2 PRoCon is free software:
    you can redistribute it and/or modify it under the terms of the GNU
    General Public License as published by the Free Software Foundation,
    either version 3 of the License, or (at your option) any later version.

    Imisnew2's Advanced Scramble Plugin for BFBC2 PRoCon is distributed in
    the hope that it will be useful, but WITHOUT ANY WARRANTY; without even
    the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Imisnew2's Advanced Scramble Plugin for BFBC2 PRoCon.
    If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;

namespace PRoConEvents
{
    /// <summary>
    /// This plugin expands on functionalities implemented in the simply scramble plugin.
    /// </summary
    class AdvancedScramble : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Plugin Information

        /// <summary>Allows PRoCon to get the name of this plugin.</summary>
        /// <returns>The name of this plugin.</returns>
        public string GetPluginName() { return "Advanced Scramble"; }

        /// <summary>Allows PRoCon to get the version of this plugin.</summary>
        /// <returns>The version of this plugin.</returns>
        public string GetPluginVersion() { return "0.1"; }

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
                       "<p>Advanced Scramble lets you manage the teams on your Bad Company 2 server." +
                          "<br/>" +
                          "This plugin expands upon functionality introduced in the Simple Scramble plugin.  This plugin allows server administrators to scramble (or, more appropriately, distribute) players on their Bad Company 2 server.  The ability to ignore and group players has been added as well.  The command for this plugin is <i>ascramble</i>.<br/>" +
                          "<br/>" +
                          "To use the <i>ascramble</i> command, the player must have an account on procon with the permission \"Allowed to move players between teams and squads\". For syntax, see the following:<br/>" +
                          "[@!#/]ascramble &lt;now/round/map&gt; &lt;random/clan&gt;<br/>" +
                          "Descriptions:<br/>" +
                          "The <i>now</i> option will scramble the players immediately.  You can delay the scrambler by specifying either <i>round</i> or <i>map</i> to scramble upon round end and map end respectively.<br/>" +
                          "The <i>random</i> option will scramble the players without bias.  You can specify <i>clan</i> to attempt to keep clans together.<br/>" +

                     "<h2>Settings</h2>" +
                       "<h3>No Description For Now...</h3>" +

                         "<h4>Placeholder</h4>" +
                         "<blockquote style=\"margin-left: 0px; margin-right:0px; margin-top:0px;\">Placeholder." +
                           "<br/>" +
                           "<br/><u>Values</u>:" +
                           "<br/><i>Placeholder</i>: Placeholder." +
                           "<br/><i>Placeholder</i>: Placeholder.</blockquote>";
        }

        #endregion
        #region Plugin PRoCon Variables

        #region UI Variables

        // Scramble Settings
        String uiScrWhen = "none";
        String uiScrHow = "random";
        String uiScrGroup = "none";
        Int32 uiScrCount = 1;

        // Scramble Options
        Double uiSopScoreDelta = 210.5;
        Double uiSopKdrDelta = 3.5;
        Double uiSopMatchPercent = 80;

        // Whitelist Settings
        String[] uiWhtSeeders = new String[] { };
        String[] uiWhtIgnorePlayers = new String[] { };
        String[] uiWhtIgnoreClans = new String[] { };

        // Command Settings
        String uiComCommand = "ascramble";
        String[] uiComPrefixes = new String[] { "@", "!", "#", "/" };
        String uiComErrorMessage = "You are not allowed to use this command.";

        // Debug Settings
        enumBoolOnOff uiDbgToggle = enumBoolOnOff.Off;

        #endregion

        /// <summary>Allows PRoCon to figure out what fields to display to the user.</summary>
        /// <returns>A list of variables to display.</returns>
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            // Scramble Settings
            lstReturn.Add(new CPluginVariable("Default Scramble Settings|When", typeof(String), uiScrWhen));
            lstReturn.Add(new CPluginVariable("Default Scramble Settings|Method", typeof(String), uiScrHow));
            lstReturn.Add(new CPluginVariable("Default Scramble Settings|Grouping", typeof(String), uiScrGroup));
            lstReturn.Add(new CPluginVariable("Default Scramble Settings|Round/Map Count", typeof(Int32), uiScrCount));

            // Scramble Options
            lstReturn.Add(new CPluginVariable("Scramble Options|Score Delta", typeof(Double), uiSopScoreDelta));
            lstReturn.Add(new CPluginVariable("Scramble Options|Kdr Delta", typeof(Double), uiSopKdrDelta));
            lstReturn.Add(new CPluginVariable("Scramble Options|Clan Tag Matching Threshold", typeof(Int32), uiSopMatchPercent));

            // Whitelist Settings
            lstReturn.Add(new CPluginVariable("Whitelist Settings|Seeders", typeof(String[]), uiWhtSeeders));
            lstReturn.Add(new CPluginVariable("Whitelist Settings|Ignore Players", typeof(String[]), uiWhtIgnorePlayers));
            lstReturn.Add(new CPluginVariable("Whitelist Settings|Ignore Clans", typeof(String[]), uiWhtIgnoreClans));

            // Command Settings
            lstReturn.Add(new CPluginVariable("Command Settings|Command", typeof(String), uiComCommand));
            lstReturn.Add(new CPluginVariable("Command Settings|Prefixes", typeof(String[]), uiComPrefixes));
            lstReturn.Add(new CPluginVariable("Command Settings|Error Message", typeof(String), uiComErrorMessage));

            // Debug Settings
            lstReturn.Add(new CPluginVariable("Debug Settings|Show Debug", typeof(enumBoolOnOff), uiDbgToggle));

            return lstReturn;
        }

        /// <summary>Allows PRoCon to save the variables for persistence across sessions.</summary>
        /// <returns>A list of variables to save.</returns>
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            // Scramble Settings
            lstReturn.Add(new CPluginVariable("When", typeof(String), uiScrWhen));
            lstReturn.Add(new CPluginVariable("Method", typeof(String), uiScrHow));
            lstReturn.Add(new CPluginVariable("Grouping", typeof(String), uiScrGroup));
            lstReturn.Add(new CPluginVariable("Round/Map Count", typeof(Int32), uiScrCount));

            // Scramble Options
            lstReturn.Add(new CPluginVariable("Score Delta", typeof(Double), uiSopScoreDelta));
            lstReturn.Add(new CPluginVariable("Kdr Delta", typeof(Double), uiSopKdrDelta));
            lstReturn.Add(new CPluginVariable("Clan Tag Matching Threshold", typeof(Int32), uiSopMatchPercent));

            // Whitelist Settings
            lstReturn.Add(new CPluginVariable("Seeders", typeof(String[]), uiWhtSeeders));
            lstReturn.Add(new CPluginVariable("Ignore Players", typeof(String[]), uiWhtIgnorePlayers));
            lstReturn.Add(new CPluginVariable("Ignore Clans", typeof(String[]), uiWhtIgnoreClans));

            // Command Settings
            lstReturn.Add(new CPluginVariable("Command", typeof(String), uiComCommand));
            lstReturn.Add(new CPluginVariable("Prefixes", typeof(String[]), uiComPrefixes));
            lstReturn.Add(new CPluginVariable("Error Message", typeof(String), uiComErrorMessage));

            // Debug Settings
            lstReturn.Add(new CPluginVariable("Show Debug", typeof(enumBoolOnOff), uiDbgToggle));

            return lstReturn;
        }

        /// <summary>Allows PRoCon to load the variables from a previous session.  Also allows the user to change the variable as well.</summary>
        /// <param name="strVariable">The name of the variable we're loading.</param>
        /// <param name="strValue">The value of the variable we're loading.</param>
        public void SetPluginVariable(string strVariable, string strValue)
        {
            Int32 intOut = 0;
            Double dblOut = 0.0;

            #region Scramble Settings

            if (strVariable == "When")
            {
                // If the user changed the "When" property, reset the current counter.
                if (uiScrWhen != strValue.ToLower())
                {
                    scrCurrentCount = 0;
                    scrDefaultProperties.When = strValue.ToLower();
                    uiScrWhen = scrDefaultProperties.When;
                }
            }
            else if (strVariable == "Method")
            {
                // Don't allow the user to set the "How" property to "None" directly.
                if (strValue.ToLower() != "none")
                {
                    scrDefaultProperties.How = strValue.ToLower();
                    uiScrHow = scrDefaultProperties.How;
                }
            }
            else if (strVariable == "Grouping")
            {
                scrDefaultProperties.Group = strValue.ToLower();
                uiScrGroup = scrDefaultProperties.Group;
            }
            else if (strVariable == "Round/Map Count" && Int32.TryParse(strValue, out intOut))
            {
                // Don't allow the user to set the Count to less than 1.
                uiScrCount = scrCount = (intOut > 0) ? intOut : 1;
            }

            #endregion
            #region Scramble Options

            else if (strVariable == "Score Delta" && Double.TryParse(strValue, out dblOut))
            {
                // Don't allow the user to set the Delta to less than 0.
                uiSopScoreDelta = sopScoreDelta = (dblOut >= 0.0) ? dblOut : 100.0;
            }
            else if (strVariable == "Kdr Delta" && Double.TryParse(strValue, out dblOut))
            {
                // Don't allow the user to set the Delta to less than 0.
                uiSopKdrDelta = sopKdrDelta = (dblOut >= 0.0) ? dblOut : 2.5;
            }
            else if (strVariable == "Clan Tag Matching Threshold" && Double.TryParse(strValue, out dblOut))
            {
                // Don't allow the user to set the Matching Percent equal to 0.
                uiSopMatchPercent = sopMatchPercent = (dblOut > 0.0) ? dblOut : 80.0;
            }

            #endregion
            #region Whitelist Settings

            else if (strVariable == "Seeders")
            {
                uiWhtSeeders = whtSeeders = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable == "Ignore Players")
            {
                uiWhtIgnorePlayers = whtIgnorePlayers = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable == "Ignore Clans")
            {
                uiWhtIgnoreClans = whtIgnoreClans = CPluginVariable.DecodeStringArray(strValue);
            }

            #endregion
            #region Command Settings

            else if (strVariable == "Command" && strValue.Length > 0)
            {
                // Unregister/Reregister command when user changes a part of it.
                unregisterCommand();
                uiComCommand = comCommand = strValue;
                registerCommand();
            }
            else if (strVariable == "Prefixes")
            {
                // Unregister/Reregister command when user changes a part of it.
                unregisterCommand();
                uiComPrefixes = CPluginVariable.DecodeStringArray(strValue);
                comPrefixes = new List<String>(uiComPrefixes);
                registerCommand();
            }
            else if (strVariable == "Error Message" && strValue.Length > 0)
            {
                // Unregister/Reregister command when user changes a part of it.
                unregisterCommand();
                uiComErrorMessage = comErrorMessage = strValue;
                registerCommand();
            }

            #endregion
            #region Debug Settings

            else if (strVariable == "Show Debug" && Enum.IsDefined(typeof(enumBoolOnOff), strValue))
            {
                othDebug = (uiDbgToggle = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue)) == enumBoolOnOff.On;
            }

            #endregion
        }

        #endregion
        #region Plugin Variables

        // [0] The default properties used to scramble players.  Includes the following:
        //     -- When to scramble the players.
        //     -- How to scramble the players.
        //     -- How to group the players.
        // [1] The command properties for the last command specified.
        // [2] How many maps/rounds must pass before the automated scrambler kicks in.
        // [3] Counts how many rounds/maps have passed since the automation mode was set.
        // [4] Stores the map name so we can determine when the map changes.
        // [5] A random number generator, used when the "How" is "Random".
        ScrambleProperties scrDefaultProperties;
        ScrambleProperties scrCurrentProperties;    // Not Used in UI.
        Int32 scrCount;
        Int32 scrCurrentCount;         // Not Used in UI.
        String scrCurrentMap;           // Not Used in UI.
        Random scrRandom;               // Not Used in UI.

        // [0] The amount of tolerance we'll accept when trying to find a split for score.
        // [1] The amount of tolerance we'll accept when trying to find a split for kdr.
        // [2] The amount of tolerance we'll accept when grouping players by clan tags.
        Double sopScoreDelta;
        Double sopKdrDelta;
        Double sopMatchPercent;

        // [0] The player names of seeders.  Are balanced separately.
        // [1] The player names of people exempt from balancing.  Do not get swapped.
        // [2] The clan tags of people exempt from balancing.  Do not get swapped.
        String[] whtSeeders;
        String[] whtIgnorePlayers;
        String[] whtIgnoreClans;

        // [0] The string the the player must type to use this plugin as a command.
        // [1] The prefixes the player can use in conjunction with the command.
        // [2] The valid arguments that can be supplied for this command.
        // [3] The error message that is displayed to the player if he/she does not have valid permissions.
        String comCommand;
        List<String> comPrefixes;
        List<String> comArguments;                  // Not Used in UI.
        String comErrorMessage;

        // [0] The state of the plugin.
        // [1] Whether to display debug information.
        // [2] A dictionary of players on the server (and their respective player information).
        Boolean othEnabled;
        Boolean othDebug;
        Dictionary<String, CPlayerInfo> othCurrentPlayers;  // Not Used in UI.
        Dictionary<String, CPlayerInfo> othArchivedPlayers; // Not Used in UI.

        // --- Internal Classes
        // [0] Properties to determine how to scramble the players.
        internal class ScrambleProperties
        {
            public static readonly List<String> mValidWhens = new List<String>(new String[] { "now", "round", "map" });
            public static readonly List<String> mValidHows = new List<String>(new String[] { "random", "score", "kdr" });
            public static readonly List<String> mValidGroups = new List<String>(new String[] { "clan", "squad" });

            /// <summary>
            /// When this command should be issued.
            /// Valid options are: None, Now, Round, Map.
            /// </summary>
            public String When
            {
                get { return mWhen; }
                set
                {
                    if (mValidWhens.Contains(value) || value == "none")
                        mWhen = value;
                }
            }
            private String mWhen = "none";
            /// <summary>
            /// How this command should be issued.
            /// Valid options are: None, Random, Score, Kdr.
            /// </summary>
            public String How
            {
                get { return mHow; }
                set
                {
                    if (mValidHows.Contains(value) || value == "none")
                        mHow = value;
                }
            }
            private String mHow = "none";
            /// <summary>
            /// How this command should group players.
            /// Valid options are: None, Clan, Squad.
            /// </summary>
            public String Group
            {
                get { return mGroup; }
                set
                {
                    if (mValidGroups.Contains(value) || value == "none")
                        mGroup = value;
                }
            }
            private String mGroup = "none";
        }
        // [1] Represents a group of players that are similar in respect to the group property.
        internal class ScrambleGroup
        {
            /// <summary>The type of this scramble group.</summary>
            public ScrambleGroupType Type
            {
                get { return mType; }
                private set { mType = value; }
            }
            private ScrambleGroupType mType;

            /// <summary>The players of this group.</summary>
            public List<CPlayerInfo> Players
            {
                get { return mPlayers; }
                private set { mPlayers = value; }
            }
            private List<CPlayerInfo> mPlayers;

            /// <summary>The combined score of this group.</summary>
            public Double Score
            {
                get { return mScore; }
                private set { mScore = value; }
            }
            private Double mScore;
            /// <summary>The combined kdr of this group.</summary>
            public Double Kdr
            {
                get { return mKdr; }
                private set { mKdr = value; }
            }
            private Double mKdr;

            /// <summary>Creates a group of players using these players information.</summary>
            public ScrambleGroup(List<CPlayerInfo> players, ScrambleGroupType type)
            {
                Type = type;
                Players = new List<CPlayerInfo>(players);
                foreach (CPlayerInfo p in Players)
                {
                    Score += p.Score;
                    Kdr += p.Kdr;
                }
            }
        }
        // [2] Represents a group of scramble groups.
        internal class ScrambleSplit
        {
            /// <summary>The groups of this split.</summary>
            public List<ScrambleGroup> Groups
            {
                get { return mGroups; }
                private set { mGroups = value; }
            }
            private List<ScrambleGroup> mGroups;

            /// <summary>The number of players in this split.</summary>
            public Int32 Count
            {
                get { return mCount; }
                private set { mCount = value; }
            }
            private Int32 mCount;
            /// <summary>The combined score of these groups.</summary>
            public Double Score
            {
                get { return mScore; }
                private set { mScore = value; }
            }
            private Double mScore;
            /// <summary>The combined kdr of these groups.</summary>
            public Double Kdr
            {
                get { return mKdr; }
                private set { mKdr = value; }
            }
            private Double mKdr;

            /// <summary>Adds a group to the split.</summary>
            public void AddGroup(ScrambleGroup group)
            {
                Groups.Add(group);
                Count += group.Players.Count;
                Score += group.Score;
                Kdr += group.Kdr;
            }

            public ScrambleSplit()
            {
                Groups = new List<ScrambleGroup>();
            }
            public ScrambleSplit(ScrambleSplit split)
            {
                Groups = new List<ScrambleGroup>(split.Groups);
                Count = split.Count;
                Score = split.Score;
                Kdr = split.Kdr;
            }
        }
        // [3] Represents the type of the scramble group.
        internal enum ScrambleGroupType
        {
            Single,
            Group,
            Whitelist
        }

        #endregion
        #region Plugin Loaded/Enable/Disable

        /// <summary>Is called when the plugin is successfully loaded.</summary>
        public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
        {
            // Register Events.
            this.RegisterEvents(
                /* Determines which players are currently on the server. */
                "OnPlayerJoin", "OnPlayerLeft", "OnListPlayers",
                /* Used for announcements and automation.                */
                "OnLoadingLevel", "OnRoundOver", "OnLevelStarted");
        }

        /// <summary>Is called when the plugin is turned on.</summary>
        public void OnPluginEnable()
        {
            // Clear Previous Settings
            scrCurrentCount = 0;
            scrCurrentMap = String.Empty;
            scrCurrentProperties = new ScrambleProperties();
            othCurrentPlayers = new Dictionary<String, CPlayerInfo>();
            othArchivedPlayers = new Dictionary<String, CPlayerInfo>();

            // Plugin Starting:
            consoleWrite("^2Enabled!");
            othEnabled = true;
            registerCommand();
        }

        /// <summary>Is called when the plugin is turned off.</summary>
        public void OnPluginDisable()
        {
            // Plugin Stopping:
            consoleWrite("^1Disabled. =(");
            unregisterCommand();
            othEnabled = false;
        }

        #endregion
        #region Plugin Methods

        private void setPluginState(Boolean state)
        { this.ExecuteCommand("procon.protected.plugins.enable", "AdvancedScramble", state.ToString()); }
        private void consoleWrite(String message)
        { this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdv. Scramble: ^n" + message); }
        private void debugWrite(String message)
        { if (othDebug) this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdv. Scramble: ^n^7" + message); }
        private void forceBcListing()
        { this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all"); }
        private void movePlayer(String player, Int32 teamId)
        { this.ExecuteCommand("procon.protected.send", "admin.movePlayer", player, teamId.ToString(), "0", "true"); }
        private void say(String message)
        { this.ExecuteCommand("procon.protected.send", "admin.say", message, "all"); }
        private void yell(String message, Int32 duration)
        { this.ExecuteCommand("procon.protected.send", "admin.yell", message, duration.ToString(), "all"); }

        #endregion
        #region Plugin Events

        /// <summary>Is called when a player joins the server.</summary>
        /// <param name="strSoldierName">The player's name.</param>
        public void OnPlayerJoin(String strSoldierName)
        {
            if (!othCurrentPlayers.ContainsKey(strSoldierName))
                othCurrentPlayers.Add(strSoldierName, null);
        }

        /// <summary>Is called when a player leaves the server.</summary>
        /// <param name="strSoldierName">The player's name.</param>
        public void OnPlayerLeft(String strSoldierName)
        {
            if (othCurrentPlayers.ContainsKey(strSoldierName))
                othCurrentPlayers.Remove(strSoldierName);
        }

        /// <summary>Is called when a player list is received.</summary>
        /// <param name="lstPlayers">The list of players.</param>
        /// <param name="cpsSubset">The subset the list of players is of.</param>
        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            // Only update the player list if we've gotten an entire subset.
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                othCurrentPlayers.Clear();
                foreach (CPlayerInfo player in lstPlayers)
                    othCurrentPlayers.Add(player.SoldierName, player);
            }
        }

        /// <summary>Is called when the next level is being loaded.</summary>
        /// <param name="strMapFileName">The name of the level being loaded.</param>
        public void OnLoadingLevel(String strMapFileName, Int32 currentRound, Int32 maxRounds)
        {
            Boolean scrambleDefault = false;
            Boolean scrambleCurrent = false;
            String scrambleMode = null;
            String scrambleGroup = null;

            // Check for Round-Based Automation and Commands.
            if (scrDefaultProperties.When == "round")
                if (++scrCurrentCount >= scrCount)
                {
                    scrambleDefault = true;
                    scrCurrentCount = 0;
                }
            if (scrCurrentProperties.When == "round")
                scrambleCurrent = true;

            // Check for Map-Based Automation and Commands.
            if (scrCurrentMap != strMapFileName)
            {
                scrCurrentMap = strMapFileName;
                if (scrDefaultProperties.When == "map")
                    if (++scrCurrentCount >= scrCount)
                    {
                        scrambleDefault = true;
                        scrCurrentCount = 0;
                    }
                if (scrCurrentProperties.When == "map")
                    scrambleCurrent = true;
            }

            // Commands take precedence over the default.
            if (scrambleCurrent)
            {
                scrambleMode = scrCurrentProperties.How;
                scrambleGroup = scrCurrentProperties.Group;
                resetCommand();
            }
            else if (scrambleDefault)
            {
                scrambleMode = scrDefaultProperties.How;
                scrambleGroup = scrDefaultProperties.Group;
            }

            // Scramble/Distribute the players.
            if (scrambleMode != null)
            {
                consoleWrite("Scrambling The Players: Method (" + scrambleMode + "), Grouping (" + scrambleGroup + ").");
                displayMessage("now", scrambleMode, scrambleGroup);
                scramblePlayers(scrambleMode, scrambleGroup);
            }
        }

        /// <summary>Is called when the round ends.</summary>
        public void OnRoundOver(int iWinningTeamID)
        {
            // Archive old players.
            othArchivedPlayers = new Dictionary<String, CPlayerInfo>(othCurrentPlayers);

            // Get the properties that are currently going to be used to scramble the teams.
            String mWhen = (scrCurrentProperties.When != "none") ? scrCurrentProperties.When : scrDefaultProperties.When;
            String mHow = (scrCurrentProperties.How != "none") ? scrCurrentProperties.How : scrDefaultProperties.How;
            String mGroup = (scrCurrentProperties.Group != "none") ? scrCurrentProperties.Group : scrDefaultProperties.Group;

            // Convert the scramble properties to human readable text.
            displayMessage(mWhen, mHow, mGroup);
        }

        #endregion

        /// <summary>Constructor.</summary>
        public AdvancedScramble()
        {
            scrDefaultProperties = new ScrambleProperties();
            scrDefaultProperties.When = uiScrWhen;
            scrDefaultProperties.How = uiScrHow;
            scrDefaultProperties.Group = uiScrGroup;
            scrCurrentProperties = new ScrambleProperties();
            scrCount = uiScrCount;
            scrCurrentCount = 0;
            scrCurrentMap = String.Empty;
            scrRandom = new Random();

            whtSeeders = uiWhtSeeders;
            whtIgnorePlayers = uiWhtIgnorePlayers;
            whtIgnoreClans = uiWhtIgnoreClans;

            comCommand = uiComCommand;
            comPrefixes = new List<String>(uiComPrefixes);
            comArguments = new List<String>();
            comArguments.AddRange(ScrambleProperties.mValidWhens);
            comArguments.AddRange(ScrambleProperties.mValidHows);
            comArguments.AddRange(ScrambleProperties.mValidGroups);
            comArguments.AddRange(new String[] { "status", "cancel" });
            comErrorMessage = uiComErrorMessage;

            othEnabled = false;
            othDebug = true;
            othCurrentPlayers = new Dictionary<String, CPlayerInfo>();
            othArchivedPlayers = new Dictionary<String, CPlayerInfo>();
        }


        /// <summary>Is fired whenever someone with the correct permissions uses the scramble command.</summary>
        /// <param name="strSpeaker">The person who used the command.</param>
        /// <param name="strText">The specific message the person typed.</param>
        /// <param name="mtcCommand">The command that was matched.</param>
        /// <param name="capCommand">The details of the command that was sent.</param>
        /// <param name="subMatchedScope">The scope the command was sent to.</param>
        public void OnCommandScramble(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            String lastWhenArg = "none";
            String lastHowArg = "none";
            String lastGroupArg = "none";
            Boolean isCancel = false;
            Boolean isStatus = false;

            // Get the the command arguments.
            foreach (MatchArgument matchArg in capCommand.MatchedArguments)
                switch (matchArg.Argument.ToLower())
                {
                    // When Argument:
                    case "now":
                    case "round":
                    case "map":
                        lastWhenArg = matchArg.Argument.ToLower();
                        break;

                    // How Argument:
                    case "random":
                    case "score":
                    case "kdr":
                        lastHowArg = matchArg.Argument.ToLower();
                        break;

                    // Group Arguments:
                    case "clan":
                    case "squad":
                        lastGroupArg = matchArg.Argument.ToLower();
                        break;

                    // Cancel Command:
                    case "cancel":
                        isCancel = true;
                        break;

                    // Status Command:
                    case "status":
                        isStatus = true;
                        break;
                }

            // Nullify the current command and send out messages if applicable.
            if (isCancel)
            {
                // Get the properties that are currently going to be used to scramble the teams.
                String mWhen = scrCurrentProperties.When;
                String mHow = (scrCurrentProperties.How != "none") ? scrCurrentProperties.How : scrDefaultProperties.How;
                String mGroup = (scrCurrentProperties.Group != "none") ? scrCurrentProperties.Group : scrDefaultProperties.Group;

                // Display a canceled message.
                if (mWhen != "none")
                {
                    logMessage(strSpeaker, "Canceled", mWhen, mHow, mGroup);
                    say("The previously set scramble has been canceled.");
                }
                resetCommand();
                return;
            }

            // Simply print out current command and exit.
            if (isStatus)
            {
                // Get the properties that are currently going to be used to scramble the teams.
                String mWhen = scrCurrentProperties.When;
                String mHow = (scrCurrentProperties.How != "none") ? scrCurrentProperties.How : scrDefaultProperties.How;
                String mGroup = (scrCurrentProperties.Group != "none") ? scrCurrentProperties.Group : scrDefaultProperties.Group;

                // Display a status message.
                logMessage(strSpeaker, "Status", mWhen, mHow, mGroup);
                if (mWhen == "none")
                    say("No command is currently set.");
                else
                    displayMessage(mWhen, mHow, mGroup);

                return;
            }

            // Set the command using the specified (or default) properties.
            scrCurrentProperties.When = (lastWhenArg != "none") ? lastWhenArg : "now";
            scrCurrentProperties.How = (lastHowArg != "none") ? lastHowArg : scrDefaultProperties.How;
            scrCurrentProperties.Group = (lastGroupArg != "none") ? lastGroupArg : scrDefaultProperties.Group;

            // Display a message that relates to the command.
            logMessage(strSpeaker, "Set", scrCurrentProperties.When, scrCurrentProperties.How, scrCurrentProperties.Group);
            displayMessage(scrCurrentProperties.When, scrCurrentProperties.How, scrCurrentProperties.Group);

            // If the scramble was asked to be done now:
            if (scrCurrentProperties.When == "now")
            {
                scramblePlayers(scrCurrentProperties.How, scrCurrentProperties.Group);
                scrCurrentCount = 0;
                resetCommand();
            }
        }


        /// <summary>Registers the advanced scramble command.</summary>
        private void registerCommand()
        {
            // Only register the command if the plugin has been enabled.
            if (othEnabled)
            {
                // No Args.
                this.RegisterCommand(
                    new MatchCommand(
                        "AdvancedScramble",
                        "OnCommandScramble",
                        comPrefixes,
                        comCommand,
                        Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanMovePlayers, comErrorMessage),
                        "Allows a player in-game to use the Advanced Scramble plugin."));
                // 1 Arg.
                this.RegisterCommand(
                    new MatchCommand(
                        "AdvancedScramble",
                        "OnCommandScramble",
                        comPrefixes,
                        comCommand,
                        Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat("ScrambleArgs", comArguments)),
                        new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanMovePlayers, comErrorMessage),
                        "Allows a player in-game to use the Advanced Scramble plugin."));
                // 2 Args.
                this.RegisterCommand(
                    new MatchCommand(
                        "AdvancedScramble",
                        "OnCommandScramble",
                        comPrefixes,
                        comCommand,
                        Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat("ScrambleArgs", comArguments),
                            new MatchArgumentFormat("ScrambleArgs", comArguments)),
                        new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanMovePlayers, comErrorMessage),
                        "Allows a player in-game to use the Advanced Scramble plugin."));
                // 3 Args.
                this.RegisterCommand(
                    new MatchCommand(
                        "AdvancedScramble",
                        "OnCommandScramble",
                        comPrefixes,
                        comCommand,
                        Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat("ScrambleArgs", comArguments),
                            new MatchArgumentFormat("ScrambleArgs", comArguments),
                            new MatchArgumentFormat("ScrambleArgs", comArguments)),
                        new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanMovePlayers, comErrorMessage),
                        "Allows a player in-game to use the Advanced Scramble plugin."));
            }
        }
        /// <summary>Unregisters the advanced scramble command.</summary>
        private void unregisterCommand()
        {
            // No Args.
            this.UnregisterCommand(
                new MatchCommand(
                    comPrefixes,
                    comCommand,
                    Listify<MatchArgumentFormat>()));
            // 1 Arg.
            this.UnregisterCommand(
                new MatchCommand(
                    comPrefixes,
                    comCommand,
                    Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat("ScrambleArgs", comArguments))));
            // 2 Args.
            this.UnregisterCommand(
                new MatchCommand(
                    comPrefixes,
                    comCommand,
                    Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat("ScrambleArgs", comArguments),
                        new MatchArgumentFormat("ScrambleArgs", comArguments))));
            // 3 Args.
            this.UnregisterCommand(
                new MatchCommand(
                    comPrefixes,
                    comCommand,
                    Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat("ScrambleArgs", comArguments),
                        new MatchArgumentFormat("ScrambleArgs", comArguments),
                        new MatchArgumentFormat("ScrambleArgs", comArguments))));
        }
        /// <summary>Sets the current command back to default values.</summary>
        private void resetCommand()
        {
            scrCurrentProperties.When = "none";
            scrCurrentProperties.How = "none";
            scrCurrentProperties.Group = "none";
        }


        /// <summary>Displays a message to the server using the specified parameters.</summary>
        /// <param name="when">When the players will be scrambled.</param>
        /// <param name="how">How the players will be scrambled.</param>
        /// <param name="group">How the players will be grouped.</param>
        private void displayMessage(String when, String how, String group)
        {
            // Do not display a message if we have a bad parameter.
            if (when == "none" || how == "none")
                return;

            // Format the first part of the message (How the players will be scrambled).
            String mMessage = "";
            switch (how)
            {
                case "random":
                    mMessage += "The Players will be Scrambled Randomly";
                    break;
                case "score":
                    mMessage += "The Players will be Distributed by Score";
                    break;
                case "kdr":
                    mMessage += "The Players will be Distributed by KDR";
                    break;
            }
            // Format the second part of the message (How the players will be grouped).
            switch (group)
            {
                case "clan":
                    mMessage += ", attempting to keep Clans together,";
                    break;
                case "squad":
                    mMessage += ", attempting to keep Squads together,";
                    break;
            }
            // Format the third part of the message (When the players will be scrambled).
            switch (when)
            {
                case "now":
                    mMessage += " Now.";
                    break;
                case "round":
                    mMessage += " on Round end.";
                    break;
                case "map":
                    mMessage += " on Map end.";
                    break;
            }

            // Display the message.
            say(mMessage);
        }
        /// <summary>Logs what the specified person did.</summary>
        /// <param name="speaker">The person who said the command.</param>
        /// <param name="command">Whether the command is a set or cancel.</param>
        /// <param name="when">When the players will be scrambled.</param>
        /// <param name="how">How the players will be scrambled.</param>
        /// <param name="group">How the players will be grouped.</param>
        private void logMessage(String speaker, String command, String when, String how, String group)
        {
            consoleWrite(speaker + "- " + command + ": Scramble (m." + how + "/g." + group + ") => w." + when);
        }


        /// <summary>Scrambles the players according to the specified parameters.</summary>
        /// <param name="how">How to scramble the players.</param>
        /// <param name="group">How to group the players.</param>
        private void scramblePlayers(String how, String group)
        {
            List<ScrambleGroup> groupedPlayers = groupPlayers(group);
            switch (how)
            {
                case "random":
                    scramblePlayersByRandom(groupedPlayers);
                    break;
                case "score":
                    scramblePlayersByScore(groupedPlayers);
                    break;
                case "kdr":
                    scramblePlayersByKdr(groupedPlayers);
                    break;
            }
            forceBcListing();
        }
        /// <summary>Distributes players across two teams the players using a random approach.</summary>
        private void scramblePlayersByRandom(List<ScrambleGroup> groups)
        {
            List<ScrambleGroup> pool = randomizeGroups(groups);

            // For random, since we don't care about specifically balancing teams, we can request
            // a random split, which is simply the first split found with this ordering of groups.
            // -- Try 'x' times to get a split before giving up.
            ScrambleSplit split = getRandomSplit(pool);
            for (int i = 0; split == null && i < 100; i++)
            {
                pool = randomizeGroups(groups);
                split = getRandomSplit(pool);
            }

            // -- Check to make sure we got a split before proceeding.  If we couldn't find a split,
            //    let the administrator know how sorry we are and exit the function.  Ideally, this
            //    should never be hit.
            if (split == null)
            { consoleWrite("Sorry, I couldn't find a random split of players."); return; }

            // -- Distribute the players onto their teams as denoted by the split.
            distributePlayers(split, groups);
        }
        /// <summary>Distributes players across two teams the players balancing them by their Score.</summary>
        private void scramblePlayersByScore(List<ScrambleGroup> groups)
        {
            List<ScrambleGroup> pool = randomizeGroups(groups);

            // With score, we are faced with a Partition Problem.  We must split up the players evenly,
            // but we must also split up the scores.  To top it off, we're dealing with groups of players,
            // not single players.  For this reason, we request score splits.
            // -- Try 'x' times to get a split before giving up.
            ScrambleSplit split = getScoreSplit(pool);
            for (int i = 0; split == null && i < 4; i++)
            {
                pool = randomizeGroups(groups);
                split = getScoreSplit(pool);
            }

            // -- Check to make sure we got a split before proceeding.  If we couldn't find a split,
            //    let the administrator know how sorry we are and exit the function.  Ideally, this
            //    should never be hit.
            if (split == null)
            { consoleWrite("Sorry, I couldn't find a balanced split of players using score."); return; }

            // -- Distribute the players onto their teams as denoted by the split.
            distributePlayers(split, groups);
        }
        /// <summary>Distributes players across two teams the players balancing them by their Kill per Death ratio.</summary>
        private void scramblePlayersByKdr(List<ScrambleGroup> groups)
        {
            List<ScrambleGroup> pool = randomizeGroups(groups);

            // With kdr, we are faced with a Partition Problem.  We must split up the players evenly,
            // but we must also split up the kdrs.  To top it off, we're dealing with groups of players,
            // not single players.  For this reason, we request kdr splits.
            // -- Try 'x' times to get a split before giving up.
            ScrambleSplit split = getKdrSplit(pool);
            for (int i = 0; split == null && i < 4; i++)
            {
                pool = randomizeGroups(groups);
                split = getKdrSplit(pool);
            }

            // -- Check to make sure we got a split before proceeding.  If we couldn't find a split,
            //    let the administrator know how sorry we are and exit the function.  Ideally, this
            //    should never be hit.
            if (split == null)
            { consoleWrite("Sorry, I couldn't find a balanced split of players using kdr."); return; }

            // -- Distribute the players onto their teams as denoted by the split.
            distributePlayers(split, groups);
        }


        /// <summary>Groups players according to a set of group properties.</summary>
        /// <param name="group">How to group the players.</param>
        private List<ScrambleGroup> groupPlayers(String group)
        {
            List<CPlayerInfo> upToDatePlayers = getPlayers();
            switch (group)
            {
                case "none":
                    return groupPlayersByNone(upToDatePlayers);
                case "clan":
                    return groupPlayersByClan(upToDatePlayers);
                case "squad":
                    return groupPlayersBySquad(upToDatePlayers);
            }
            return null;
        }
        /// <summary>Groups each player into their own group.</summary>
        private List<ScrambleGroup> groupPlayersByNone(List<CPlayerInfo> players)
        {
            List<ScrambleGroup> groupedPlayers = new List<ScrambleGroup>();
            foreach (CPlayerInfo player in players)
                groupedPlayers.Add(new ScrambleGroup(new List<CPlayerInfo>(new CPlayerInfo[] { player }), ScrambleGroupType.Single));
            return groupedPlayers;
        }
        /// <summary>Groups the specified players by clans.</summary>
        private List<ScrambleGroup> groupPlayersByClan(List<CPlayerInfo> players)
        {
            List<ScrambleGroup> groupedPlayers = new List<ScrambleGroup>();

            // Remove all the players who are not in a clan.
            for (int i = 0; i < players.Count; i++)
                if (players[i].ClanTag == String.Empty)
                {
                    groupedPlayers.Add(new ScrambleGroup(new List<CPlayerInfo>(new CPlayerInfo[] { players[i] }), ScrambleGroupType.Single));
                    players.RemoveAt(i);
                    i--;
                }

            // Group all the players who are on the same team in the same clan.
            while (players.Count > 0)
            {
                List<CPlayerInfo> commonPlayers = new List<CPlayerInfo>();
                commonPlayers.Add(players[0]);
                for (int i = 1; i < players.Count; i++)
                    if (commonPlayers[0].TeamID == players[i].TeamID)
                        if (calcPercentMatch(commonPlayers[0].ClanTag.ToLower(), players[i].ClanTag.ToLower()) >= sopMatchPercent)
                            commonPlayers.Add(players[i]);
                foreach (CPlayerInfo player in commonPlayers)
                    players.Remove(player);
                groupedPlayers.Add(new ScrambleGroup(commonPlayers, ScrambleGroupType.Group));
            }

            #region ---- DEBUG PRINT ----

            if (othDebug)
            {
                debugWrite("Result of Clan Grouping:");
                foreach (ScrambleGroup group in groupedPlayers)
                {
                    debugWrite("--Group:");
                    foreach (CPlayerInfo player in group.Players)
                        debugWrite(String.Format("----Player [{0}], Clan [{1}]", player.SoldierName, player.ClanTag));
                }
            }

            #endregion ---- DEBUG PRINT ----
            return groupedPlayers;
        }
        /// <summary>Groups the specified players by squads.</summary>
        private List<ScrambleGroup> groupPlayersBySquad(List<CPlayerInfo> players)
        {
            List<ScrambleGroup> groupedPlayers = new List<ScrambleGroup>();

            // Remove all the players who are not in a squad.
            for (int i = 0; i < players.Count; i++)
                if (players[i].SquadID <= 0)
                {
                    groupedPlayers.Add(new ScrambleGroup(new List<CPlayerInfo>(new CPlayerInfo[] { players[i] }), ScrambleGroupType.Single));
                    players.RemoveAt(i);
                    i--;
                }

            // Group all the players who are on the same team in the same squad.
            while (players.Count > 0)
            {
                while (players.Count > 0)
                {
                    List<CPlayerInfo> commonPlayers = new List<CPlayerInfo>();
                    commonPlayers.Add(players[0]);
                    for (int i = 1; i < players.Count; i++)
                        if (commonPlayers[0].SquadID == players[i].SquadID && commonPlayers[0].TeamID == players[i].TeamID)
                            commonPlayers.Add(players[i]);
                    foreach (CPlayerInfo player in commonPlayers)
                        players.Remove(player);
                    groupedPlayers.Add(new ScrambleGroup(commonPlayers, ScrambleGroupType.Group));
                }
            }

            #region ---- DEBUG PRINT ----

            if (othDebug)
            {
                debugWrite("Result of Squad Grouping:");
                foreach (ScrambleGroup group in groupedPlayers)
                {
                    debugWrite("--Group:");
                    foreach (CPlayerInfo player in group.Players)
                        debugWrite(String.Format("----Player [{0}], Squad [{1}]", player.SoldierName, player.SquadID));
                }
            }

            #endregion ---- DEBUG PRINT ----
            return groupedPlayers;
        }


        /// <summary>
        /// Attempts to find a split using this collection of groups.  This, like it's sibling functions,
        /// will return the same split if called with the same ordering of groups. If no split could be 
        /// found, null is returned.
        /// </summary>
        /// <param name="groups">The list of groups we want to create splits with.</param>
        /// <returns>A ScrambleSplit that best matches our criteria.</returns>
        private ScrambleSplit getRandomSplit(List<ScrambleGroup> groups)
        {
            IEnumerator<ScrambleSplit> e = getSplits(groups, new ScrambleSplit(), 0, 0).GetEnumerator();

            // Resets the enumerator then gets the first value.
            e.MoveNext();
            return e.Current;
        }
        /// <summary>
        /// Attempts to find a split whose Score is within the specified delta.  This will only attempt to
        /// find a split for 5 seconds before returning the best split we could find.  If no splits were
        /// found, null will be returned.
        /// </summary>
        /// <param name="groups">The list of groups we want to create splits with.</param>
        /// <returns>A ScrambleSplit that best matches our criteria.</returns>
        private ScrambleSplit getScoreSplit(List<ScrambleGroup> groups)
        {
            IEnumerator<ScrambleSplit> e = getSplits(groups, new ScrambleSplit(), 0, 0).GetEnumerator();

            // Get the optimal score that the split should be.
            Double optScore = 0.0;
            foreach (ScrambleGroup group in groups)
                optScore += group.Score;
            optScore = Math.Ceiling(optScore / 2.0);

            // Record the current time so that we can time-out if necessary.
            // The reason for this is two-fold:
            // -- Procon freezes up because all of this is on the UI's thread.
            // -- We want the command to do something soon after it's issued.
            DateTime start = DateTime.Now;

            // Keep requesting splits until we hit either 1 of 2 conditions:
            // -- We find a split whose score is within our allowed delta.
            // -- We time out.
            ScrambleSplit split = null;
            Double best = Double.MaxValue;
            Double delta = Double.MaxValue;
            while (e.MoveNext())
            {
                // Do logic to find best split.
                delta = Math.Abs(e.Current.Score - optScore);
                if (delta <= sopScoreDelta)
                {
                    split = e.Current;
                    break;
                }
                else if (delta < best)
                {
                    split = e.Current;
                    best = delta;
                }

                // Check to see if we've timed out.
                if ((DateTime.Now - start).TotalSeconds > 5.0)
                    break;
            }

            // Return the split that we've found.  Note: this may not be a split
            // whose delta is within our threshold, however, it's the closest one
            // we could find.
            return split;
        }
        /// <summary>
        /// Attempts to find a split whose Kdr is within the specified delta.  This will only attempt to
        /// find a split for 5 seconds before returning the best split we could find.  If no splits were
        /// found, null will be returned.
        /// </summary>
        /// <param name="groups">The list of groups we want to create splits with.</param>
        /// <returns>A ScrambleSplit that best matches our criteria.</returns>
        private ScrambleSplit getKdrSplit(List<ScrambleGroup> groups)
        {
            IEnumerator<ScrambleSplit> e = getSplits(groups, new ScrambleSplit(), 0, 0).GetEnumerator();

            // Get the optimal kdr that the split should be.
            Double optKdr = 0.0;
            foreach (ScrambleGroup group in groups)
                optKdr += group.Score;
            optKdr = Math.Ceiling(optKdr / 2.0);

            // Record the current time so that we can time-out if necessary.
            // The reason for this is two-fold:
            // -- Procon freezes up because all of this is on the UI's thread.
            // -- We want the command to do something soon after it's issued.
            DateTime start = DateTime.Now;

            // Keep requesting splits until we hit either 1 of 2 conditions:
            // -- We find a split whose kdr is within our allowed delta.
            // -- We time out.
            ScrambleSplit split = null;
            Double best = Double.MaxValue;
            Double delta = Double.MaxValue;
            while (e.MoveNext())
            {
                // Do logic to find best split.
                delta = Math.Abs(e.Current.Kdr - optKdr);
                if (delta <= sopKdrDelta)
                {
                    split = e.Current;
                    break;
                }
                else if (delta < best)
                {
                    split = e.Current;
                    best = delta;
                }

                // Check to see if we've timed out.
                if ((DateTime.Now - start).TotalSeconds > 5.0)
                    break;
            }

            // Return the split that we've found.  Note: this may not be a split
            // whose delta is within our threshold, however, it's the closest one
            // we could find.
            return split;
        }
        /// <summary>Takes a list of scramble groups and divides them up into equal splits of players.</summary>
        /// <param name="input">The list of scramble groups to turn into scramble splits.</param>
        /// <param name="split">The current split the recursion is working with.</param>
        /// <param name="start">The current group the recursion is on.</param>
        /// <param name="maxPlayers">The maximum number of players to fit into a single split.</param>
        private IEnumerable<ScrambleSplit> getSplits(List<ScrambleGroup> input, ScrambleSplit split, Int32 start, Int32 maxPlayers)
        {
            // Calculate the maximum number of players allowed in a split.
            if (maxPlayers == 0)
            {
                foreach (ScrambleGroup group in input)
                    maxPlayers += group.Players.Count;
                maxPlayers = (Int32)Math.Ceiling(maxPlayers / 2.0);
            }

            // Begin the actual recursion.
            Int32 curPlayers;
            ScrambleSplit curSplit;
            while (start < input.Count)
            {
                curSplit = new ScrambleSplit(split);
                curSplit.AddGroup(input[start++]);
                curPlayers = curSplit.Count;
                // The split isn't full, recurse.
                if (curPlayers < maxPlayers)
                    foreach (ScrambleSplit ss in getSplits(input, curSplit, start, maxPlayers))
                        yield return ss;
                // Split is full, add to output.
                else if (curPlayers == maxPlayers)
                    yield return curSplit;
            }
        }


        /// <summary>Gets the most accurate list of players.</summary>
        private List<CPlayerInfo> getPlayers()
        {
            Dictionary<String, CPlayerInfo> upToDatePlayers = new Dictionary<String, CPlayerInfo>();

            // Map old information to current players and create information for players who have no information.
            foreach (String player in othCurrentPlayers.Keys)
                if (othArchivedPlayers.ContainsKey(player) && othArchivedPlayers[player] != null)
                    upToDatePlayers.Add(player, othArchivedPlayers[player]);
                else if (othCurrentPlayers[player] != null)
                    upToDatePlayers.Add(player, othCurrentPlayers[player]);
                else
                    upToDatePlayers.Add(player, new CPlayerInfo(player, String.Empty, -1, -1));

            #region ---- DEBUG PRINT ----

            if (othDebug)
            {
                debugWrite("Result of Up To Date Players:");
                foreach (String player in upToDatePlayers.Keys)
                {
                    debugWrite(String.Format("--Player [{0}]", player));
                }
            }

            #endregion ---- DEBUG PRINT ----
            return new List<CPlayerInfo>(upToDatePlayers.Values);
        }
        /// <summary>Actually move the players onto the teams as denoted by the split.</summary>
        /// <param name="split">The players that should go on team 1.</param>
        /// <param name="groups">All of the players in the server, and by omission, team 2.</param>
        private void distributePlayers(ScrambleSplit split, List<ScrambleGroup> groups)
        {
            List<CPlayerInfo> team1 = new List<CPlayerInfo>();
            List<CPlayerInfo> team2 = new List<CPlayerInfo>();

            // -- Populate team 1 from the split.
            foreach (ScrambleGroup group in split.Groups)
                foreach (CPlayerInfo player in group.Players)
                    team1.Add(player);
            // -- Populate team 2 by ommision.
            foreach (ScrambleGroup group in groups)
                foreach (CPlayerInfo player in group.Players)
                    if (!team1.Contains(player))
                        team2.Add(player);

            // -- Move the players onto their new teams.
            foreach (CPlayerInfo player in team1)
                movePlayer(player.SoldierName, 1);
            foreach (CPlayerInfo player in team2)
                movePlayer(player.SoldierName, 2);

            #region ---- DEBUG PRINT ----

            if (othDebug)
            {
                debugWrite("Result of Scramble/Distribution:");
                debugWrite("--Team 1:");
                foreach (CPlayerInfo player in team1)
                    debugWrite(String.Format("----Player [{0}]", player.SoldierName));
                debugWrite("--Team 2:");
                foreach (CPlayerInfo player in team2)
                    debugWrite(String.Format("----Player [{0}]", player.SoldierName));
            }

            #endregion ---- DEBUG PRINT ----
        }


        /// <summary>Randomizes the order of the groups passed in.</summary>
        private List<ScrambleGroup> randomizeGroups(List<ScrambleGroup> groups)
        {
            List<ScrambleGroup> groupsCopy = new List<ScrambleGroup>(groups);
            List<ScrambleGroup> groupsRand = new List<ScrambleGroup>();

            // Iterate over the copy until there are no groups left.
            Int32 swap;
            while (groupsCopy.Count > 0)
            {
                swap = scrRandom.Next(groupsCopy.Count);
                groupsRand.Add(groupsCopy[swap]);
                groupsCopy.RemoveAt(swap);
            }

            // Send back the randomized list of groups.
            return groupsRand;
        }


        /// <summary>Calculates the percent match of a substring in another string. The shortest string is used as the substring.</summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <returns>The percent match.</returns>
        private Double calcPercentMatch(String s1, String s2)
        {
            // Variables
            double max;
            double min;
            int levDist;

            // Always use the longest string first (for Levinshtein Distance).
            if (s1.Length >= s2.Length)
            {
                max = s1.Length;
                min = s2.Length;
                levDist = calcLevenshteinDistance(s1, s2);
            }
            else
            {
                max = s2.Length;
                min = s1.Length;
                levDist = calcLevenshteinDistance(s2, s1);
            }

            double percent = (max - levDist) / max;
            double maxPossMatch = min / max;

            // Calc percent to which the string matched.
            // Calc largest possible match (i.e difference in length, if larger string contained exact substring match).
            // Use relative match.
            // Example:
            //  Bob -> B0bWuzHere
            //  Percent match:    (10 - 8) / 10 = 20%
            //  Max poss match:   3 / 10        = 30%
            //  Relative percent: 20% / 30%     = 66.6%
            return (percent / maxPossMatch) * 100;

            // Another Algorithm works as follows:
            // Calc percent to which the string matched.
            // Calc largest possible match (i.e difference in length, if larger string contained exact substring match).
            // Assume 100% match, then remove the relative amount the strings didn't match.
            // Example:
            //  Bob -> B0bWuzHere
            //  Percent match:  (10 - 8) / 10      = 20%
            //  Max poss match: 3 / 10             = 30%
            //  Assume 100%:    100% - (30% - 20%) = 90%
            //return 100 - (maxPossMatch - percent) * 100;
        }
        /// <summary>Returns the number of changes needed to be made to the first string to turn it into the second string.</summary>
        /// <param name="s1">The first string.  Note: This string needs to be the longer of the two.</param>
        /// <param name="s2">The second string.  Note: This string needs to be the shorter of the two.</param>
        /// <returns>The number of changes needed to be made to the first string.</returns>
        private Int32 calcLevenshteinDistance(String s1, String s2)
        {
            // [0] Length of S1.
            // [1] Length of S2.
            // [2] Where we're at in S1.
            // [3] Where we're at in S2.
            // [4] The Character at posS1 in S1.
            // [5] The Character at posS2 in S2.
            int lenS1, lenS2;
            int posS1, posS2;
            String chrS1, chrS2;

            // Exit early if one is an empty string.
            lenS1 = s1.Length;
            lenS2 = s2.Length;
            if (lenS1 == 0)
                return lenS2;
            if (lenS2 == 0)
                return lenS1;

            // Build a matrix.
            // Fill first row with 0,1,2,3,4,5...
            // Fill first column with 0,1,2,3,4,5...
            int[][] matrix = new int[lenS1][];
            for (int i = 0; i < lenS1; i++)
                matrix[i] = new int[lenS2];

            for (int i = 0; i < lenS1; i++)
                matrix[i][0] = i;

            for (int i = 0; i < lenS2; i++)
                matrix[0][i] = i;


            // Start filling the matrix.
            // For each character in the first string...
            for (posS1 = 1; posS1 < lenS1; posS1++)
            {
                chrS1 = s1.Substring(posS1, 1);

                // For each character in the second string...
                for (posS2 = 1; posS2 < lenS2; posS2++)
                {
                    chrS2 = s2.Substring(posS2, 1);

                    // Determine if they are equal.
                    int cost = 0;
                    if (chrS1 != chrS2)
                        cost = 1;

                    // Calculate which value should be put in this spot of the matrix.
                    int val1 = matrix[posS1 - 1][posS2] + 1;
                    int val2 = matrix[posS1][posS2 - 1] + 1;
                    int val3 = matrix[posS1 - 1][posS2 - 1] + cost;
                    matrix[posS1][posS2] = Math.Min(Math.Min(val1, val2), val3);
                }
            }

            // Return the number of additions/removals/changes it would take for the first string to match the second string.
            return matrix[lenS1 - 1][lenS2 - 1];
        }
    }
}
