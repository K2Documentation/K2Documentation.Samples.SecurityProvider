using System;
using System.Collections.Generic;
using System.Text;

using SourceCode.Hosting.Server.Interfaces;

namespace MyCompany.MySecurity
{
    public class SampleSecurityProvider : IHostableSecurityProvider
    {
        #region Private Members

        private ISecurityManager _securityManager;
        private IConfigurationManager _cfgMgr;
        private string _labelName;
        private string _url;

        private BackEndProvider _backEndProvider;

        #endregion

        #region IHostableSecurityProvider Members

        public bool RequiresAuthentication()
        {
            return false;
        }

        #endregion

        #region IHostableType Members

        public void Init(IServiceMarshalling ServiceMarshalling, IServerMarshaling ServerMarshaling)
        {
            //Initialize resources when Host Server starts up. This gets called when Authentication and RoleProvider
            //gets instantiated when HostServer starts up.

            _cfgMgr = ServiceMarshalling.GetConfigurationManagerContext();
            _securityManager = ServerMarshaling.GetSecurityManagerContext();
        }

        public void Unload()
        {
            //Unload resources when Host Server Shuts down...
        }

        #endregion

        #region IAuthenticationProvider Members

        bool IAuthenticationProvider.AuthenticateUser(string userName, string password, string extraData)
        {
            //Interpret extra data - this can be a token that has been encrypted...
            SecurityToken token = SecurityToken.CreateToken(extraData);
            
            //Connect to webservice using init value set from config....
            BackEndProvider bp = new BackEndProvider();
            bp.Initialize(_url);

            return bp.Authenticate(userName, password, token);
        }

        void IAuthenticationProvider.Init(string label, string authInit)
        {
            _labelName = label;

            _url = authInit;
        }

        string IAuthenticationProvider.Login(string connectionString)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IRoleProvider Members

        void IRoleProvider.Init(string label, string authInit)
        {
            _labelName = label;

            _url = authInit;
        }

        IGroupCollection IRoleProvider.FindGroups(string userName, IDictionary<string, object> properties)
        {
            //Connect to webservice using init value set from config....
            BackEndProvider bp = new BackEndProvider();
            bp.Initialize(_url);

            IGroupCollection groups = bp.GetGroups(userName, properties);

            return groups;

            //Implement all other methods that has not been implemented in this sample in the same way....
            //Get User/Group objects from back end and return objects that implement IUser/IGroup etc.

        }

        IGroupCollection IRoleProvider.FindGroups(string userName, IDictionary<string, object> properties, string extraData)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        IUserCollection IRoleProvider.FindUsers(string groupName, IDictionary<string, object> properties)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        IUserCollection IRoleProvider.FindUsers(string groupName, IDictionary<string, object> properties, string extraData)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        string IRoleProvider.FormatItemName(string name)
        {
            //Return name that Provider understandes:
            //ex: if name = Johan and your BackEnd Provider uses ad domain notation this must return domain\johan
            //the format for a provider will typically be set in Init using the AuthInit parameter or RoleInit parameter.
            return name;
        }

        IGroup IRoleProvider.GetGroup(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        IGroup IRoleProvider.GetGroup(string name, string extraData)
        {
            throw new Exception("The method or operation is not implemented.");
            //Interpret extra data - this can be a token that has been encrypted...
            SecurityToken token = SecurityToken.CreateToken(extraData);

            //Connect to webservice using init value set from config....
            BackEndProvider bp = new BackEndProvider();
            bp.Initialize(_url);
        }

        IUser IRoleProvider.GetUser(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        IUser IRoleProvider.GetUser(string name, string extraData)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        Dictionary<string, string> IRoleProvider.QueryGroupProperties()
        {
            Dictionary<string, string> groupProps = new Dictionary<string,string>();
            groupProps.Add("Name", "System.String");
            groupProps.Add("Description", "System.String");
            groupProps.Add("Another Group Property", "System.String");

            return groupProps;
        }

        Dictionary<string, string> IRoleProvider.QueryUserProperties()
        {
            Dictionary<string, string> userProperties = new Dictionary<string,string>();
            userProperties.Add("Name", "System.String");
            userProperties.Add("Description", "System.String");
            userProperties.Add("Email", "System.String");
            userProperties.Add("Manager", "System.String");
            userProperties.Add("AdditionalCustomProperties", "String.Empty");

            return userProperties;
        }

        System.Collections.ArrayList IRoleProvider.ResolveQueue(string data)
        {
            return null;
            //Backwards compatibility...
        }

        string IRoleProvider.Login(string connectionString)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }

    public class SecurityToken
    {
        public static SecurityToken CreateToken(string encryptedToken)
        {
            return new SecurityToken();
        }

        public string AProperty = string.Empty;
        public string BProperty = string.Empty;
    }

    public class BackEndProvider
    {
        public bool Initialize(string url)
        {
            //Connect to actual webService...
            return true;
        }

        public bool Authenticate(string userName, string passWord, SecurityToken securityToken)
        {
            //Autheticate user...
            return true;
        }

        public string GetUser(string name)
        {
            //get user from backend and convert to string....
            return string.Empty;
        }
    
        public IGroupCollection GetGroups(string userName,IDictionary<string,object> properties)    
        {
 	        throw new Exception("The method or operation is not implemented.");
        }      
    }
}
