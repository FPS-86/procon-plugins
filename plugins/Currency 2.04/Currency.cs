/*  Copyright 2010 blactionhero, via http://www.Phogue.net

    This file is part of Currency for PRoCon.

    Currency for PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Currency for PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    For any further questions, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
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

namespace PRoConEvents
{
    public class Currency : PRoConPluginAPI, IPRoConPluginInterface3
    {
        #region Plugin variable declarations and assignments

        private enumBoolYesNo m_ebynEnableAutoBounties;
        private enumBoolYesNo m_ebynPublicAutoBountyNotifications;
        private enumBoolYesNo m_ebynSayAutoBountyNotifications;
        private enumBoolYesNo m_ebynYellAutoBountyNotifications;
        private enumBoolYesNo m_ebynEnablePlayerBounties;
        private enumBoolYesNo m_ebynPublicPlayerBountyNotifications;
        private enumBoolYesNo m_ebynSayPlayerBountyNotifications;
        private enumBoolYesNo m_ebynYellPlayerBountyNotifications;
        private enumBoolYesNo m_ebynEnableMugging;
        private enumBoolYesNo m_ebynEnableMuggingLimit;
        private enumBoolYesNo m_ebynSayStrMugged;
        private enumBoolYesNo m_ebynYellStrMugged;
        private enumBoolYesNo m_ebynSpamPluginConsole;
        private enumBoolYesNo m_ebynAllowAdminPrivileges;
        private enumBoolYesNo m_ebynEnablePayroll;
        private enumBoolYesNo m_ebynPayForKills;
        private enumBoolYesNo m_ebynDeductForDeaths;
        private enumBoolYesNo m_ebynPayForSpecificWeaponKills;
        private enumBoolYesNo m_ebynPayForHeadshots;

        private int m_iAutoBountyStreak;
        private int m_iAutoBountyStartingBounty;
        private int m_iAutoBountyIncreasePerKill;
        private int m_iAutoBountyNotificationsYellLength;
        private int m_iPlayerBountyNotificationsYellLength;
        private int m_iMuggingPercentStolen;
        private int m_iMuggingLimit;
        private int m_iStrMuggedYellLength;
        private int m_iItemCount;
        private int m_iPayrollAmount;
        private int m_iPayrollFrequency;
        private int m_iAmountPaidPerKill;
        private int m_iAmountLostPerDeath;
        private int m_iAmountPaidPerHeadshot;
        private int m_iSpecificWeaponPay;
        private int m_iPreviousStampIndex;

        private string m_strAutoBountyPosted;
        private string m_strAutoBountyCollected;
        private string m_strCurrentGameMode;
        private string m_strPlayerBountyPostCommand;
        private string m_strPlayerBountyPosted;
        private string m_strPlayerBountyCollected;
        private string m_strBountyBoardCommand;
        private string m_strMugged;
        private string m_strMuggingWeaponChosen;
        private string m_strSelectedConfirmMuggingWeaponChosen;
        private string m_strPreviousMessage;
        private string m_strCurrencyInstanceName;
        private string m_strCurrencySymbol;
        private string m_strAdminAddCurrencyCommand;
        private string m_strAdminRemoveCurrencyCommand;
        private string m_strAdminCheckBalanceCommand;
        private string m_strSQLHostname;
        private string m_strSQLDatabaseName;
        private string m_strSQLUsername;
        private string m_strSQLPassword;
        private string m_strPayrollMessage;
        private string m_strMainMenuTrigger;
        private string m_strShopTrigger;
        private string m_strCheckBalanceTrigger;
        private string m_strPlayerPersonalSpamTrigger;
        private string m_strSpecificWeaponChosen;
        private string m_strSelectedConfirmWeaponChosen;
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private string[] m_straMainMenu;
        private string[] m_straShop;

        private DateTime m_dtPreviousEntryStamp;

        private List<string> m_lstMuggingWeapons;
        private List<string> m_lstWeaponAndPayValue;
        private List<string> m_lstNoSpam;

        private Dictionary<string, CPlayerInfo> m_dicPlayersAndInfo = new Dictionary<string, CPlayerInfo>();
        private Dictionary<string, int> m_dicPlayersAndBalances = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicOldPlayersAndBalances = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicPayrollSchedule = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicPlayerStreaks = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicPlayersAndBounties = new Dictionary<string, int>();
        private Dictionary<int, Item> m_dicItemsForSale = new Dictionary<int, Item>();
        private Dictionary<string, int> m_dicSquadsAndPopulations = new Dictionary<string, int>();

        private System.Data.Odbc.OdbcConnection OdbcCon = new System.Data.Odbc.OdbcConnection("DSN=Currency");
        private System.Data.Odbc.OdbcCommand OdbcCom;
        private System.Data.Odbc.OdbcDataReader OdbcDR;

        public Currency()
        {
            this.m_ebynEnableAutoBounties = enumBoolYesNo.No;
            this.m_ebynPublicAutoBountyNotifications = enumBoolYesNo.Yes;
            this.m_ebynSayAutoBountyNotifications = enumBoolYesNo.Yes;
            this.m_ebynYellAutoBountyNotifications = enumBoolYesNo.No;
            this.m_ebynEnablePlayerBounties = enumBoolYesNo.No;
            this.m_ebynPublicPlayerBountyNotifications = enumBoolYesNo.Yes;
            this.m_ebynSayPlayerBountyNotifications = enumBoolYesNo.Yes;
            this.m_ebynYellPlayerBountyNotifications = enumBoolYesNo.No;
            this.m_ebynEnableMugging = enumBoolYesNo.No;
            this.m_ebynEnableMuggingLimit = enumBoolYesNo.No;
            this.m_ebynSayStrMugged = enumBoolYesNo.Yes;
            this.m_ebynYellStrMugged = enumBoolYesNo.No;
            this.m_ebynSpamPluginConsole = enumBoolYesNo.Yes;
            this.m_ebynAllowAdminPrivileges = enumBoolYesNo.No;
            this.m_ebynEnablePayroll = enumBoolYesNo.No;
            this.m_ebynPayForKills = enumBoolYesNo.No;
            this.m_ebynDeductForDeaths = enumBoolYesNo.No;
            this.m_ebynPayForSpecificWeaponKills = enumBoolYesNo.No;
            this.m_ebynPayForHeadshots = enumBoolYesNo.No;

            this.m_iAutoBountyStreak = 5;
            this.m_iAutoBountyStartingBounty = 2;
            this.m_iAutoBountyIncreasePerKill = 1;
            this.m_iAutoBountyNotificationsYellLength = 3;
            this.m_iPlayerBountyNotificationsYellLength = 3;
            this.m_iMuggingPercentStolen = 1;
            this.m_iMuggingLimit = 50;
            this.m_iStrMuggedYellLength = 5;
            this.m_iItemCount = 0;
            this.m_iPayrollAmount = 500;
            this.m_iPayrollFrequency = 30;
            this.m_iAmountPaidPerKill = 0;
            this.m_iAmountLostPerDeath = 0;
            this.m_iAmountPaidPerHeadshot = 0;
            this.m_iSpecificWeaponPay = 0;

            this.m_strAutoBountyPosted = "A %amount CC$ bounty has been put on %killer's head for her %streak kill streak!";
            this.m_strAutoBountyCollected = "%killer has ended %victim's %streak kill streak, claiming the %amount CC$ bounty!";
            this.m_strCurrentGameMode = "";
            this.m_strPlayerBountyPostCommand = "@addbounty";
            this.m_strPlayerBountyPosted = "%poster just put a %amount CC$ bounty on %target's head!";
            this.m_strPlayerBountyCollected = "%killer just murked %victim, claiming her %amount CC$ bounty!";
            this.m_strBountyBoardCommand = "@bountyboard";
            this.m_strMugged = "%killer just mugged %victim, stealing %amount CC$!";
            this.m_strMuggingWeaponChosen = "";
            this.m_strSelectedConfirmWeaponChosen = "";
            this.m_strCurrencyInstanceName = "Currency";
            this.m_strCurrencySymbol = "CC$";
            this.m_strPayrollMessage = "Payroll - You just got paid %amount CC$!";
            this.m_strMainMenuTrigger = "@currency";
            this.m_strAdminAddCurrencyCommand = "@addcc";
            this.m_strAdminRemoveCurrencyCommand = "@removecc";
            this.m_strAdminCheckBalanceCommand = "@checkbalance";
            this.m_strSQLHostname = "";
            this.m_strSQLDatabaseName = "";
            this.m_strSQLUsername = "";
            this.m_strSQLPassword = "";
            this.m_strShopTrigger = "@shop";
            this.m_strCheckBalanceTrigger = "@balance";
            this.m_strPlayerPersonalSpamTrigger = "@quiet";
            this.m_strSpecificWeaponChosen = "";
            this.m_strSelectedConfirmMuggingWeaponChosen = "";
            this.m_straMainMenu = new string[] { "This is the main menu!" };
            this.m_straShop = new string[] { "This is the shop!" };
            this.m_strHostName = "";
            this.m_strPort = "";
            this.m_strPRoConVersion = "";

            this.m_dtPreviousEntryStamp = DateTime.Now;

            this.m_lstMuggingWeapons = new List<string>();
            this.m_lstWeaponAndPayValue = new List<string>();
            this.m_lstNoSpam = new List<string>();
        }
        #endregion

        #region Plugin information

        public string GetPluginName()
        {
            return "Currency";
        }

        public string GetPluginVersion()
        {
            return "v2.04";
        }

        public string GetPluginAuthor()
        {
            return "blactionhero";
        }

        public string GetPluginWebsite()
        {
            return "http://phogue.net/forum/viewtopic.php?f=18&t=1338";
        }

        public string GetPluginDescription()
        {
            return @"
			
<h2><u>Description</u></h2>

<p>Please see this plugin's full documentation at Phogue.net: http://phogue.net/forum/viewtopic.php?f=18&t=1338
";
        }

        #endregion

        #region PRoCon variables and Item class

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnLevelStarted", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnServerInfo", "OnPlayerKilled", "OnPlayerLeft", "OnPluginEnable", "OnPluginDisable", "OnPlayerKicked", "OnPlayerTeamChange", "OnPlayerSquadChange");
        }

        private class Item
        {
            private int _id;
            private string _name;
            private string _description;
            public enumBoolYesNo Enabled;
            private string _buycommand;
            private int _ccvalue;
            private string _templatechosen;
            private List<string> _payload;

            public Item()
            {
                this._id = 0;
                this._name = "Item";
                this.Enabled = enumBoolYesNo.No;
                this._ccvalue = 100;
                this._buycommand = "Enter the chat trigger used to buy this item";
                this._description = "Enter a short description and show proper syntax";
                this._templatechosen = "";
                this._payload = new List<string>();
            }

            public Item(int itemID)
            {
                this._id = itemID;
                this._name = String.Format("Item {0}", itemID);
                this.Enabled = enumBoolYesNo.No;
                this._ccvalue = 100;
                this._buycommand = "Enter the chat trigger used to buy this item";
                this._description = "Enter a short description and show proper syntax";
                this._templatechosen = "";
                this._payload = new List<string>();
            }

            public Item(string strName)
            {
                this._id = 0;
                this._name = "Item";
                this.Enabled = enumBoolYesNo.No;
                this._ccvalue = 100;
                this._buycommand = "Enter the chat trigger used to buy this item";
                this._description = "Enter a short description and show proper syntax";
                this._templatechosen = "";
                this._payload = new List<string>();
            }

            public int ID
            {
                set
                {
                    _id = value;
                }
                get
                {
                    return _id;
                }
            }
            public string Name
            {
                set
                {
                    _name = value;
                }
                get
                {
                    return _name;
                }
            }
            public string Description
            {
                set
                {
                    _description = value;
                }
                get
                {
                    return _description;
                }
            }
            public string BuyCommand
            {
                set
                {
                    _buycommand = value;

                }
                get
                {
                    return _buycommand;
                }
            }
            public int Cost
            {
                set
                {
                    _ccvalue = value;
                }
                get
                {
                    return _ccvalue;
                }
            }
            public string TemplateChosen
            {
                set
                {
                    _templatechosen = value;
                }
                get
                {
                    return _templatechosen;
                }
            }
            public List<string> Payload
            {
                set
                {
                    _payload = value;
                }
                get
                {
                    return _payload;
                }
            }
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1. General Options|Rename Currency to:", this.m_strCurrencyInstanceName.GetType(), this.m_strCurrencyInstanceName));
            lstReturn.Add(new CPluginVariable("1. General Options|Currency symbol:", this.m_strCurrencySymbol.GetType(), this.m_strCurrencySymbol));
            lstReturn.Add(new CPluginVariable("1. General Options|Show plugin console spam? (recommended)", typeof(enumBoolYesNo), this.m_ebynSpamPluginConsole));
            lstReturn.Add(new CPluginVariable("1. General Options|Main menu chat trigger:", this.m_strMainMenuTrigger.GetType(), this.m_strMainMenuTrigger));
            lstReturn.Add(new CPluginVariable("1. General Options|    Main menu header:", this.m_straMainMenu.GetType(), this.m_straMainMenu));
            lstReturn.Add(new CPluginVariable("1. General Options|Shop menu chat trigger:", this.m_strShopTrigger.GetType(), this.m_strShopTrigger));
            lstReturn.Add(new CPluginVariable("1. General Options|    Shop menu header:", this.m_straShop.GetType(), this.m_straShop));
            lstReturn.Add(new CPluginVariable("1. General Options|Check balance chat trigger:", this.m_strCheckBalanceTrigger.GetType(), this.m_strCheckBalanceTrigger));
            lstReturn.Add(new CPluginVariable("1. General Options|Toggle notifications chat trigger:", this.m_strPlayerPersonalSpamTrigger.GetType(), this.m_strPlayerPersonalSpamTrigger));
            lstReturn.Add(new CPluginVariable("1. General Options|Grant admins special privileges?", typeof(enumBoolYesNo), this.m_ebynAllowAdminPrivileges));

            if (m_ebynAllowAdminPrivileges == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("1. General Options|    Admin check player's balance command:", this.m_strAdminCheckBalanceCommand.GetType(), this.m_strAdminCheckBalanceCommand));
                lstReturn.Add(new CPluginVariable("1. General Options|    Admin add " + m_strCurrencySymbol + " to player's account command:", this.m_strAdminAddCurrencyCommand.GetType(), this.m_strAdminAddCurrencyCommand));
                lstReturn.Add(new CPluginVariable("1. General Options|    Admin remove " + m_strCurrencySymbol + " from player's account command:", this.m_strAdminRemoveCurrencyCommand.GetType(), this.m_strAdminRemoveCurrencyCommand));
            }

            lstReturn.Add(new CPluginVariable("2. MySQL Server Config|Hostname/IP:", this.m_strSQLHostname.GetType(), this.m_strSQLHostname));
            lstReturn.Add(new CPluginVariable("2. MySQL Server Config|Database Name:", this.m_strSQLDatabaseName.GetType(), this.m_strSQLDatabaseName));
            lstReturn.Add(new CPluginVariable("2. MySQL Server Config|UserName:", this.m_strSQLUsername.GetType(), this.m_strSQLUsername));
            lstReturn.Add(new CPluginVariable("2. MySQL Server Config|Password:", this.m_strSQLPassword.GetType(), this.m_strSQLPassword));

            lstReturn.Add(new CPluginVariable("3. Payroll Config|Enable payroll?", typeof(enumBoolYesNo), this.m_ebynEnablePayroll));

            if (m_ebynEnablePayroll == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("3. Payroll Config|    Payroll frequency (in minutes):", this.m_iPayrollFrequency.GetType(), this.m_iPayrollFrequency));
                lstReturn.Add(new CPluginVariable("3. Payroll Config|    Payroll amount:", this.m_iPayrollAmount.GetType(), this.m_iPayrollAmount));
                lstReturn.Add(new CPluginVariable("3. Payroll Config|    Message sent to player when paid:", this.m_strPayrollMessage.GetType(), this.m_strPayrollMessage));
            }

            lstReturn.Add(new CPluginVariable("4. Kills and Deaths|Pay for kills?", typeof(enumBoolYesNo), this.m_ebynPayForKills));

            if (m_ebynPayForKills == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("4. Kills and Deaths|    Amount paid per kill:", this.m_iAmountPaidPerKill.GetType(), this.m_iAmountPaidPerKill));
            }

            lstReturn.Add(new CPluginVariable("4. Kills and Deaths|Deduct for deaths?", typeof(enumBoolYesNo), this.m_ebynDeductForDeaths));

            if (m_ebynDeductForDeaths == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("4. Kills and Deaths|    Amount deducted per death:", this.m_iAmountLostPerDeath.GetType(), this.m_iAmountLostPerDeath));
            }

            lstReturn.Add(new CPluginVariable("4. Kills and Deaths|Pay bonus for headshots?", typeof(enumBoolYesNo), this.m_ebynPayForHeadshots));

            if (m_ebynPayForHeadshots == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("4. Kills and Deaths|    Bonus paid per headshot:", this.m_iAmountPaidPerHeadshot.GetType(), this.m_iAmountPaidPerHeadshot));
            }

            lstReturn.Add(new CPluginVariable("4. Kills and Deaths|Pay for kills with specific weapons?", typeof(enumBoolYesNo), this.m_ebynPayForSpecificWeaponKills));

            if (m_ebynPayForSpecificWeaponKills == enumBoolYesNo.Yes)
            {
                lstReturn.Add(this.GetWeaponListPluginVariable("4. Kills and Deaths|    Select a weapon and enter its kill value below:", "KillStreakWeaponList", this.m_strSpecificWeaponChosen, DamageTypes.None));
                lstReturn.Add(new CPluginVariable("4. Kills and Deaths|    Enter selected weapon's kill value, then select Add below:", this.m_iSpecificWeaponPay.GetType(), this.m_iSpecificWeaponPay));
                lstReturn.Add(new CPluginVariable("4. Kills and Deaths|    Add or remove selected weapon and kill value to or from list?", "enum.AddRemoveWeaponTracker(Select an action|Add|Remove)", this.m_strSelectedConfirmWeaponChosen));
                lstReturn.Add(new CPluginVariable("4. Kills and Deaths|    List of weapons and corresponding " + m_strCurrencySymbol + " values:", typeof(string[]), this.m_lstWeaponAndPayValue.ToArray()));
            }

            if (m_ebynEnableAutoBounties == enumBoolYesNo.Yes || m_ebynEnablePlayerBounties == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("5. Bounties|Command used to display the Bounty Board:", this.m_strBountyBoardCommand.GetType(), this.m_strBountyBoardCommand));
            }

            lstReturn.Add(new CPluginVariable("5. Bounties|Enable automatic bounties?", typeof(enumBoolYesNo), this.m_ebynEnableAutoBounties));

            if (m_ebynEnableAutoBounties == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Number of kills needed to start bounty:", this.m_iAutoBountyStreak.GetType(), this.m_iAutoBountyStreak));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Starting bounty amount:", this.m_iAutoBountyStartingBounty.GetType(), this.m_iAutoBountyStartingBounty));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Amount added to bounty for additional kills:", this.m_iAutoBountyIncreasePerKill.GetType(), this.m_iAutoBountyIncreasePerKill));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Message sent when a bounty is posted:", this.m_strAutoBountyPosted.GetType(), this.m_strAutoBountyPosted));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Message sent when a bounty is collected:", this.m_strAutoBountyCollected.GetType(), this.m_strAutoBountyCollected));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Send messages to everyone?", typeof(enumBoolYesNo), this.m_ebynPublicAutoBountyNotifications));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Say the messages above?", typeof(enumBoolYesNo), this.m_ebynSayAutoBountyNotifications));
                lstReturn.Add(new CPluginVariable("5. Bounties|    AutoBounty: Yell the messages above?", typeof(enumBoolYesNo), this.m_ebynYellAutoBountyNotifications));

                if (m_ebynYellAutoBountyNotifications == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("5. Bounties|        AutoBounty: Yell them for how long (seconds)?", this.m_iAutoBountyNotificationsYellLength.GetType(), this.m_iAutoBountyNotificationsYellLength));
                }
            }

            lstReturn.Add(new CPluginVariable("5. Bounties|Enable player-made bounties?", typeof(enumBoolYesNo), this.m_ebynEnablePlayerBounties));

            if (m_ebynEnablePlayerBounties == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("5. Bounties|    PlayerBounty: Command players use to add bounties:", this.m_strPlayerBountyPostCommand.GetType(), this.m_strPlayerBountyPostCommand));
                lstReturn.Add(new CPluginVariable("5. Bounties|    PlayerBounty: Message sent when a bounty is posted:", this.m_strPlayerBountyPosted.GetType(), this.m_strPlayerBountyPosted));
                lstReturn.Add(new CPluginVariable("5. Bounties|    PlayerBounty: Message sent when a bounty is collected:", this.m_strPlayerBountyCollected.GetType(), this.m_strPlayerBountyCollected));
                lstReturn.Add(new CPluginVariable("5. Bounties|    PlayerBounty: Send messages to everyone?", typeof(enumBoolYesNo), this.m_ebynPublicPlayerBountyNotifications));
                lstReturn.Add(new CPluginVariable("5. Bounties|    PlayerBounty: Say the messages?", typeof(enumBoolYesNo), this.m_ebynSayPlayerBountyNotifications));
                lstReturn.Add(new CPluginVariable("5. Bounties|    PlayerBounty: Yell the messages?", typeof(enumBoolYesNo), this.m_ebynYellPlayerBountyNotifications));

                if (m_ebynYellPlayerBountyNotifications == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("5. Bounties|        PlayerBounty: Yell messages for how long (seconds)?", this.m_iPlayerBountyNotificationsYellLength.GetType(), this.m_iPlayerBountyNotificationsYellLength));
                }
            }

            lstReturn.Add(new CPluginVariable("6. Mugging|Enable mugging?", typeof(enumBoolYesNo), this.m_ebynEnableMugging));

            if (m_ebynEnableMugging == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("6. Mugging|    Percentage of victim's " + m_strCurrencySymbol + " stolen when mugged:", this.m_iMuggingPercentStolen.GetType(), this.m_iMuggingPercentStolen));
                lstReturn.Add(new CPluginVariable("6. Mugging|    Limit the amount of " + m_strCurrencySymbol + " that can be stolen per mugging?", typeof(enumBoolYesNo), this.m_ebynEnableMuggingLimit));

                if (m_ebynEnableMuggingLimit == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("6. Mugging|        Maximum amount of " + m_strCurrencySymbol + " that can be stolen:", this.m_iMuggingLimit.GetType(), this.m_iMuggingLimit));
                }

                lstReturn.Add(new CPluginVariable("6. Mugging|    Message sent to everyone when someone is mugged:", this.m_strMugged.GetType(), this.m_strMugged));
                lstReturn.Add(new CPluginVariable("6. Mugging|    Say the message above?", typeof(enumBoolYesNo), this.m_ebynSayStrMugged));
                lstReturn.Add(new CPluginVariable("6. Mugging|    Yell the message above?", typeof(enumBoolYesNo), this.m_ebynYellStrMugged));

                if (m_ebynYellStrMugged == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("6. Mugging|        Yell message for how long (seconds)?", this.m_iStrMuggedYellLength.GetType(), this.m_iStrMuggedYellLength));
                }

                lstReturn.Add(this.GetWeaponListPluginVariable("6. Mugging|    Select a mugging weapon:", "KillStreakWeaponList", this.m_strMuggingWeaponChosen, DamageTypes.None));
                lstReturn.Add(new CPluginVariable("6. Mugging|    Add or remove selected weapon to or from the list?", "enum.AddRemoveWeaponTracker(Select an action|Add|Remove)", this.m_strSelectedConfirmMuggingWeaponChosen));
                lstReturn.Add(new CPluginVariable("6. Mugging|    List of mugging weapons:", typeof(string[]), this.m_lstMuggingWeapons.ToArray()));
            }

            lstReturn.Add(new CPluginVariable("7. Shop Config|Number of items:", this.m_iItemCount.GetType(), this.m_iItemCount));

            if (this.m_dicItemsForSale.Count < this.m_iItemCount)
            {
                do
                {
                    this.m_dicItemsForSale.Add(m_dicItemsForSale.Count + 1, new Item(m_dicItemsForSale.Count + 1));
                }
                while (this.m_dicItemsForSale.Count < this.m_iItemCount);
            }

            else if (this.m_dicItemsForSale.Count > this.m_iItemCount)
            {
                do
                {
                    this.m_dicItemsForSale.Remove(m_dicItemsForSale.Count);
                }
                while (this.m_dicItemsForSale.Count > this.m_iItemCount);
            }

            foreach (Item item in m_dicItemsForSale.Values)
            {
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} - Name:", item.ID), item.Name.GetType(), item.Name));
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} - Enabled:", item.ID), typeof(enumBoolYesNo), item.Enabled));
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} - Cost:", item.ID), item.Cost.GetType(), item.Cost));
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} - Buy command:", item.ID), item.BuyCommand.GetType(), item.BuyCommand));
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} - Description:", item.ID), item.Description.GetType(), item.Description));
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} - Add a console command to run when bought:", item.ID), "enum.AddRemoveCommandTemplate(Select a command template to add it to the list below|Say|Whisper|Yell|Growl|Move|Mute|Voice|Kill|Nuke|Kick|TempBan|PermBan|UnBan|RestartRound|NextLevel|ReserveAdd|ReserveRemove)", item.TemplateChosen));
                lstReturn.Add(new CPluginVariable(String.Format("7. Shop Config: Item {0}|Item {0} -    List of console commands for Item {0}:", item.ID), typeof(string[]), item.Payload.ToArray()));
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Rename Currency to:", this.m_strCurrencyInstanceName.GetType(), this.m_strCurrencyInstanceName));
            lstReturn.Add(new CPluginVariable("Currency symbol:", this.m_strCurrencySymbol.GetType(), this.m_strCurrencySymbol));
            lstReturn.Add(new CPluginVariable("Show plugin console spam? (recommended)", typeof(enumBoolYesNo), this.m_ebynSpamPluginConsole));
            lstReturn.Add(new CPluginVariable("Main menu chat trigger:", this.m_strMainMenuTrigger.GetType(), this.m_strMainMenuTrigger));
            lstReturn.Add(new CPluginVariable("    Main menu header:", this.m_straMainMenu.GetType(), this.m_straMainMenu));
            lstReturn.Add(new CPluginVariable("Shop menu chat trigger:", this.m_strShopTrigger.GetType(), this.m_strShopTrigger));
            lstReturn.Add(new CPluginVariable("    Shop menu header:", this.m_straShop.GetType(), this.m_straShop));
            lstReturn.Add(new CPluginVariable("Check balance chat trigger:", this.m_strCheckBalanceTrigger.GetType(), this.m_strCheckBalanceTrigger));
            lstReturn.Add(new CPluginVariable("Toggle notifications chat trigger:", this.m_strPlayerPersonalSpamTrigger.GetType(), this.m_strPlayerPersonalSpamTrigger));
            lstReturn.Add(new CPluginVariable("Grant admins special privileges?", typeof(enumBoolYesNo), this.m_ebynAllowAdminPrivileges));

            if (m_ebynAllowAdminPrivileges == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("    Admin check player's balance command:", this.m_strAdminCheckBalanceCommand.GetType(), this.m_strAdminCheckBalanceCommand));
                lstReturn.Add(new CPluginVariable("    Admin add " + m_strCurrencySymbol + " to player's account command:", this.m_strAdminAddCurrencyCommand.GetType(), this.m_strAdminAddCurrencyCommand));
                lstReturn.Add(new CPluginVariable("    Admin remove " + m_strCurrencySymbol + " from player's account command:", this.m_strAdminRemoveCurrencyCommand.GetType(), this.m_strAdminRemoveCurrencyCommand));
            }

            lstReturn.Add(new CPluginVariable("Hostname/IP:", this.m_strSQLHostname.GetType(), this.m_strSQLHostname));
            lstReturn.Add(new CPluginVariable("Database Name:", this.m_strSQLDatabaseName.GetType(), this.m_strSQLDatabaseName));
            lstReturn.Add(new CPluginVariable("UserName:", this.m_strSQLUsername.GetType(), this.m_strSQLUsername));
            lstReturn.Add(new CPluginVariable("Password:", this.m_strSQLPassword.GetType(), this.m_strSQLPassword));

            lstReturn.Add(new CPluginVariable("Enable payroll?", typeof(enumBoolYesNo), this.m_ebynEnablePayroll));

            if (m_ebynEnablePayroll == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("    Payroll frequency (in minutes):", this.m_iPayrollFrequency.GetType(), this.m_iPayrollFrequency));
                lstReturn.Add(new CPluginVariable("    Payroll amount:", this.m_iPayrollAmount.GetType(), this.m_iPayrollAmount));
                lstReturn.Add(new CPluginVariable("    Message sent to player when paid:", this.m_strPayrollMessage.GetType(), this.m_strPayrollMessage));
            }

            lstReturn.Add(new CPluginVariable("Pay for kills?", typeof(enumBoolYesNo), this.m_ebynPayForKills));
            lstReturn.Add(new CPluginVariable("    Amount paid per kill:", this.m_iAmountPaidPerKill.GetType(), this.m_iAmountPaidPerKill));
            lstReturn.Add(new CPluginVariable("Deduct for deaths?", typeof(enumBoolYesNo), this.m_ebynDeductForDeaths));
            lstReturn.Add(new CPluginVariable("    Amount deducted per death:", this.m_iAmountLostPerDeath.GetType(), this.m_iAmountLostPerDeath));
            lstReturn.Add(new CPluginVariable("Pay bonus for headshots?", typeof(enumBoolYesNo), this.m_ebynPayForHeadshots));
            lstReturn.Add(new CPluginVariable("    Bonus paid per headshot:", this.m_iAmountPaidPerHeadshot.GetType(), this.m_iAmountPaidPerHeadshot));
            lstReturn.Add(new CPluginVariable("Pay for kills with specific weapons?", typeof(enumBoolYesNo), this.m_ebynPayForSpecificWeaponKills));

            if (m_ebynPayForSpecificWeaponKills == enumBoolYesNo.Yes)
            {
                lstReturn.Add(this.GetWeaponListPluginVariable("    Select a weapon and enter its kill value below:", "KillStreakWeaponList", this.m_strSpecificWeaponChosen, DamageTypes.None));
                lstReturn.Add(new CPluginVariable("    Enter selected weapon's kill value, then select Add below:", this.m_iSpecificWeaponPay.GetType(), this.m_iSpecificWeaponPay));
                lstReturn.Add(new CPluginVariable("    Add or remove selected weapon and kill value to or from list?", "enum.AddRemoveWeaponTracker(Select an action|Add|Remove)", this.m_strSelectedConfirmWeaponChosen));
                lstReturn.Add(new CPluginVariable("    List of weapons and corresponding " + m_strCurrencySymbol + " values:", typeof(string[]), this.m_lstWeaponAndPayValue.ToArray()));
            }

            if (m_ebynEnableAutoBounties == enumBoolYesNo.Yes || m_ebynEnablePlayerBounties == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Command used to display the Bounty Board:", this.m_strBountyBoardCommand.GetType(), this.m_strBountyBoardCommand));
            }

            lstReturn.Add(new CPluginVariable("Enable automatic bounties?", typeof(enumBoolYesNo), this.m_ebynEnableAutoBounties));

            if (m_ebynEnableAutoBounties == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("    AutoBounty: Number of kills needed to start bounty:", this.m_iAutoBountyStreak.GetType(), this.m_iAutoBountyStreak));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Starting bounty amount:", this.m_iAutoBountyStartingBounty.GetType(), this.m_iAutoBountyStartingBounty));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Amount added to bounty for additional kills:", this.m_iAutoBountyIncreasePerKill.GetType(), this.m_iAutoBountyIncreasePerKill));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Message sent when a bounty is posted:", this.m_strAutoBountyPosted.GetType(), this.m_strAutoBountyPosted));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Message sent when a bounty is collected:", this.m_strAutoBountyCollected.GetType(), this.m_strAutoBountyCollected));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Send messages to everyone?", typeof(enumBoolYesNo), this.m_ebynPublicAutoBountyNotifications));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Say the messages above?", typeof(enumBoolYesNo), this.m_ebynSayAutoBountyNotifications));
                lstReturn.Add(new CPluginVariable("    AutoBounty: Yell the messages above?", typeof(enumBoolYesNo), this.m_ebynYellAutoBountyNotifications));

                if (m_ebynYellAutoBountyNotifications == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("        AutoBounty: Yell them for how long (seconds)?", this.m_iAutoBountyNotificationsYellLength.GetType(), this.m_iAutoBountyNotificationsYellLength));
                }
            }

            lstReturn.Add(new CPluginVariable("Enable player-made bounties?", typeof(enumBoolYesNo), this.m_ebynEnablePlayerBounties));

            if (m_ebynEnablePlayerBounties == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("    PlayerBounty: Command players use to add bounties:", this.m_strPlayerBountyPostCommand.GetType(), this.m_strPlayerBountyPostCommand));
                lstReturn.Add(new CPluginVariable("    PlayerBounty: Message sent when a bounty is posted:", this.m_strPlayerBountyPosted.GetType(), this.m_strPlayerBountyPosted));
                lstReturn.Add(new CPluginVariable("    PlayerBounty: Message sent when a bounty is collected:", this.m_strPlayerBountyCollected.GetType(), this.m_strPlayerBountyCollected));
                lstReturn.Add(new CPluginVariable("    PlayerBounty: Send messages to everyone?", typeof(enumBoolYesNo), this.m_ebynPublicPlayerBountyNotifications));
                lstReturn.Add(new CPluginVariable("    PlayerBounty: Say the messages?", typeof(enumBoolYesNo), this.m_ebynSayPlayerBountyNotifications));
                lstReturn.Add(new CPluginVariable("    PlayerBounty: Yell the messages?", typeof(enumBoolYesNo), this.m_ebynYellPlayerBountyNotifications));

                if (m_ebynYellPlayerBountyNotifications == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("        PlayerBounty: Yell messages for how long (seconds)?", this.m_iPlayerBountyNotificationsYellLength.GetType(), this.m_iPlayerBountyNotificationsYellLength));
                }
            }

            lstReturn.Add(new CPluginVariable("Enable mugging?", typeof(enumBoolYesNo), this.m_ebynEnableMugging));

            if (m_ebynEnableMugging == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("    Percentage of victim's " + m_strCurrencySymbol + " stolen when mugged:", this.m_iMuggingPercentStolen.GetType(), this.m_iMuggingPercentStolen));
                lstReturn.Add(new CPluginVariable("    Limit the amount of " + m_strCurrencySymbol + " that can be stolen per mugging?", typeof(enumBoolYesNo), this.m_ebynEnableMuggingLimit));

                if (m_ebynEnableMuggingLimit == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("        Maximum amount of " + m_strCurrencySymbol + " that can be stolen:", this.m_iMuggingLimit.GetType(), this.m_iMuggingLimit));
                }

                lstReturn.Add(new CPluginVariable("    Message sent to everyone when someone is mugged:", this.m_strMugged.GetType(), this.m_strMugged));
                lstReturn.Add(new CPluginVariable("    Say the message above?", typeof(enumBoolYesNo), this.m_ebynSayStrMugged));
                lstReturn.Add(new CPluginVariable("    Yell the message above?", typeof(enumBoolYesNo), this.m_ebynYellStrMugged));

                if (m_ebynYellStrMugged == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("        Yell message for how long (seconds)?", this.m_iStrMuggedYellLength.GetType(), this.m_iStrMuggedYellLength));
                }

                lstReturn.Add(this.GetWeaponListPluginVariable("    Select a mugging weapon:", "KillStreakWeaponList", this.m_strMuggingWeaponChosen, DamageTypes.None));
                lstReturn.Add(new CPluginVariable("    Add or remove selected weapon to or from the list?", "enum.AddRemoveWeaponTracker(Select an action|Add|Remove)", this.m_strSelectedConfirmMuggingWeaponChosen));
                lstReturn.Add(new CPluginVariable("    List of mugging weapons:", typeof(string[]), this.m_lstMuggingWeapons.ToArray()));
            }

            lstReturn.Add(new CPluginVariable("Number of items:", this.m_iItemCount.GetType(), this.m_iItemCount));

            foreach (Item item in m_dicItemsForSale.Values)
            {
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} - Name:", item.ID), item.Name.GetType(), item.Name));
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} - Enabled:", item.ID), typeof(enumBoolYesNo), item.Enabled));
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} - Cost:", item.ID), item.Cost.GetType(), item.Cost));
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} - Buy command:", item.ID), item.BuyCommand.GetType(), item.BuyCommand));
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} - Description:", item.ID), item.Description.GetType(), item.Description));
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} - Add a console command to run when bought:", item.ID), "enum.AddRemoveCommandTemplate(Select a command template to add it to the list below|Say|Whisper|Yell|Growl|Move|Mute|Voice|Kill|Nuke|Kick|TempBan|PermBan|UnBan|RestartRound|NextLevel|ReserveAdd|ReserveRemove)", item.TemplateChosen));
                lstReturn.Add(new CPluginVariable(String.Format("Item {0} -    List of console commands for Item {0}:", item.ID), typeof(string[]), item.Payload.ToArray()));
            }

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Rename Currency to:") == 0)
            {
                this.m_strCurrencyInstanceName = strValue;
            }

            else if (strVariable.CompareTo("Currency symbol:") == 0)
            {
                this.m_strCurrencySymbol = strValue;
            }

            else if (strVariable.CompareTo("Disable console messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynSpamPluginConsole = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("Main menu chat trigger:") == 0)
            {
                this.m_strMainMenuTrigger = strValue;
            }

            else if (strVariable.CompareTo("    Main menu header:") == 0)
            {
                this.m_straMainMenu = CPluginVariable.DecodeStringArray(strValue); ;
            }

            else if (strVariable.CompareTo("Shop menu chat trigger:") == 0)
            {
                this.m_strShopTrigger = strValue;
            }

            else if (strVariable.CompareTo("    Shop menu header:") == 0)
            {
                this.m_straShop = CPluginVariable.DecodeStringArray(strValue); ;
            }

            else if (strVariable.CompareTo("Check balance chat trigger:") == 0)
            {
                this.m_strCheckBalanceTrigger = strValue;
            }

            else if (strVariable.CompareTo("Toggle notifications chat trigger:") == 0)
            {
                this.m_strPlayerPersonalSpamTrigger = strValue;
            }

            else if (strVariable.CompareTo("Grant admins special privileges?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynAllowAdminPrivileges = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Admin check player's balance command:") == 0)
            {
                this.m_strAdminCheckBalanceCommand = strValue;
            }

            else if (strVariable.CompareTo("    Admin add " + m_strCurrencySymbol + " to player's account command:") == 0)
            {
                this.m_strAdminAddCurrencyCommand = strValue;
            }

            else if (strVariable.CompareTo("    Admin remove " + m_strCurrencySymbol + " from player's account command:") == 0)
            {
                this.m_strAdminRemoveCurrencyCommand = strValue;
            }

            else if (strVariable.CompareTo("Hostname/IP:") == 0)
            {
                this.m_strSQLHostname = strValue;
            }

            else if (strVariable.CompareTo("Database Name:") == 0)
            {
                this.m_strSQLDatabaseName = strValue;
            }

            else if (strVariable.CompareTo("UserName:") == 0)
            {
                this.m_strSQLUsername = strValue;
            }

            else if (strVariable.CompareTo("Password:") == 0)
            {
                this.m_strSQLPassword = strValue;
            }

            else if (strVariable.CompareTo("Enable payroll?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynEnablePayroll = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Payroll frequency (in minutes):") == 0)
            {
                this.m_iPayrollFrequency = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    Payroll amount:") == 0)
            {
                this.m_iPayrollAmount = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    Message sent to player when paid:") == 0)
            {
                this.m_strPayrollMessage = strValue;
            }

            else if (strVariable.CompareTo("Pay for kills?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPayForKills = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Amount paid per kill:") == 0)
            {
                this.m_iAmountPaidPerKill = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("Deduct for deaths?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynDeductForDeaths = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Amount deducted per death:") == 0)
            {
                this.m_iAmountLostPerDeath = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("Pay bonus for headshots?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPayForHeadshots = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Bonus paid per headshot:") == 0)
            {
                this.m_iAmountPaidPerHeadshot = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("Pay for kills with specific weapons?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPayForSpecificWeaponKills = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Select a weapon and enter its kill value below:") == 0)
            {
                this.m_strSpecificWeaponChosen = strValue;
            }

            else if (strVariable.CompareTo("    Enter selected weapon's kill value, then select Add below:") == 0)
            {
                this.m_iSpecificWeaponPay = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    Add or remove selected weapon and kill value to or from list?") == 0)
            {
                if (this.m_strSpecificWeaponChosen.Equals(string.Empty) == false)
                {
                    string t_strWeaponAndItsPayValue = this.m_strSpecificWeaponChosen + "=" + this.m_iSpecificWeaponPay;

                    if (strValue.Equals("Add"))
                    {
                        if (this.m_iSpecificWeaponPay > 0)
                        {
                            if (this.m_lstWeaponAndPayValue.Contains(this.m_strSpecificWeaponChosen) == false)
                            {
                                this.m_lstWeaponAndPayValue.Add(t_strWeaponAndItsPayValue);
                            }
                        }
                    }

                    else if (strValue.Equals("Remove"))
                    {
                        foreach (string line in m_lstWeaponAndPayValue)
                        {
                            if (line.StartsWith(m_strSpecificWeaponChosen) == true)
                            {
                                this.m_lstWeaponAndPayValue.Remove(t_strWeaponAndItsPayValue);
                                break;
                            }
                        }
                    }
                    this.m_lstWeaponAndPayValue.RemoveAll(string.IsNullOrEmpty);
                }
            }

            else if (strVariable.CompareTo("    List of weapons and corresponding " + m_strCurrencySymbol + " values:") == 0)
            {
                this.m_lstWeaponAndPayValue = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }

            else if (strVariable.CompareTo("Command used to display the Bounty Board:") == 0)
            {
                this.m_strBountyBoardCommand = strValue;
            }

            else if (strVariable.CompareTo("Enable automatic bounties?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynEnableAutoBounties = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    AutoBounty: Number of kills needed to start bounty:") == 0)
            {
                this.m_iAutoBountyStreak = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    AutoBounty: Starting bounty amount:") == 0)
            {
                this.m_iAutoBountyStartingBounty = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    AutoBounty: Amount added to bounty for additional kills:") == 0)
            {
                this.m_iAutoBountyIncreasePerKill = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    AutoBounty: Message sent when a bounty is posted:") == 0)
            {
                this.m_strAutoBountyPosted = strValue;
            }

            else if (strVariable.CompareTo("    AutoBounty: Message sent when a bounty is collected:") == 0)
            {
                this.m_strAutoBountyCollected = strValue;
            }

            else if (strVariable.CompareTo("    AutoBounty: Send messages to everyone?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPublicAutoBountyNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    AutoBounty: Say the messages above?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynSayAutoBountyNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    AutoBounty: Yell the messages above?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynYellAutoBountyNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("        AutoBounty: Yell them for how long (seconds)?") == 0)
            {
                this.m_iAutoBountyNotificationsYellLength = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("Enable player-made bounties?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynEnablePlayerBounties = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    PlayerBounty: Command players use to add bounties:") == 0)
            {
                this.m_strPlayerBountyPostCommand = strValue;
            }

            else if (strVariable.CompareTo("    PlayerBounty: Message sent when a bounty is posted:") == 0)
            {
                this.m_strPlayerBountyPosted = strValue;
            }

            else if (strVariable.CompareTo("    PlayerBounty: Message sent when a bounty is collected:") == 0)
            {
                this.m_strPlayerBountyCollected = strValue;
            }

            else if (strVariable.CompareTo("    PlayerBounty: Send messages to everyone?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynPublicPlayerBountyNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    PlayerBounty: Say the messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynSayPlayerBountyNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    PlayerBounty: Yell the messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynYellPlayerBountyNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("        PlayerBounty: Yell messages for how long (seconds)?") == 0)
            {
                this.m_iPlayerBountyNotificationsYellLength = (int.Parse(strValue) * 1000);
            }

            else if (strVariable.CompareTo("Enable mugging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynEnableMugging = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Percentage of victim's " + m_strCurrencySymbol + " stolen when mugged:") == 0)
            {
                this.m_iMuggingPercentStolen = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    Limit the amount of " + m_strCurrencySymbol + " that can be stolen per mugging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynEnableMuggingLimit = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("        Maximum amount of " + m_strCurrencySymbol + " that can be stolen:") == 0)
            {
                this.m_iMuggingLimit = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    Message sent to everyone when someone is mugged:") == 0)
            {
                this.m_strMugged = strValue;
            }

            else if (strVariable.CompareTo("    Say the message above?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynSayStrMugged = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("    Yell the message above?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ebynYellStrMugged = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("        Yell message for how long (seconds)?") == 0)
            {
                this.m_iStrMuggedYellLength = int.Parse(strValue);
            }

            else if (strVariable.CompareTo("    Select a mugging weapon:") == 0)
            {
                this.m_strMuggingWeaponChosen = strValue;
            }

            else if (strVariable.CompareTo("    Add or remove selected weapon to or from the list?") == 0)
            {
                if (this.m_strMuggingWeaponChosen.Equals(string.Empty) == false)
                {
                    if (strValue.Equals("Add"))
                    {
                        if (this.m_lstMuggingWeapons.Contains(this.m_strMuggingWeaponChosen) == false)
                        {
                            this.m_lstMuggingWeapons.Add(m_strMuggingWeaponChosen);
                        }
                    }

                    else if (strValue.Equals("Remove"))
                    {
                        if (this.m_lstMuggingWeapons.Contains(this.m_strMuggingWeaponChosen) == true)
                        {
                            this.m_lstMuggingWeapons.Remove(m_strMuggingWeaponChosen);
                        }
                    }
                    this.m_lstMuggingWeapons.RemoveAll(string.IsNullOrEmpty);
                }
            }

            else if (strVariable.CompareTo("    List of mugging weapons:") == 0)
            {
                this.m_lstMuggingWeapons = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }

            if (strVariable.CompareTo("Number of items:") == 0)
            {
                this.m_iItemCount = int.Parse(strValue);
            }

            int returnInt = 0;
            int itemIDFromStrVariable = (strVariable.IndexOf("-") - strVariable.IndexOf("|"));

            if (itemIDFromStrVariable > 0)
            {
                int.TryParse(strVariable.Substring(strVariable.IndexOf("|") + 6, (strVariable.IndexOf("-") - strVariable.IndexOf("|") - 6)), out returnInt);
                Item item = m_dicItemsForSale[returnInt];

                if (strVariable.CompareTo(string.Format("Item {0} - Name:", item.ID)) == 0)
                {
                    item.Name = strValue;
                }

                else if (strVariable.CompareTo(string.Format("Item {0} - Enabled:", item.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    item.Enabled = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }

                else if (strVariable.CompareTo(string.Format("Item {0} - Cost:", item.ID)) == 0)
                {
                    item.Cost = int.Parse(strValue);
                }

                else if (strVariable.CompareTo(string.Format("Item {0} - Buy command:", item.ID)) == 0)
                {
                    item.BuyCommand = strValue;
                }

                else if (strVariable.CompareTo(string.Format("Item {0} - Description:", item.ID)) == 0)
                {
                    item.Description = strValue;
                }

                else if (strVariable.CompareTo(string.Format("Item {0} - Add a console command to run when bought:", item.ID)) == 0)
                {
                    if (strValue.Equals("Say"))
                    {
                        item.Payload.Add("Say, %buyer just bought an item!");
                    }

                    if (strValue.Equals("Whisper"))
                    {
                        item.Payload.Add("Whisper, Buyer, You just bought an item!");
                    }

                    if (strValue.Equals("Yell"))
                    {
                        item.Payload.Add("Yell, 5, %buyer just bought an item!");
                    }

                    if (strValue.Equals("Growl"))
                    {
                        item.Payload.Add("Growl, 5, Buyer, You just bought an item!");
                    }

                    if (strValue.Equals("Move"))
                    {
                        item.Payload.Add("Move, Player, US");
                    }

                    if (strValue.Equals("Mute"))
                    {
                        item.Payload.Add("Mute, Player, 5, Minutes");
                    }

                    if (strValue.Equals("Voice"))
                    {
                        item.Payload.Add("Voice");
                    }

                    if (strValue.Equals("Kill"))
                    {
                        item.Payload.Add("Kill, Player");
                    }

                    if (strValue.Equals("Nuke"))
                    {
                        item.Payload.Add("Nuke, Squad, %buyer just nuked your squad!");
                    }

                    if (strValue.Equals("Kick"))
                    {
                        item.Payload.Add("Kick, Player, Replace this text with the kick reason.");
                    }

                    if (strValue.Equals("TempBan"))
                    {
                        item.Payload.Add("TempBan, Player, 5, Hours, Replace this text with the ban reason.");
                    }

                    if (strValue.Equals("PermBan"))
                    {
                        item.Payload.Add("PermBan, Player, Replace this text with the ban reason.");
                    }

                    if (strValue.Equals("UnBan"))
                    {
                        item.Payload.Add("UnBan");
                    }

                    if (strValue.Equals("RestartRound"))
                    {
                        item.Payload.Add("RestartRound");
                    }

                    if (strValue.Equals("NextLevel"))
                    {
                        item.Payload.Add("NextLevel");
                    }

                    if (strValue.Equals("ReserveAdd"))
                    {
                        item.Payload.Add("ReserveAdd, Player, 60, Minutes");
                    }

                    if (strValue.Equals("ReserveRemove"))
                    {
                        item.Payload.Add("ReserveRemove, Player");
                    }
                    item.Payload.RemoveAll(string.IsNullOrEmpty);
                }

                if (strVariable.CompareTo(string.Format("Item {0} -    List of console commands for Item {0}:", item.ID)) == 0)
                {
                    item.Payload = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                }
            }
        }

        #endregion

        #region Currency methods

        public string BestMatchPlayerName(string strTargetGiven) // Returns bmpn_strMatchResult based on the playernames in m_dicPlayersAndBalances
        {
            int bmpn_iNumberOfMatches = 0;
            string bmpn_strMatchResult = "";

            foreach (string key in m_dicPlayersAndBalances.Keys)
            {
                if (key.ToLower().StartsWith(strTargetGiven.ToLower()) == true)
                {
                    bmpn_iNumberOfMatches++;
                    bmpn_strMatchResult = key;
                }
            }

            if (bmpn_iNumberOfMatches > 1)
            {
                bmpn_strMatchResult = "many";
            }

            else if (bmpn_iNumberOfMatches == 0)
            {
                bmpn_strMatchResult = "none";
            }
            return bmpn_strMatchResult;
        }

        public string GetRandom(string task, int team) // Returns a random player or populated squad's ID, defined by task.
        {
            Random random = new Random();

            if (task == "player")
            {
                string[] gr_straPlayers = new string[m_dicPlayersAndBalances.Count];
                m_dicPlayersAndBalances.Keys.CopyTo(gr_straPlayers, 0);
                string gr_strRandomPlayer = gr_straPlayers[random.Next(0, gr_straPlayers.Length)];
                return gr_strRandomPlayer;
            }

            else if (task == "squad")
            {
                if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                {
                    int gr_iRandomSquadID = random.Next(1, 8);

                    while (SquadCensus("read", team, gr_iRandomSquadID) == 0)
                    {
                        gr_iRandomSquadID = random.Next(1, 8);
                    }
                    return gr_iRandomSquadID.ToString();
                }

                else
                {
                    int gr_iRandomTeamID = random.Next(1, 4);

                    while (SquadCensus("read", gr_iRandomTeamID, 0) == 0)
                    {
                        gr_iRandomTeamID = random.Next(1, 4);
                    }
                    return gr_iRandomTeamID.ToString();
                }
            }
            return "error";
        }

        public int SquadCensus(string task, int teamID, int squadID) // Returns sc_iPopulation: the number of players in the squad passed to SquadCensus
        {
            int sc_iPopulation = 0;

            if (task == "update")
            {
                if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                {
                    this.m_dicSquadsAndPopulations = new Dictionary<string, int>();

                    this.m_dicSquadsAndPopulations.Add("1.1", 0);
                    this.m_dicSquadsAndPopulations.Add("1.2", 0);
                    this.m_dicSquadsAndPopulations.Add("1.3", 0);
                    this.m_dicSquadsAndPopulations.Add("1.4", 0);
                    this.m_dicSquadsAndPopulations.Add("1.5", 0);
                    this.m_dicSquadsAndPopulations.Add("1.6", 0);
                    this.m_dicSquadsAndPopulations.Add("1.7", 0);
                    this.m_dicSquadsAndPopulations.Add("1.8", 0);

                    this.m_dicSquadsAndPopulations.Add("2.1", 0);
                    this.m_dicSquadsAndPopulations.Add("2.2", 0);
                    this.m_dicSquadsAndPopulations.Add("2.3", 0);
                    this.m_dicSquadsAndPopulations.Add("2.4", 0);
                    this.m_dicSquadsAndPopulations.Add("2.5", 0);
                    this.m_dicSquadsAndPopulations.Add("2.6", 0);
                    this.m_dicSquadsAndPopulations.Add("2.7", 0);
                    this.m_dicSquadsAndPopulations.Add("2.8", 0);

                    foreach (CPlayerInfo cpiPlayer in m_dicPlayersAndInfo.Values)
                    {
                        if (cpiPlayer.TeamID == 1)
                        {
                            switch (cpiPlayer.SquadID)
                            {
                                case 1:
                                    m_dicSquadsAndPopulations["1.1"]++; break;
                                case 2:
                                    m_dicSquadsAndPopulations["1.2"]++; break;
                                case 3:
                                    m_dicSquadsAndPopulations["1.3"]++; break;
                                case 4:
                                    m_dicSquadsAndPopulations["1.4"]++; break;
                                case 5:
                                    m_dicSquadsAndPopulations["1.5"]++; break;
                                case 6:
                                    m_dicSquadsAndPopulations["1.6"]++; break;
                                case 7:
                                    m_dicSquadsAndPopulations["1.7"]++; break;
                                case 8:
                                    m_dicSquadsAndPopulations["1.8"]++; break;
                                default: break;
                            }
                        }

                        else if (cpiPlayer.TeamID == 2)
                        {
                            switch (cpiPlayer.SquadID)
                            {
                                case 1:
                                    m_dicSquadsAndPopulations["2.1"]++; break;
                                case 2:
                                    m_dicSquadsAndPopulations["2.2"]++; break;
                                case 3:
                                    m_dicSquadsAndPopulations["2.3"]++; break;
                                case 4:
                                    m_dicSquadsAndPopulations["2.4"]++; break;
                                case 5:
                                    m_dicSquadsAndPopulations["2.5"]++; break;
                                case 6:
                                    m_dicSquadsAndPopulations["2.6"]++; break;
                                case 7:
                                    m_dicSquadsAndPopulations["2.7"]++; break;
                                case 8:
                                    m_dicSquadsAndPopulations["2.8"]++; break;
                                default: break;
                            }
                        }
                    }
                }

                else
                {
                    this.m_dicSquadsAndPopulations = new Dictionary<string, int>();

                    this.m_dicSquadsAndPopulations.Add("1", 0);
                    this.m_dicSquadsAndPopulations.Add("2", 0);
                    this.m_dicSquadsAndPopulations.Add("3", 0);
                    this.m_dicSquadsAndPopulations.Add("4", 0);

                    foreach (CPlayerInfo cpiPlayer in m_dicPlayersAndInfo.Values)
                    {
                        switch (cpiPlayer.TeamID)
                        {
                            case 1:
                                m_dicSquadsAndPopulations["1"]++; break;
                            case 2:
                                m_dicSquadsAndPopulations["2"]++; break;
                            case 3:
                                m_dicSquadsAndPopulations["3"]++; break;
                            case 4:
                                m_dicSquadsAndPopulations["4"]++; break;
                            default: break;
                        }
                    }
                }
            }

            if (task == "read")
            {
                if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                {
                    if (teamID == 1)
                    {
                        switch (squadID)
                        {
                            case 1:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.1"]; break;
                            case 2:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.2"]; break;
                            case 3:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.3"]; break;
                            case 4:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.4"]; break;
                            case 5:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.5"]; break;
                            case 6:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.6"]; break;
                            case 7:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.7"]; break;
                            case 8:
                                sc_iPopulation = m_dicSquadsAndPopulations["1.8"]; break;
                            default: break;
                        }
                    }

                    else if (teamID == 2)
                    {
                        switch (squadID)
                        {
                            case 1:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.1"]; break;
                            case 2:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.2"]; break;
                            case 3:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.3"]; break;
                            case 4:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.4"]; break;
                            case 5:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.5"]; break;
                            case 6:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.6"]; break;
                            case 7:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.7"]; break;
                            case 8:
                                sc_iPopulation = m_dicSquadsAndPopulations["2.8"]; break;
                            default: break;
                        }
                    }
                }

                else
                {
                    switch (teamID)
                    {
                        case 1:
                            sc_iPopulation = m_dicSquadsAndPopulations["1"]; break;
                        case 2:
                            sc_iPopulation = m_dicSquadsAndPopulations["2"]; break;
                        case 3:
                            sc_iPopulation = m_dicSquadsAndPopulations["3"]; break;
                        case 4:
                            sc_iPopulation = m_dicSquadsAndPopulations["4"]; break;
                        default: break;
                    }
                }
            }
            return sc_iPopulation;
        }

        public int MuggingHandler(string strKiller, string strVictim) // returns mh_iMuggingPayoff for use in OnPlayerKilled() payments below
        {
            int mh_iMuggingPayoff = 0;

            if (m_ebynEnableMugging == enumBoolYesNo.Yes)
            {
                if (m_dicPlayersAndBalances.ContainsKey(strVictim) && m_dicPlayersAndBalances[strVictim] > 0)
                {
                    mh_iMuggingPayoff = (int)Math.Floor((Decimal)m_dicPlayersAndBalances[strVictim] * ((Decimal)m_iMuggingPercentStolen / 100M));
                }

                if (m_ebynEnableMuggingLimit == enumBoolYesNo.Yes && mh_iMuggingPayoff > m_iMuggingLimit)
                {
                    mh_iMuggingPayoff = m_iMuggingLimit;
                }

                if (mh_iMuggingPayoff > 0)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "" + strKiller + " just mugged " + strVictim + " for " + mh_iMuggingPayoff + " " + m_strCurrencySymbol + "!");
                    }

                    if (m_lstNoSpam.Contains(strVictim) == false)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + strKiller + " just mugged you for " + mh_iMuggingPayoff + " " + m_strCurrencySymbol + "!", "player", strVictim);
                    }

                    if (m_lstNoSpam.Contains(strKiller) == false)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": You just mugged " + strVictim + " for " + mh_iMuggingPayoff + " " + m_strCurrencySymbol + "!", "player", strKiller);
                    }

                    if (m_strMugged != String.Empty)
                    {
                        if (m_ebynYellStrMugged == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.yell", m_strMugged.Replace("%killer", strKiller).Replace("%victim", strVictim).Replace("%amount", mh_iMuggingPayoff.ToString()), (m_iStrMuggedYellLength * 1000).ToString(), "all");
                        }

                        if (m_ebynSayStrMugged == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strMugged.Replace("%killer", strKiller).Replace("%victim", strVictim).Replace("%amount", mh_iMuggingPayoff.ToString()), "all");
                        }
                    }
                }
            }
            return mh_iMuggingPayoff;
        }

        public void AutoBountyHandler(string strKiller, string strVictim) // Handles automatic bounties
        {
            if (m_dicPlayerStreaks.ContainsKey(strKiller))
            {
                m_dicPlayerStreaks[strKiller]++;

                if (this.m_dicPlayerStreaks[strKiller] == this.m_iAutoBountyStreak)
                {
                    if (m_dicPlayersAndBounties.ContainsKey(strKiller))
                    {
                        m_dicPlayersAndBounties[strKiller] += m_iAutoBountyStartingBounty;
                    }

                    else
                    {
                        m_dicPlayersAndBounties.Add(strKiller, m_iAutoBountyStartingBounty);
                    }

                    if (m_ebynPublicAutoBountyNotifications == enumBoolYesNo.Yes)
                    {
                        if (m_ebynSayAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyPosted != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strAutoBountyPosted.Replace("%killer", strKiller).Replace("%amount", m_iAutoBountyStartingBounty.ToString()).Replace("%streak", m_dicPlayerStreaks[strKiller].ToString()), "all");
                        }

                        if (m_ebynYellAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyPosted != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strAutoBountyPosted.Replace("%killer", strKiller).Replace("%amount", m_iAutoBountyStartingBounty.ToString()).Replace("%streak", m_dicPlayerStreaks[strKiller].ToString()), (m_iAutoBountyNotificationsYellLength * 1000).ToString(), "all");
                        }
                    }

                    else
                    {
                        if (m_lstNoSpam.Contains(strKiller) == false)
                        {
                            if (m_ebynSayAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyPosted != String.Empty)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strAutoBountyPosted.Replace("%killer", "your").Replace("%amount", m_iAutoBountyStartingBounty.ToString()).Replace("%streak", m_dicPlayerStreaks[strKiller].ToString()), "player", strKiller);
                            }

                            if (m_ebynYellAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyPosted != String.Empty)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strAutoBountyPosted.Replace("%killer", "your").Replace("%amount", m_iAutoBountyStartingBounty.ToString()).Replace("%streak", m_dicPlayerStreaks[strKiller].ToString()), (m_iAutoBountyNotificationsYellLength * 1000).ToString(), "player", strKiller);
                            }
                        }
                    }
                }

                else if (this.m_dicPlayerStreaks[strKiller] > this.m_iAutoBountyStreak && this.m_dicPlayersAndBounties.ContainsKey(strKiller))
                {
                    m_dicPlayersAndBounties[strKiller] += m_iAutoBountyIncreasePerKill;
                }
            }

            else
            {
                m_dicPlayerStreaks.Add(strKiller, 1);
            }

            if (m_dicPlayerStreaks.ContainsKey(strVictim) && m_dicPlayerStreaks[strVictim] >= m_iAutoBountyStreak)
            {
                if (m_ebynPublicAutoBountyNotifications == enumBoolYesNo.Yes)
                {
                    if (m_ebynSayAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyCollected != String.Empty)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strAutoBountyCollected.Replace("%killer", strKiller).Replace("%victim", strVictim).Replace("%amount", m_dicPlayersAndBounties[strVictim].ToString()).Replace("%streak", m_dicPlayerStreaks[strVictim].ToString()), "all");
                    }

                    if (m_ebynYellAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyCollected != String.Empty)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strAutoBountyCollected.Replace("%killer", strKiller).Replace("%victim", strVictim).Replace("%amount", m_dicPlayersAndBounties[strVictim].ToString()).Replace("%streak", m_dicPlayerStreaks[strVictim].ToString()), (m_iAutoBountyNotificationsYellLength * 1000).ToString(), "all");
                    }
                }

                else
                {
                    if (m_lstNoSpam.Contains(strVictim) == false)
                    {
                        if (m_ebynSayAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyCollected != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strAutoBountyCollected.Replace("%killer", strKiller).Replace("%victim", "your").Replace("%amount", m_dicPlayersAndBounties[strVictim].ToString()).Replace("%streak", m_dicPlayerStreaks[strVictim].ToString()), "player", strVictim);
                        }

                        if (m_ebynYellAutoBountyNotifications == enumBoolYesNo.Yes && m_strAutoBountyCollected != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strAutoBountyCollected.Replace("%killer", strKiller).Replace("%victim", "your").Replace("%amount", m_dicPlayersAndBounties[strVictim].ToString()).Replace("%streak", m_dicPlayerStreaks[strVictim].ToString()), (m_iAutoBountyNotificationsYellLength * 1000).ToString(), "player", strVictim);
                        }
                    }
                }

            }
        }

        public void PlayerBountyHandler(string task, string killer, string victim, int amount) // Handles player-created bounties
        {
            if (task == "add")
            {
                if (amount <= 0)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Mmhmm, now why would you want to add a " + amount + " " + m_strCurrencySymbol + " bounty to " + victim + "? d:", "player", killer);
                }

                else if (m_dicPlayersAndBalances[killer] < amount)
                {
                    int difference = amount - m_dicPlayersAndBalances[killer];
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Insufficient funds. You need " + difference + " more " + m_strCurrencySymbol + " to set a bounty that high!", "player", killer);
                }

                else
                {
                    if (m_dicPlayersAndBounties.ContainsKey(victim))
                    {
                        m_dicPlayersAndBalances[killer] -= amount;
                        m_dicPlayersAndBounties[victim] += amount;
                    }

                    else
                    {
                        m_dicPlayersAndBalances[killer] -= amount;
                        m_dicPlayersAndBounties.Add(victim, amount);
                    }

                    if (m_ebynPublicPlayerBountyNotifications == enumBoolYesNo.Yes)
                    {
                        if (m_ebynSayPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyPosted != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPlayerBountyPosted.Replace("%poster", killer).Replace("%target", victim).Replace("%amount", amount.ToString()), "all");
                        }

                        if (m_ebynYellPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyPosted != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strPlayerBountyPosted.Replace("%poster", killer).Replace("%target", victim).Replace("%amount", amount.ToString()), (m_iPlayerBountyNotificationsYellLength * 1000).ToString(), "all");
                        }
                    }

                    else
                    {
                        if (m_ebynSayPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyPosted != String.Empty)
                        {
                            if (m_lstNoSpam.Contains(killer) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPlayerBountyPosted.Replace("%poster", "you").Replace("%target", victim).Replace("%amount", amount.ToString()), "player", killer);
                            }

                            if (m_lstNoSpam.Contains(victim) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPlayerBountyPosted.Replace("%poster", killer).Replace("%target", "your").Replace("%amount", amount.ToString()), "player", victim);
                            }
                        }

                        if (m_ebynYellPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyPosted != String.Empty)
                        {
                            if (m_lstNoSpam.Contains(killer) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strPlayerBountyPosted.Replace("%poster", "you").Replace("%target", victim).Replace("%amount", amount.ToString()), (m_iPlayerBountyNotificationsYellLength * 1000).ToString(), "player", killer);
                            }

                            if (m_lstNoSpam.Contains(victim) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strPlayerBountyPosted.Replace("%poster", killer).Replace("%target", "your").Replace("%amount", amount.ToString()), (m_iPlayerBountyNotificationsYellLength * 1000).ToString(), "player", victim);
                            }
                        }
                    }
                }
            }

            else if (task == "kill")
            {
                if (m_dicPlayersAndBounties.ContainsKey(victim) && m_dicPlayersAndBounties[victim] > 0)
                {
                    if (m_ebynPublicPlayerBountyNotifications == enumBoolYesNo.Yes)
                    {
                        if (m_ebynSayPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyCollected != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPlayerBountyCollected.Replace("%killer", killer).Replace("%victim", victim).Replace("%amount", m_dicPlayersAndBounties[victim].ToString()), "all");
                        }

                        if (m_ebynYellPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyCollected != String.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strPlayerBountyCollected.Replace("%killer", killer).Replace("%victim", victim).Replace("%amount", m_dicPlayersAndBounties[victim].ToString()), (m_iPlayerBountyNotificationsYellLength * 1000).ToString(), "all");
                        }
                    }

                    else
                    {
                        if (m_ebynSayPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyCollected != String.Empty)
                        {
                            if (m_lstNoSpam.Contains(killer) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPlayerBountyCollected.Replace("%killer", "you").Replace("%victim", victim).Replace("%amount", m_dicPlayersAndBounties[victim].ToString()), "player", killer);
                            }

                            if (m_lstNoSpam.Contains(victim) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPlayerBountyCollected.Replace("%killer", killer).Replace("%victim", "your").Replace("%amount", m_dicPlayersAndBounties[victim].ToString()), "player", victim);
                            }
                        }

                        if (m_ebynYellPlayerBountyNotifications == enumBoolYesNo.Yes && m_strPlayerBountyCollected != String.Empty)
                        {
                            if (m_lstNoSpam.Contains(killer) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strPlayerBountyCollected.Replace("%killer", "you").Replace("%victim", victim).Replace("%amount", m_dicPlayersAndBounties[victim].ToString()), (m_iPlayerBountyNotificationsYellLength * 1000).ToString(), "player", killer);
                            }

                            if (m_lstNoSpam.Contains(victim) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", m_strCurrencyInstanceName + ": " + m_strPlayerBountyCollected.Replace("%killer", killer).Replace("%victim", "your").Replace("%amount", m_dicPlayersAndBounties[victim].ToString()), (m_iPlayerBountyNotificationsYellLength * 1000).ToString(), "player", victim);
                            }
                        }
                    }
                }
            }
        }

        public void PayrollManager(string strSoldierName) // Handles payroll actions and adds Currency to m_dicPlayersAndBalances
        {
            if (m_ebynEnablePayroll == enumBoolYesNo.Yes)
            {
                int iNewStampIndexOffset = (int)(DateTime.Now - m_dtPreviousEntryStamp).TotalSeconds;

                if (this.m_dicPayrollSchedule.ContainsKey(strSoldierName))
                {
                    if (m_dicPayrollSchedule[strSoldierName] < iNewStampIndexOffset)
                    {
                        m_dicPayrollSchedule[strSoldierName] = iNewStampIndexOffset + (m_iPayrollFrequency * 60);
                        m_dicPlayersAndBalances[strSoldierName] += m_iPayrollAmount;

                        if (m_lstNoSpam.Contains(strSoldierName) == false)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + m_strPayrollMessage.Replace("%amount", m_iPayrollAmount.ToString()), "player", strSoldierName);
                        }

                        if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", m_strCurrencyInstanceName + ": Payroll - Added " + m_iPayrollAmount + " " + m_strCurrencySymbol + " to " + strSoldierName + "'s account.");
                        }
                    }
                }

                else
                {
                    m_dicPayrollSchedule.Add(strSoldierName, iNewStampIndexOffset + (m_iPayrollFrequency * 60));
                }
            }
        }

        public void CreateSQLAccount(string strGuid, string strSoldierName) // Creates a MySQL row for the specified player
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();

                    string insertSQL = "INSERT INTO tbl_currency (guid, CC, playername) VALUE ('" + strGuid + "', '0', '" + strSoldierName + "')";

                    OdbcCommand OdbcCom = new OdbcCommand(insertSQL, OdbcCon);
                    OdbcCom.ExecuteNonQuery();
                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                    }
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
            }

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }
        }

        public bool CheckAccountByName(string strSoldierName) // Checks the database for strSoldierName. Returns `true` if the ID key is > 0.
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();

                    string uploadSQL = "SELECT count(id) FROM tbl_currency WHERE `playername` = '" + strSoldierName + "'";
                    OdbcCommand OdbcCom = new OdbcCommand(uploadSQL, OdbcCon);
                    OdbcDataReader odbcDR = OdbcCom.ExecuteReader();

                    while (odbcDR.Read())
                    {
                        return (Convert.ToInt32(odbcDR[0]) > 0);
                    }

                    if (OdbcCon.State == ConnectionState.Open)
                    {
                        OdbcCon.Close();
                    }
                    return false;
                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                    }
                    return false;
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
                return false;
            }
        }

        public void AddCurrency(int strCC, string strSoldierName) // Adds strCC currency to strSoldierName
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();
                    string updateSQL = "UPDATE tbl_currency SET `CC` = `CC`+'" + strCC + "' WHERE `playername` = '" + strSoldierName + "'";

                    OdbcCommand OdbcCom = new OdbcCommand(updateSQL, OdbcCon);
                    OdbcCom.ExecuteNonQuery();
                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                    }
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
            }

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }

        }

        public void SetCurrency(int strCC, string strSoldierName) // Sets strSoldierName's currency to strCC
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();

                    string updateSQL = "UPDATE tbl_currency SET `CC` = '" + strCC + "' WHERE `playername` = '" + strSoldierName + "'";

                    OdbcCommand OdbcCom = new OdbcCommand(updateSQL, OdbcCon);
                    OdbcCom.ExecuteNonQuery();
                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                    }
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
            }

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }
        }

        public void RemoveCurrency(int strCC, string strSoldierName) // Removes strCC currency from strSoldierName
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();

                    string updateSQL = "UPDATE tbl_currency SET `CC` = `CC`-'" + strCC + "' WHERE `playername` = '" + strSoldierName + "'";

                    OdbcCommand OdbcCom = new OdbcCommand(updateSQL, OdbcCon);
                    OdbcCom.ExecuteNonQuery();

                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                    }
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
            }

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }
        }

        public string GetBalance(string strSoldierName) // Checks the database for strSoldierName. Returns the `CC` key.
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();

                    string uploadSQL = "SELECT * FROM tbl_currency WHERE `playername` = '" + strSoldierName + "'";
                    OdbcCommand OdbcCom = new OdbcCommand(uploadSQL, OdbcCon);
                    OdbcDataReader odbcDR = OdbcCom.ExecuteReader();

                    while (odbcDR.Read())
                    {
                        return (Convert.ToString(odbcDR[2]));
                    }

                    if (OdbcCon.State == ConnectionState.Open)
                    {
                        OdbcCon.Close();
                    }
                    return "0";
                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                    }
                    return "0";
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
                return "0";
            }

        }

        #endregion

        #region PRoCon events

        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if (strSpeaker.Equals("Server") == false)
            {
                string[] messageWords = strMessage.Split(new Char[] { ' ' });

                if (messageWords[0].ToLower() == this.m_strMainMenuTrigger.ToLower())
                {
                    foreach (string line in m_straMainMenu)
                    {
                        if (line != string.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", line, "player", strSpeaker);
                        }
                    }

                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Your balance is " + this.m_dicPlayersAndBalances[strSpeaker] + " " + m_strCurrencySymbol + " on " + strSpeaker + ".", "player", strSpeaker);

                    if (m_ebynEnablePayroll == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": You automatically receive " + m_iPayrollAmount + " " + m_strCurrencySymbol + " for every " + m_iPayrollFrequency + " minutes you're here.", "player", strSpeaker);
                    }

                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Type " + m_strShopTrigger + " to browse the shop or " + m_strPlayerPersonalSpamTrigger + " to toggle " + m_strCurrencyInstanceName + " notifications.", "player", strSpeaker);
                }

                if (messageWords[0].ToLower() == m_strShopTrigger.ToLower())
                {
                    foreach (string line in m_straShop)
                    {
                        if (line != string.Empty)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", line, "player", strSpeaker);
                        }
                    }

                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + " shop: Below is a list of our current inventory. You have " + m_dicPlayersAndBalances[strSpeaker] + " " + m_strCurrencySymbol + ".", "player", strSpeaker);

                    foreach (Item item in m_dicItemsForSale.Values)
                    {
                        if (item.Enabled == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", item.Name + " - " + item.Cost + " " + m_strCurrencySymbol + ". " + item.BuyCommand + ": " + item.Description, "player", strSpeaker);
                        }
                    }

                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + " shop: Type a @command to buy and use that item. Tap ~ for better viewing.", "player", strSpeaker);
                }

                if (messageWords[0].ToLower() == m_strCheckBalanceTrigger.ToLower())
                {
                    if (CheckAccountByName(strSpeaker) == true)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Your balance is currently " + m_dicPlayersAndBalances[strSpeaker] + " " + m_strCurrencySymbol + ".", "player", strSpeaker);
                    }
                }

                if (messageWords[0].ToLower() == m_strPlayerPersonalSpamTrigger.ToLower())
                {
                    if (this.m_lstNoSpam.Contains(strSpeaker))
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Notifications ON. Type " + m_strPlayerPersonalSpamTrigger + " again to turn them back off.", "player", strSpeaker);
                        this.m_lstNoSpam.Remove(strSpeaker);
                    }

                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Notifications OFF. Type " + m_strPlayerPersonalSpamTrigger + " to turn them back on.", "player", strSpeaker);
                        this.m_lstNoSpam.Add(strSpeaker);
                    }
                }

                if (m_ebynAllowAdminPrivileges == enumBoolYesNo.Yes)
                {
                    if (messageWords[0].ToLower() == m_strAdminCheckBalanceCommand.ToLower())
                    {
                        CPrivileges cpSpeakerPrivs = GetAccountPrivileges(strSpeaker);

                        if (cpSpeakerPrivs != null && cpSpeakerPrivs.PrivilegesFlags > 8328)
                        {
                            if (messageWords.Length < 2)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Syntax error. Whose balance do you want to check?", "player", strSpeaker);
                            }

                            else if (BestMatchPlayerName(messageWords[1]) == "many")
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                            }

                            else if (BestMatchPlayerName(messageWords[1]) == "none")
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                            }

                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": " + BestMatchPlayerName(messageWords[1]) + "'s balance is " + m_dicPlayersAndBalances[BestMatchPlayerName(messageWords[1])] + " " + m_strCurrencySymbol + ".", "player", strSpeaker);
                            }
                        }
                    }

                    if (messageWords[0].ToLower() == m_strAdminAddCurrencyCommand.ToLower())
                    {
                        CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSpeaker);

                        if (cpSpeakerPrivs != null && cpSpeakerPrivs.PrivilegesFlags > 8328)
                        {
                            if (messageWords.Length < 2)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Syntax error. Whose account do you want to add to?", "player", strSpeaker);
                            }

                            else if (BestMatchPlayerName(messageWords[1]) == "many")
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                            }

                            else if (BestMatchPlayerName(messageWords[1]) == "none")
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                            }

                            else
                            {
                                m_dicPlayersAndBalances[BestMatchPlayerName(messageWords[1])] += int.Parse(messageWords[2]);
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Added " + messageWords[2] + " " + m_strCurrencySymbol + " to " + BestMatchPlayerName(messageWords[1]) + "'s account. New balance: " + m_dicPlayersAndBalances[BestMatchPlayerName(messageWords[1])].ToString() + " " + m_strCurrencySymbol, "player", strSpeaker);
                            }
                        }
                    }

                    if (messageWords[0].ToLower() == m_strAdminRemoveCurrencyCommand.ToLower())
                    {
                        CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSpeaker);

                        if (cpSpeakerPrivs != null && cpSpeakerPrivs.PrivilegesFlags > 8328)
                        {
                            if (messageWords.Length < 2)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Syntax error. Whose account do you want to remove from?", "player", strSpeaker);
                            }

                            else if (BestMatchPlayerName(messageWords[1]) == "many")
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                            }

                            else if (BestMatchPlayerName(messageWords[1]) == "none")
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                            }

                            else
                            {
                                m_dicPlayersAndBalances[BestMatchPlayerName(messageWords[1])] -= int.Parse(messageWords[2]);
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Removed " + messageWords[2] + " " + m_strCurrencySymbol + " from " + BestMatchPlayerName(messageWords[1]) + "'s account. New balance: " + m_dicPlayersAndBalances[BestMatchPlayerName(messageWords[1])].ToString() + " " + m_strCurrencySymbol, "player", strSpeaker);
                            }
                        }
                    }
                }

                if (m_ebynEnablePlayerBounties == enumBoolYesNo.Yes)
                {
                    if (messageWords[0].ToLower() == m_strPlayerBountyPostCommand.ToLower())
                    {
                        if (messageWords.Length < 2)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Syntax error. Who do you want to add a bounty to?", "player", strSpeaker);
                        }

                        else if (messageWords.Length < 3)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Syntax error. How much do you want to put on " + BestMatchPlayerName(messageWords[1]) + "'s head?", "player", strSpeaker);
                        }

                        else if (BestMatchPlayerName(messageWords[1]) == "many")
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                        }

                        else if (BestMatchPlayerName(messageWords[1]) == "none")
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                        }

                        else
                        {
                            PlayerBountyHandler("add", strSpeaker, BestMatchPlayerName(messageWords[1]), int.Parse(messageWords[2]));
                        }
                    }
                }

                if (m_ebynEnableAutoBounties == enumBoolYesNo.Yes || m_ebynEnablePlayerBounties == enumBoolYesNo.Yes)
                {
                    if (messageWords[0].ToLower() == m_strBountyBoardCommand.ToLower())
                    {
                        if (m_dicPlayersAndBounties.Count == 0)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": No bounties posted! Type @addbounty [playername] [amount] to add one!", "player", strSpeaker);
                        }

                        else
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Bounty Board - First to kill 'em gets the bounty!", "player", strSpeaker);

                            foreach (KeyValuePair<string, int> playerAndBounty in m_dicPlayersAndBounties)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", playerAndBounty.Key + " - " + playerAndBounty.Value + " " + m_strCurrencySymbol, "player", strSpeaker);
                            }
                        }
                    }
                }

                foreach (Item item in m_dicItemsForSale.Values)
                {
                    if (messageWords[0].ToLower() == item.BuyCommand.ToLower())
                    {
                        if (item.Enabled == enumBoolYesNo.Yes)
                        {
                            if (m_dicPlayersAndBalances.ContainsKey(strSpeaker) && m_dicPlayersAndBalances[strSpeaker] < item.Cost)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + " shop: Insufficient funds. You need " + (item.Cost - m_dicPlayersAndBalances[strSpeaker]).ToString() + " more " + m_strCurrencySymbol + " to purchase that item.", "player", strSpeaker);
                            }

                            else
                            {
                                int commandsSuccessful = 0;

                                foreach (string payload in item.Payload)
                                {
                                    string[] payloadFields = payload.Split(',');

                                    if (payloadFields[0].CompareTo("Say") == 0)
                                    {
                                        this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[1].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1])).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), "all");
                                        commandsSuccessful++;
                                    }

                                    if (payloadFields[0].CompareTo("Whisper") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), "player", strSpeaker);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Target") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), "player", strSpeaker);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [playername] [message]", "player", strSpeaker);
                                            }

                                            else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                            }

                                            else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1].Replace("&", " ") + "' not found. Check your spelling.", "player", strSpeaker);
                                            }

                                            else if (payloadFields.Length < 3)
                                            {
                                                if (messageWords.Length < 3)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [playername] [message]", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    int i = 2;

                                                    string whisperPayload = "";

                                                    do
                                                    {
                                                        whisperPayload = whisperPayload + " " + messageWords[i];
                                                        i++;
                                                    }
                                                    while (i < messageWords.Length);

                                                    whisperPayload = "From " + strSpeaker + ": " + whisperPayload;

                                                    if (whisperPayload.Length > 99)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Your message must be under 100 characters long.", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", whisperPayload.Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), "player", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                        commandsSuccessful++;
                                                    }
                                                }
                                            }

                                            else
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), "player", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                commandsSuccessful++;
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("Yell") == 0)
                                    {
                                        this.ExecuteCommand("procon.protected.send", "admin.yell", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), (int.Parse(payloadFields[1]) * 1000).ToString(), "all");
                                        commandsSuccessful++;
                                    }

                                    if (payloadFields[0].CompareTo("Growl") == 0)
                                    {
                                        if (payloadFields[2].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.yell", payloadFields[3].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), (int.Parse(payloadFields[1]) * 1000).ToString(), "player", strSpeaker);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Target") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.yell", payloadFields[3].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), (int.Parse(payloadFields[1]) * 1000).ToString(), "player", strSpeaker);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[2].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                            }

                                            else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                            }

                                            else if (payloadFields.Length < 4)
                                            {
                                                if (messageWords.Length < 3)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [message]", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    int i = 2;
                                                    string growlPayload = "";

                                                    do
                                                    {
                                                        growlPayload = growlPayload + " " + messageWords[i];
                                                        i++;
                                                    }
                                                    while (i < messageWords.Length);

                                                    growlPayload = "From " + strSpeaker + ": " + growlPayload;

                                                    if (growlPayload.Length > 99)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Your message must be under 100 characters long.", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.yell", growlPayload.Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), (int.Parse(payloadFields[1]) * 1000).ToString(), "player", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                        commandsSuccessful++;
                                                    }
                                                }
                                            }

                                            else
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.yell", payloadFields[3].Replace("%buyer", strSpeaker).Replace("%target", BestMatchPlayerName(messageWords[1].Replace("%", " "))).Replace("%amount", item.Cost.ToString()).Replace("%balance", m_dicPlayersAndBalances[strSpeaker].ToString()), (int.Parse(payloadFields[1]) * 1000).ToString(), "player", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                commandsSuccessful++;
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("Move") == 0)
                                    {
                                        if (messageWords.Length < 2)
                                        {
                                            if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [Alpha/Bravo/...]", "player", strSpeaker);
                                            }
                                        }

                                        else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1].Replace("&", " ") + "' matches too many names. Be more specific.", "player", strSpeaker);
                                        }

                                        else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1].Replace("&", " ") + "' not found. Check your spelling.", "player", strSpeaker);
                                        }

                                        else
                                        {
                                            if (messageWords.Length < 3)
                                            {
                                                if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                                {
                                                    int otherTeam = -1;

                                                    if (m_dicPlayersAndInfo[BestMatchPlayerName(messageWords[1].Replace("&", " "))].TeamID == 1)
                                                    {
                                                        otherTeam = 2;
                                                    }

                                                    else
                                                    {
                                                        otherTeam = 1;
                                                    }

                                                    this.ExecuteCommand("procon.protected.send", "admin.movePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")), otherTeam.ToString(), "0", "true");
                                                    commandsSuccessful++;
                                                }

                                                else
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [Alpha/Bravo/...]", "player", strSpeaker);
                                                }
                                            }

                                            else if (messageWords.Length == 3)
                                            {
                                                if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                                {
                                                    if (messageWords[2].ToLower() != "us" && messageWords[2].ToLower() != "ru" && messageWords[2].ToLower() != "1" && messageWords[2].ToLower() != "2")
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [US/RU]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")), messageWords[2].Replace("us", "1").Replace("ru", "2").Replace("US", "1").Replace("RU", "2"), "0", "true");
                                                        commandsSuccessful++;
                                                    }
                                                }

                                                else
                                                {
                                                    if (messageWords[2].ToLower() != "alpha" && messageWords[2].ToLower() != "bravo" && messageWords[2].ToLower() != "charlie" && messageWords[2].ToLower() != "delta")
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [Alpha/Bravo/...]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")), messageWords[2].ToLower().Replace("alpha", "1").Replace("bravo", "2").Replace("charlie", "3").Replace("delta", "4"), "0", "true");
                                                        commandsSuccessful++;
                                                    }
                                                }
                                            }

                                            else if (messageWords.Length == 4)
                                            {
                                                if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                                {
                                                    if (messageWords[3].ToLower() != "alpha" && messageWords[3].ToLower() != "bravo" && messageWords[3].ToLower() != "charlie" && messageWords[3].ToLower() != "delta" && messageWords[3].ToLower() != "echo" && messageWords[3].ToLower() != "foxtrot" && messageWords[3].ToLower() != "golf" && messageWords[3].ToLower() != "hotel")
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [US/RU] [Alpha/Bravo/...]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")), messageWords[2].ToLower().Replace("us", "1").Replace("ru", "2").Replace("US", "1").Replace("RU", "2"), messageWords[3].Replace("alpha", "1").Replace("bravo", "2").Replace("charlie", "3").Replace("delta", "4").Replace("echo", "5").Replace("foxtrot", "6").Replace("golf", "7").Replace("hotel", "8"), "true");
                                                        commandsSuccessful++;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("Mute") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "muted", strSpeaker);
                                            this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
                                            this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_UnMute_" + strSpeaker, ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].ToLower().Replace("seconds", "1").Replace("minutes", "60").Replace("hours", "3600").Replace("days", "86400").Replace("weeks", "604800").Replace("months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "textChatModerationList.removePlayer", strSpeaker);
                                            this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_UnMuteSave_" + strSpeaker, ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].ToLower().Replace("seconds", "1").Replace("minutes", "60").Replace("hours", "3600").Replace("days", "86400").Replace("weeks", "604800").Replace("months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "textChatModerationList.save");
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "muted", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                    this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
                                                    this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_UnMute_" + strSpeaker, ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].ToLower().Replace("seconds", "1").Replace("minutes", "60").Replace("hours", "3600").Replace("days", "86400").Replace("weeks", "604800").Replace("months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "textChatModerationList.removePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                    this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_UnMuteSave_" + strSpeaker, ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].ToLower().Replace("seconds", "1").Replace("minutes", "60").Replace("hours", "3600").Replace("days", "86400").Replace("weeks", "604800").Replace("months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "textChatModerationList.save");
                                                    commandsSuccessful++;
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("Voice") == 0)
                                    {
                                        if (messageWords.Length < 2)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                        }

                                        else
                                        {
                                            if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                            }

                                            else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                this.ExecuteCommand("procon.protected.send", "textChatModerationList.removePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
                                                commandsSuccessful++;
                                            }
                                        }

                                    }

                                    if (payloadFields[0].CompareTo("Kick") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSpeaker, payloadFields[2]);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Random") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", GetRandom("player", 0), payloadFields[2]);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    if (payloadFields.Length > 2)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")), payloadFields[2]);
                                                        commandsSuccessful++;
                                                    }

                                                    else
                                                    {
                                                        if (messageWords.Length < 3)
                                                        {
                                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [message]", "player", strSpeaker);
                                                        }

                                                        else
                                                        {
                                                            int i = 2;
                                                            string kickMessage = "";

                                                            do
                                                            {
                                                                kickMessage = kickMessage + " " + messageWords[i];
                                                                i++;
                                                            }
                                                            while (i < messageWords.Length);

                                                            kickMessage = strSpeaker + ": " + kickMessage;

                                                            if (kickMessage.Length > 50)
                                                            {
                                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Your message must be under 50 characters long.", "player", strSpeaker);
                                                            }

                                                            else
                                                            {
                                                                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")), kickMessage);
                                                                commandsSuccessful++;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("Kill") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", strSpeaker);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Random") == 0)
                                        {
                                            string randomPlayer = GetRandom("player", 0);
                                            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", randomPlayer);
                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": You were randomly killed by " + strSpeaker + "'s " + item.BuyCommand + "!", "player", randomPlayer);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.killPlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                    commandsSuccessful++;
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("Nuke") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Random") == 0)
                                        {
                                            int randomKills = 0;

                                            if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                            {
                                                Random random = new Random();
                                                int randomTeam = random.Next(1, 2);
                                                string randomSquad = GetRandom("squad", randomTeam);

                                                foreach (CPlayerInfo player in m_dicPlayersAndInfo.Values)
                                                {
                                                    if (randomKills >= 4)
                                                    {
                                                        break;
                                                    }

                                                    if (player.TeamID == randomTeam && player.SquadID == int.Parse(randomSquad))
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player.SoldierName);
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[4].Replace("%buyer", strSpeaker));
                                                        randomKills++;
                                                    }
                                                }
                                                commandsSuccessful++;
                                            }

                                            else
                                            {
                                                int randomTeam = int.Parse(GetRandom("squad", 0));

                                                foreach (CPlayerInfo player in m_dicPlayersAndInfo.Values)
                                                {
                                                    if (randomKills >= 4)
                                                    {
                                                        break;
                                                    }

                                                    if (player.TeamID == randomTeam)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player.SoldierName);
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[4].Replace("%buyer", strSpeaker));
                                                        randomKills++;
                                                    }
                                                }
                                                commandsSuccessful++;
                                            }
                                        }

                                        else if (payloadFields[1].CompareTo(" Squad") == 0)
                                        {
                                            int nukeKills = 0;
                                            string targetTeam = "";
                                            string targetSquad = "";

                                            if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                            {
                                                if (messageWords.Length < 3)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [US/RU] [Alpha/Bravo/...]", "player", strSpeaker);
                                                }

                                                else if (messageWords[1].ToLower() != "us" && messageWords[1].ToLower() != "ru")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [US/RU] [Alpha/Bravo/...]", "player", strSpeaker);
                                                }

                                                else if (messageWords[2].ToLower() != "alpha" && messageWords[2].ToLower() != "bravo" && messageWords[2].ToLower() != "charlie" && messageWords[2].ToLower() != "delta" && messageWords[2].ToLower() != "echo" && messageWords[2].ToLower() != "foxtrot" && messageWords[2].ToLower() != "golf" && messageWords[2].ToLower() != "hotel")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [US/RU] [Alpha/Bravo/...]", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    targetTeam = messageWords[1].Replace("US", "1").Replace("us", "1").Replace("RU", "2").Replace("ru", "2");
                                                    targetSquad = messageWords[2].ToLower().Replace("alpha", "1").Replace("bravo", "2").Replace("charlie", "3").Replace("delta", "4").Replace("echo", "5").Replace("foxtrot", "6").Replace("golf", "7").Replace("hotel", "8");

                                                    if (SquadCensus("read", int.Parse(targetTeam), int.Parse(targetSquad)) == 0)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error: Invalid/Empty squad. Syntax: " + item.BuyCommand + " [US/RU] [Alpha/Bravo/...]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        foreach (CPlayerInfo player in m_dicPlayersAndInfo.Values)
                                                        {
                                                            if (nukeKills >= 4)
                                                            {
                                                                break;
                                                            }

                                                            if (player.TeamID == int.Parse(targetTeam) && player.SquadID == int.Parse(targetSquad))
                                                            {
                                                                if (payloadFields[2] != string.Empty)
                                                                {
                                                                    this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%amount", item.Cost.ToString()), "player", player.SoldierName);
                                                                }

                                                                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player.SoldierName);
                                                                nukeKills++;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            else // If m_strCurrentGameMode is "sqdm" or "sqrush":
                                            {
                                                if (messageWords.Length < 2)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + "[Alpha/Bravo/...]", "player", strSpeaker);
                                                }

                                                else if (messageWords[1].ToLower() != "alpha" && messageWords[2].ToLower() != "bravo" && messageWords[2].ToLower() != "charlie" && messageWords[2].ToLower() != "delta" && messageWords[2].ToLower() != "echo" && messageWords[2].ToLower() != "foxtrot" && messageWords[2].ToLower() != "golf" && messageWords[2].ToLower() != "hotel")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [US/RU] [Alpha/Bravo/...]", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    targetTeam = messageWords[1].ToLower().Replace("alpha", "1").Replace("bravo", "2").Replace("charlie", "3").Replace("delta", "4").Replace("echo", "5").Replace("foxtrot", "6").Replace("golf", "7").Replace("hotel", "8");
                                                    targetSquad = "0";

                                                    if (SquadCensus("read", int.Parse(targetTeam), int.Parse(targetSquad)) == 0)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error: Invalid/Empty squad. Syntax: " + item.BuyCommand + " [Alpha/Bravo/...]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        foreach (CPlayerInfo player in m_dicPlayersAndInfo.Values)
                                                        {
                                                            if (nukeKills >= 4)
                                                            {
                                                                break;
                                                            }

                                                            if (player.TeamID == int.Parse(targetTeam))
                                                            {
                                                                if (payloadFields[2] != string.Empty)
                                                                {
                                                                    this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%amount", item.Cost.ToString()), "player", player.SoldierName);
                                                                }

                                                                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player.SoldierName);
                                                                nukeKills++;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Team") == 0)
                                        {
                                            if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
                                            {
                                                if (messageWords.Length < 2)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [US/RU]", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    if (messageWords[1].ToLower() != "us" && messageWords[1].ToLower() != "ru" && messageWords[1] != "1" && messageWords[1] != "2")
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [US/RU]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        string targetTeam = messageWords[1].ToLower().Replace("us", "1").Replace("ru", "2");

                                                        foreach (CPlayerInfo player in m_dicPlayersAndInfo.Values)
                                                        {
                                                            if (player.TeamID == int.Parse(targetTeam))
                                                            {
                                                                if (payloadFields[2] != string.Empty)
                                                                {
                                                                    this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%amount", item.Cost.ToString()), "player", player.SoldierName);
                                                                }

                                                                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player.SoldierName);
                                                            }
                                                        }
                                                        commandsSuccessful++;
                                                    }
                                                }
                                            }

                                            else
                                            {
                                                if (messageWords.Length < 2)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [Alpha/Bravo/...]", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    if (messageWords[1].ToLower() != "alpha" && messageWords[1].ToLower() != "bravo" && messageWords[1].ToLower() != "charlie" && messageWords[1].ToLower() != "delta" && messageWords[1] != "1" && messageWords[1] != "2" && messageWords[1].ToLower() != "3" && messageWords[1].ToLower() != "4")
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [Alpha/Bravo/...]", "player", strSpeaker);
                                                    }

                                                    else if (SquadCensus("read", int.Parse(messageWords[1].ToLower().Replace("alpha", "1").Replace("bravo", "2").Replace("charlie", "3").Replace("delta", "4")), 0) == 0)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. The team/squad you specified is empty!", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        string targetTeam = messageWords[1].ToLower().Replace("alpha", "1").Replace("bravo", "2").Replace("charlie", "3").Replace("delta", "4");

                                                        foreach (CPlayerInfo player in m_dicPlayersAndInfo.Values)
                                                        {
                                                            if (player.TeamID == int.Parse(targetTeam))
                                                            {
                                                                if (payloadFields[2] != string.Empty)
                                                                {
                                                                    this.ExecuteCommand("procon.protected.send", "admin.say", payloadFields[2].Replace("%buyer", strSpeaker).Replace("%amount", item.Cost.ToString()), "player", player.SoldierName);
                                                                }

                                                                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player.SoldierName);
                                                            }
                                                        }
                                                        commandsSuccessful++;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("TempBan") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), payloadFields[4]);
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else if (payloadFields.Length > 4)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", BestMatchPlayerName(messageWords[1].Replace("&", " ")), "seconds", ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), payloadFields[4].Replace("%buyer", strSpeaker));
                                                    commandsSuccessful++;
                                                }

                                                else
                                                {
                                                    if (messageWords.Length < 3)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [message]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        int i = 2;
                                                        string tempBanMessage = "";

                                                        do
                                                        {
                                                            tempBanMessage = tempBanMessage + " " + messageWords[i];
                                                            i++;
                                                        }
                                                        while (i < messageWords.Length);

                                                        tempBanMessage = strSpeaker + ": " + tempBanMessage;

                                                        if (tempBanMessage.Length > 50)
                                                        {
                                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Your message must be under 50 characters long.", "player", strSpeaker);
                                                        }

                                                        else
                                                        {
                                                            this.ExecuteCommand("procon.protected.send", "banList.add", "name", BestMatchPlayerName(messageWords[1].Replace("&", " ")), "seconds", ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), tempBanMessage);
                                                            commandsSuccessful++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("PermBan") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "perm", payloadFields[2]);
                                            this.ExecuteCommand("procon.protected.send", "banList.save");
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else if (payloadFields.Length > 2)
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", BestMatchPlayerName(messageWords[1].Replace("&", " ")), "perm", payloadFields[2]);
                                                    this.ExecuteCommand("procon.protected.send", "banList.save");
                                                    commandsSuccessful++;
                                                }

                                                else
                                                {
                                                    if (messageWords.Length < 3)
                                                    {
                                                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player] [message]", "player", strSpeaker);
                                                    }

                                                    else
                                                    {
                                                        int i = 2;
                                                        string permBanMessage = "";

                                                        do
                                                        {
                                                            permBanMessage = permBanMessage + " " + messageWords[i];
                                                            i++;
                                                        }
                                                        while (i < messageWords.Length);

                                                        permBanMessage = strSpeaker + ": " + permBanMessage;

                                                        if (permBanMessage.Length > 50)
                                                        {
                                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Your message must be under 50 characters long.", "player", strSpeaker);
                                                        }

                                                        else
                                                        {
                                                            this.ExecuteCommand("procon.protected.send", "banList.add", "name", BestMatchPlayerName(messageWords[1].Replace("&", " ")), "perm", permBanMessage);
                                                            this.ExecuteCommand("procon.protected.send", "banList.save");
                                                            commandsSuccessful++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("UnBan") == 0)
                                    {
                                        if (messageWords.Length < 2)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                        }

                                        else
                                        {
                                            this.ExecuteCommand("procon.protected.send", "banList.remove", "player", messageWords[1].Replace("&", " "));
                                            this.ExecuteCommand("procon.protected.send", "banList.save");
                                            commandsSuccessful++;
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("RestartRound") == 0)
                                    {
                                        this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_RestartingRound", "5", "1", "1", "procon.protected.send", "admin.restartMap");
                                        commandsSuccessful++;
                                    }

                                    if (payloadFields[0].CompareTo("NextLevel") == 0)
                                    {
                                        this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_NextLevel", "5", "1", "1", "procon.protected.send", "admin.runNextLevel");
                                        commandsSuccessful++;
                                    }

                                    if (payloadFields[0].CompareTo("ReserveAdd") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "reservedSlots.addPlayer", strSpeaker);
                                            this.ExecuteCommand("procon.protected.send", "reservedSlots.save");
                                            this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_ReserveRemove_" + strSpeaker, ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "reservedSlots.removePlayer", strSpeaker);
                                            this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_ReserveRemoveSave_" + strSpeaker, ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "reservedSlots.save");
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "reservedSlots.addPlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                    this.ExecuteCommand("procon.protected.send", "reservedSlots.save");
                                                    this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_ReserveRemove_" + messageWords[1], ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "reservedSlots.removePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                    this.ExecuteCommand("procon.protected.tasks.add", m_strCurrencyInstanceName + "_ReserveRemoveSave_" + messageWords[1], ((int.Parse(payloadFields[2])) * (int.Parse(payloadFields[3].Replace("Seconds", "1").Replace("Minutes", "60").Replace("Hours", "3600").Replace("Days", "86400").Replace("Weeks", "604800").Replace("Months", "2419200")))).ToString(), "1", "1", "procon.protected.send", "reservedSlots.save");
                                                    commandsSuccessful++;
                                                }
                                            }
                                        }
                                    }

                                    if (payloadFields[0].CompareTo("ReserveRemove") == 0)
                                    {
                                        if (payloadFields[1].CompareTo(" Buyer") == 0)
                                        {
                                            this.ExecuteCommand("procon.protected.send", "reservedSlots.removePlayer", strSpeaker);
                                            this.ExecuteCommand("procon.protected.send", "reservedSlots.save");
                                            commandsSuccessful++;
                                        }

                                        else if (payloadFields[1].CompareTo(" Player") == 0)
                                        {
                                            if (messageWords.Length < 2)
                                            {
                                                this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Error. Syntax: " + item.BuyCommand + " [player]", "player", strSpeaker);
                                            }

                                            else
                                            {
                                                if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "many")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": '" + messageWords[1] + "' matches too many names. Be more specific.", "player", strSpeaker);
                                                }

                                                else if (BestMatchPlayerName(messageWords[1].Replace("&", " ")) == "none")
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strCurrencyInstanceName + ": Player '" + messageWords[1] + "' not found. Check your spelling.", "player", strSpeaker);
                                                }

                                                else
                                                {
                                                    this.ExecuteCommand("procon.protected.send", "reservedSlots.removePlayer", BestMatchPlayerName(messageWords[1].Replace("&", " ")));
                                                    this.ExecuteCommand("procon.protected.send", "reservedSlots.save");
                                                    commandsSuccessful++;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (commandsSuccessful == item.Payload.Count)
                                {
                                    m_dicPlayersAndBalances[strSpeaker] -= item.Cost;

                                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", m_strCurrencyInstanceName + ": " + strSpeaker + " just purchased a " + item.Name + " for " + item.Cost + " " + m_strCurrencySymbol + ". She has " + m_dicPlayersAndBalances[strSpeaker] + " " + m_strCurrencySymbol + " left.");
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                this.m_strPreviousMessage = strMessage;
            }
        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.m_dicPlayersAndBalances.ContainsKey(cpiPlayer.SoldierName) == false)
                    {
                        if (this.CheckAccountByName(cpiPlayer.SoldierName) == false)
                        {
                            this.CreateSQLAccount(cpiPlayer.GUID.ToString(), cpiPlayer.SoldierName);
                            this.m_dicPlayersAndBalances.Add(cpiPlayer.SoldierName, 0);
                        }

                        else
                        {
                            int balanceFromSQL = int.Parse(GetBalance(cpiPlayer.SoldierName));
                            this.m_dicPlayersAndBalances.Add(cpiPlayer.SoldierName, balanceFromSQL);
                        }
                    }

                    if (this.m_dicPlayersAndInfo.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        this.m_dicPlayersAndInfo[cpiPlayer.SoldierName] = cpiPlayer;
                    }

                    else
                    {
                        this.m_dicPlayersAndInfo.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }

                    if (this.m_ebynEnablePayroll == enumBoolYesNo.Yes)
                    {
                        this.PayrollManager(cpiPlayer.SoldierName);
                    }

                    if (this.m_dicOldPlayersAndBalances.ContainsKey(cpiPlayer.SoldierName))
                    {
                        if (this.m_dicOldPlayersAndBalances[cpiPlayer.SoldierName] != this.m_dicPlayersAndBalances[cpiPlayer.SoldierName])
                        {
                            this.SetCurrency(this.m_dicPlayersAndBalances[cpiPlayer.SoldierName], cpiPlayer.SoldierName);
                            this.m_dicOldPlayersAndBalances[cpiPlayer.SoldierName] = this.m_dicPlayersAndBalances[cpiPlayer.SoldierName];
                        }
                    }

                    else
                    {
                        this.m_dicOldPlayersAndBalances.Add(cpiPlayer.SoldierName, this.m_dicPlayersAndBalances[cpiPlayer.SoldierName]);
                    }
                }
                SquadCensus("update", 0, 0);
            }
        }

        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            int opk_iKillPayoff = 0;
            int opk_iCurrencyLost = 0;
            Weapon weaponUsed = this.GetWeaponDefines()[kKillerVictimDetails.DamageType];
            string weaponUsedName = this.GetLocalized(weaponUsed.Name, String.Format("global.Weapons.{0}", kKillerVictimDetails.DamageType.ToLower()));
            CPlayerInfo Killer = kKillerVictimDetails.Killer;
            CPlayerInfo Victim = kKillerVictimDetails.Victim;

            if (m_ebynPayForKills == enumBoolYesNo.Yes)
            {
                opk_iKillPayoff += m_iAmountPaidPerKill;
            }

            if (m_ebynDeductForDeaths == enumBoolYesNo.Yes)
            {
                opk_iCurrencyLost += m_iAmountLostPerDeath;
            }

            if (m_ebynPayForHeadshots == enumBoolYesNo.Yes)
            {
                if (kKillerVictimDetails.Headshot)
                {
                    opk_iKillPayoff += m_iAmountPaidPerHeadshot;
                }
            }

            if (m_ebynPayForSpecificWeaponKills == enumBoolYesNo.Yes)
            {
                string[] weaponAndValue = new string[2];

                foreach (string weapAndPayValue in this.m_lstWeaponAndPayValue)
                {
                    weaponAndValue = Regex.Split(weapAndPayValue, "=");

                    if (weaponAndValue[0].Contains(weaponUsedName))
                    {
                        opk_iKillPayoff += int.Parse(weaponAndValue[1]);
                        break;
                    }
                }
            }

            if (m_ebynEnableMugging == enumBoolYesNo.Yes)
            {
                if (m_lstMuggingWeapons.Contains(weaponUsedName))
                {
                    int opk_iMuggingPayoff = MuggingHandler(Killer.SoldierName, Victim.SoldierName);
                    opk_iKillPayoff += opk_iMuggingPayoff;
                    opk_iCurrencyLost += opk_iMuggingPayoff;
                }
            }

            if (m_ebynEnableAutoBounties == enumBoolYesNo.Yes && Killer.SoldierName != Victim.SoldierName)
            {
                AutoBountyHandler(Killer.SoldierName, Victim.SoldierName);
            }

            if (m_ebynEnablePlayerBounties == enumBoolYesNo.Yes && Killer.SoldierName != Victim.SoldierName)
            {
                PlayerBountyHandler("kill", Killer.SoldierName, Victim.SoldierName, 0);
            }

            if (m_dicPlayersAndBounties.ContainsKey(Victim.SoldierName) && m_dicPlayersAndBounties[Victim.SoldierName] > 0 && Killer.SoldierName != Victim.SoldierName)
            {
                opk_iKillPayoff += m_dicPlayersAndBounties[Victim.SoldierName];
                m_dicPlayersAndBounties.Remove(Victim.SoldierName);
            }

            if (m_dicPlayerStreaks.ContainsKey(Victim.SoldierName))
            {
                m_dicPlayerStreaks.Remove(Victim.SoldierName);
            }

            if (opk_iKillPayoff > 0)
            {
                m_dicPlayersAndBalances[Killer.SoldierName] += opk_iKillPayoff;
            }

            if (opk_iCurrencyLost > 0 && m_dicPlayersAndBalances[Victim.SoldierName] >= opk_iCurrencyLost)
            {
                m_dicPlayersAndBalances[Victim.SoldierName] -= opk_iCurrencyLost;
            }
        }

        public void OnPluginEnable()
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                       "SERVER=" + m_strSQLHostname + ";" +
                                                       "PORT=3306;" +
                                                       "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                       "UID=" + m_strSQLUsername + ";" +
                                                       "PWD=" + m_strSQLPassword + ";" +
                                                       "OPTION=3;");

                    OdbcCon.Open();
                    string MakeTable = @"CREATE TABLE IF NOT EXISTS `tbl_currency` (
                                            `id` int(11) NOT NULL AUTO_INCREMENT,
                                            `playername` varchar(16) DEFAULT NULL,
                                            `CC` int(10) NOT NULL DEFAULT '0',
                                            `guid` varchar(35) DEFAULT NULL,
                                            PRIMARY KEY (`id`),
                                            UNIQUE KEY `Unique_Player` (`playername`,`guid`)
                                            ) ENGINE=MyISAM DEFAULT CHARSET=latin1";
                    OdbcCommand OdbcCom = new OdbcCommand(MakeTable, OdbcCon);
                    OdbcCom.ExecuteNonQuery();
                }

                catch (Exception c)
                {
                    if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                    {
                        if (c.ToString().Contains("Access denied for user 'ODBC'@'localhost' (using password: NO)") == false)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error: " + c);
                        }

                    }
                }
            }

            else
            {
                if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Currency error:  Please enter all of the MySQL database info in Plugin Settings.");
                }
            }

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bCurrency ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            this.m_dicPayrollSchedule.Clear();
            this.m_dicPlayersAndBalances.Clear();
            this.m_dicOldPlayersAndBalances.Clear();
            this.m_dicPlayersAndInfo.Clear();
            this.m_dicPlayerStreaks.Clear();
            this.m_dicSquadsAndPopulations.Clear();

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }

            if (m_ebynSpamPluginConsole == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bCurrency ^1Disabled! D:");
            }
        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.m_dicPlayersAndBalances.ContainsKey(strSoldierName) && this.m_dicOldPlayersAndBalances.ContainsKey(strSoldierName) && this.m_dicOldPlayersAndBalances[strSoldierName] != this.m_dicPlayersAndBalances[strSoldierName])
            {
                SetCurrency(m_dicPlayersAndBalances[strSoldierName], strSoldierName);
            }

            if (this.m_dicPlayersAndBalances.ContainsKey(strSoldierName))
            {
                this.m_dicPlayersAndBalances.Remove(strSoldierName);
            }

            if (this.m_dicOldPlayersAndBalances.ContainsKey(strSoldierName))
            {
                this.m_dicOldPlayersAndBalances.Remove(strSoldierName);
            }

            if (this.m_dicPayrollSchedule.ContainsKey(strSoldierName))
            {
                this.m_dicPayrollSchedule.Remove(strSoldierName);
            }

            if (m_strCurrentGameMode != "sqdm" && m_strCurrentGameMode != "sqrush")
            {
                if (m_dicSquadsAndPopulations.ContainsKey(m_dicPlayersAndInfo[strSoldierName].TeamID.ToString() + "." + m_dicPlayersAndInfo[strSoldierName].SquadID.ToString()))
                {
                    m_dicSquadsAndPopulations[m_dicPlayersAndInfo[strSoldierName].TeamID.ToString() + "." + m_dicPlayersAndInfo[strSoldierName].SquadID.ToString()]--;
                }
            }

            else
            {
                string checkSquad = "1." + m_dicPlayersAndInfo[strSoldierName].TeamID.ToString();

                if (m_dicSquadsAndPopulations.ContainsKey(checkSquad))
                {
                    m_dicSquadsAndPopulations[checkSquad]--;
                }
            }
            this.m_dicPlayersAndInfo.Remove(strSoldierName);
        }

        public void OnPlayerKicked(string strSoldierName, string strReason)
        {
            this.OnPlayerLeft(strSoldierName);
        }

        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.m_strCurrentGameMode = csiServerInfo.GameMode.ToLower();
        }

        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            if (this.m_dicPlayersAndInfo.ContainsKey(strSoldierName) == true)
            {
                m_dicSquadsAndPopulations[m_dicPlayersAndInfo[strSoldierName].TeamID.ToString() + "." + m_dicPlayersAndInfo[strSoldierName].SquadID.ToString()]--;
                this.m_dicPlayersAndInfo[strSoldierName].TeamID = iTeamID;
                this.m_dicPlayersAndInfo[strSoldierName].SquadID = iSquadID;
                m_dicSquadsAndPopulations[iTeamID.ToString() + "." + iSquadID.ToString()]++;
            }
        }

        public void OnPlayerSquadChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            this.OnPlayerTeamChange(strSoldierName, iTeamID, iSquadID);
        }

        public void OnLevelStarted()
        {
            m_dicPlayerStreaks.Clear();
            SquadCensus("update", 0, 0);
        }
    }
}
#endregion