
        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Messages (one per line)|Messages", typeof(string[]), this.m_lstMessages.ToArray()));
			lstReturn.Add(new CPluginVariable("Messages Yell Timing|Yell duration (seconds)", this.m_iYellDisplayTime.GetType(), this.m_iYellDisplayTime / 1000));
            lstReturn.Add(new CPluginVariable("Timing|Interval between messages (seconds)", this.m_iIntervalBetweenMessages.GetType(), this.m_iIntervalBetweenMessages));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables() {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Messages", typeof(string[]), this.m_lstMessages.ToArray()));
			lstReturn.Add(new CPluginVariable("Yell duration (seconds)", this.m_iYellDisplayTime.GetType(), this.m_iYellDisplayTime / 1000));	
            lstReturn.Add(new CPluginVariable("Interval between messages (seconds)", this.m_iIntervalBetweenMessages.GetType(), this.m_iIntervalBetweenMessages));			

            return lstReturn;
        }