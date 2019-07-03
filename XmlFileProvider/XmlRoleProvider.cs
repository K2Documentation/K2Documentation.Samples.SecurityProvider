using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourceCode.Hosting.Server.Interfaces;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace SourceCode.Security.Providers
{
    public class XmlRoleProvider : IHostableSecurityProvider, IRoleProvider
    {
        /// <summary>
        /// Provider schema namespace
        /// </summary>
        private const string NS = "http://schemas.K2.com/security/xmlUserProvider.xsd";

        /// <summary>
        /// Provider namespace manager
        /// </summary>
        private XmlNamespaceManager nsm;

        /// <summary>
        /// Provider local store
        /// </summary>
        private XmlDocument _userStore;
        
        /// <summary>
        /// Provider security label
        /// </summary>
        private string _label;

        /// <summary>
        /// Provider local store document
        /// </summary>
        private string _providerFile;

        /// <summary>
        /// User properties
        /// </summary>
        private Dictionary<string, string> _userProperties;

        /// <summary>
        /// Group properties
        /// </summary>
        private Dictionary<string, string> _groupProperties;

        /// <summary>
        /// Provider User collection
        /// </summary>
        private Dictionary<string, IUser> _users;

        /// <summary>
        /// Provider Group collection
        /// </summary>
        private Dictionary<string, IGroup> _groups;

        /// <summary>
        /// Provider Group Member collection
        /// </summary>
        private Dictionary<string, Dictionary<string, IdentityType>> _groupMembers;

        /// <summary>
        /// Provider User Groups collection
        /// </summary>
        private Dictionary<string, List<string>> _userGroups;

        /// <summary>
        /// Synchronise execution
        /// </summary>
        private ReaderWriterLock _lock = new ReaderWriterLock();

        /// <summary>
        /// Provider supporting Xml as the datasource
        /// </summary>
        public XmlRoleProvider()
        {
            // Supported User properties
            _userProperties = new Dictionary<string, string>();

            _userProperties.Add("DisplayName", typeof(string).FullName);
            _userProperties.Add("Name", typeof(string).FullName);
            _userProperties.Add("Manager", typeof(string).FullName);
            _userProperties.Add("Email", typeof(string).FullName);
            _userProperties.Add("Description", typeof(string).FullName);
            _userProperties.Add("CommonName", typeof(string).FullName);
            _userProperties.Add("UserPrincipalName", typeof(string).FullName);
            _userProperties.Add("ObjectSID", typeof(string).FullName);
            _userProperties.Add("SipAccount", typeof(string).FullName);

            // Supported Group properties
            _groupProperties = new Dictionary<string, string>();

            _groupProperties.Add("FullName", typeof(string).FullName);
            _groupProperties.Add("Name", typeof(string).FullName);
            _groupProperties.Add("Description", typeof(string).FullName);
            _groupProperties.Add("Email", typeof(string).FullName);

            _userStore = new XmlDocument();

            // Xml user store source
            _providerFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UserStore.xml");

            // Load Xml user store
            LoadUserStore();
        }

        #region IRoleProvider Members

        /// <summary>
        /// Match property values over collections
        /// </summary>
        /// <param name="itemProperties"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private bool Match(IDictionary<string, object> itemProperties, IDictionary<string, object> properties)
        {
            bool match = true;
            foreach (var prop in properties)
            {
                // Contains the property?
                if (!itemProperties.ContainsKey(prop.Key))
                    continue;

                string propVal = prop.Value == null ? null : prop.Value.ToString();
                string itemVal = itemProperties[prop.Key] == null ? null : itemProperties[prop.Key].ToString();
                if (propVal == null)
                    continue;

                if (propVal.EndsWith("*") && propVal.StartsWith("*"))
                {
                    propVal = propVal.Replace("*", "");
                    if (itemVal == null && propVal != "")
                    {
                        match = false;
                        break;
                    }
                    match &= itemVal.ToLower().Contains(propVal.ToLower());
                }
                else if (propVal.StartsWith("*"))
                {
                    propVal = propVal.Replace("*", "");
                    if (itemVal == null && propVal != null)
                    {
                        match = false;
                        break;
                    }
                    match &= itemVal.EndsWith(propVal, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (propVal.EndsWith("*"))
                {
                    propVal = propVal.Replace("*", "");
                    if (itemVal == null && propVal != null)
                    {
                        match = false;
                        break;
                    }
                    match &= itemVal.StartsWith(propVal, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (string.Compare(propVal, itemVal, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    match = false;
                    break;
                }
            }
            return match;
        }

        /// <summary>
        /// Return matching groups
        /// </summary>
        /// <param name="col"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private IGroupCollection FilterGroupCollection(IGroupCollection col, IDictionary<string, object> properties)
        {
            GroupCollection retCol = new GroupCollection();
            foreach (IGroup item in col)
            {
                if (Match(item.Properties, properties))
                    retCol.Add(item);
            }
            return retCol;
        }

        /// <summary>
        /// Return matching users
        /// </summary>
        /// <param name="col"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private IUserCollection FilterUserCollection(IUserCollection col, IDictionary<string, object> properties)
        {

            UserCollection retCol = new UserCollection();
            foreach (IUser item in col)
            {
                if (Match(item.Properties, properties))
                    retCol.Add(item);
            }
            return retCol;
        }

        /// <summary>
        /// Return groups
        /// </summary>
        /// <param name="userName">When present returns groups containing the username</param>
        /// <param name="properties">Matches groups using property values</param>
        /// <returns></returns>
        public IGroupCollection FindGroups(string userName, IDictionary<string, object> properties)
        {
            return FindGroups(userName, properties, null);
        }

        /// <summary>
        /// Return groups
        /// </summary>
        /// <param name="userName">When present returns groups containing the username</param>
        /// <param name="properties">Matches groups using property values</param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public IGroupCollection FindGroups(string userName, IDictionary<string, object> properties, string extraData)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IRoleProvider.FindGroups Name:{0}", userName));

            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);

                GroupCollection col = new GroupCollection();
                if (!string.IsNullOrEmpty(userName))
                {
                    if (_userGroups.ContainsKey(RemoveSecurityLabel(userName)))
                    {
                        foreach (var item in _userGroups[RemoveSecurityLabel(userName)])
                        {
                            col.Add(_groups[item]);
                        }
                    }
                    //Get Nested Groups
                    for (int i = 0; i < col.Count; i++)
                    {
                        IGroup item = col[i];
                        foreach (var entry in _groupMembers)
                        {
                            if (entry.Value.ContainsKey(item.GroupID) && entry.Value[item.GroupID] == IdentityType.Group)
                            {
                                col.Add(_groups[entry.Key]);
                            }
                        }
                    }
                    return FilterGroupCollection(col, properties);
                }
                else
                {
                    foreach (var item in _groups.Values)
                    {
                        col.Add(item);
                    }
                }
                return FilterGroupCollection(col, properties);

            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Return users
        /// </summary>
        /// <param name="groupName">When present returns users that are part of the groupname</param>
        /// <param name="properties">Matches users using property values</param>
        /// <returns></returns>
        public IUserCollection FindUsers(string groupName, IDictionary<string, object> properties)
        {
            return FindUsers(groupName, properties, null);
        }

        /// <summary>
        /// Return users that are part of the groupname
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private UserCollection GetUsersForGroup(string groupName)
        {
            UserCollection col = new UserCollection();
            foreach (var item in _groupMembers[RemoveSecurityLabel(groupName)])
            {
                if (item.Value == IdentityType.User)
                    col.Add(_users[item.Key]);
                else if (item.Value == IdentityType.Group)
                {
                    UserCollection nestedCol = GetUsersForGroup(item.Key);
                    foreach (IUser nestedUser in nestedCol)
                    {
                        col.Add(nestedUser);
                    }
                }
            }
            return col;
        }

        /// <summary>
        /// Return users
        /// </summary>
        /// <param name="groupName">When present returns users that are part of the groupname</param>
        /// <param name="properties">Matches users using property values</param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public IUserCollection FindUsers(string groupName, IDictionary<string, object> properties, string extraData)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IRoleProvider.FindUsers Name:{0}", groupName));

            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                UserCollection col = null;
                if (!string.IsNullOrEmpty(groupName))
                {
                    col = GetUsersForGroup(groupName);
                    return FilterUserCollection(col, properties);
                }
                else
                {
                    col = new UserCollection();
                    foreach (var item in _users.Values)
                    {
                        col.Add(item);
                    }
                    return FilterUserCollection(col, properties);
                }
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Ensures that a name is in a desired format domain\username
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string FormatItemName(string name)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IRoleProvider.FormatItemName Name:{0}", name));

            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                foreach (var item in _users.Keys)
                {
                    if (item.EndsWith("\\" + name))
                        return item;
                }

                foreach (var item in _groups.Keys)
                {
                    if (item.EndsWith("\\" + name))
                        return item;
                }
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
            return name;
        }

        /// <summary>
        /// Return group
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IGroup GetGroup(string name)
        {
            return GetGroup(name, null);
        }

        /// <summary>
        /// Return group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public IGroup GetGroup(string name, string extraData)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IRoleProvider.GetGroup Name:{0}", name));

            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);

                if (_groups.ContainsKey(RemoveSecurityLabel(name)))
                {
                    return _groups[RemoveSecurityLabel(name)];
                }
                return null;
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Return user
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IUser GetUser(string name)
        {
            return GetUser(name, null);
        }

        /// <summary>
        /// Return user
        /// </summary>
        /// <param name="name"></param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public IUser GetUser(string name, string extraData)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IRoleProvider.GetUser Name:{0}", name));

            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);

                if (_users.ContainsKey(RemoveSecurityLabel(name)))
                {
                    return _users[RemoveSecurityLabel(name)];
                }
                return null;
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Initializes the AuthenticationProvider
        /// </summary>
        /// <param name="label"></param>
        /// <param name="roleInit"></param>
        void IAuthenticationProvider.Init(string label, string roleInit)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IAuthenticationProvider.Init Label:{0}", label));

            if (_label != null)
                return;
            _label = label;
        }

        /// <summary>
        /// Initializes the RoleProvider
        /// </summary>
        /// <param name="label"></param>
        /// <param name="roleInit"></param>
        void IRoleProvider.Init(string label, string roleInit)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IRoleProvider.Init Label:{0}", label));

            if (_label != null)
                return;
            _label = label;
        }

        /// <summary>
        /// Load the user and group relationship store
        /// </summary>
        private void LoadUserStore()
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.LoadUserStore"));

            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _userStore.Load(_providerFile);

                nsm = new XmlNamespaceManager(_userStore.NameTable);
                nsm.AddNamespace("k", NS);

                _users = new Dictionary<string, IUser>(StringComparer.CurrentCultureIgnoreCase);

                _groupMembers = new Dictionary<string, Dictionary<string, IdentityType>>(StringComparer.CurrentCultureIgnoreCase);

                _userGroups = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);

                _groups = new Dictionary<string, IGroup>(StringComparer.CurrentCultureIgnoreCase);

                XmlNode users = _userStore.DocumentElement.SelectSingleNode("k:users", nsm);

                XmlNode groups = _userStore.DocumentElement.SelectSingleNode("k:groups", nsm);

                foreach (XmlNode item in users.SelectNodes("k:user", nsm))
                {
                    User user = new User();

                    user.UserID = item.Attributes.GetNamedItem("name").Value;
                    user.UserName = item.Attributes.GetNamedItem("name").Value;

                    foreach (var p in _userProperties)
                    {
                        user.Properties[p.Key] = null;
                    }

                    user.Properties["DisplayName"] = item.Attributes.GetNamedItem("fullName").Value;
                    user.Properties["Manager"] = item.Attributes.GetNamedItem("manager").Value;
                    user.Properties["Name"] = item.Attributes.GetNamedItem("name").Value;
                    user.Properties["Description"] = item.Attributes.GetNamedItem("description").Value;
                    user.Properties["Email"] = item.Attributes.GetNamedItem("email").Value;
                    user.Properties["CommonName"] = item.Attributes.GetNamedItem("commonName").Value;
                    user.Properties["UserPrincipalName"] = item.Attributes.GetNamedItem("userPrincipalName").Value;
                    user.Properties["ObjectSID"] = item.Attributes.GetNamedItem("objectSID").Value;
                    user.Properties["Password"] = item.Attributes.GetNamedItem("password").Value;

                    _users[user.UserID] = user;
                }

                foreach (XmlNode item in groups.SelectNodes("k:group", nsm))
                {
                    Group group = new Group();

                    group.GroupID = item.Attributes.GetNamedItem("name").Value;
                    group.GroupName = item.Attributes.GetNamedItem("name").Value;
                    group.Properties["Name"] = item.Attributes.GetNamedItem("name").Value;
                    group.Properties["Description"] = item.Attributes.GetNamedItem("description").Value;
                    group.Properties["Email"] = item.Attributes.GetNamedItem("email").Value;

                    Dictionary<string, IdentityType> members = null;

                    if (!_groups.ContainsKey(group.GroupID))
                    {
                        members = new Dictionary<string, IdentityType>(StringComparer.CurrentCultureIgnoreCase);

                        _groups.Add(group.GroupID, group);
                        _groupMembers.Add(group.GroupID, members);
                    }
                    else
                    {
                        members = _groupMembers[group.GroupID];
                    }

                    foreach (XmlNode member in item.SelectNodes("k:member", nsm))
                    {
                        List<string> userGroupList;
                        if (!_userGroups.ContainsKey(member.Attributes.GetNamedItem("name").Value))
                        {
                            userGroupList = new List<string>();
                            _userGroups.Add(member.Attributes.GetNamedItem("name").Value, userGroupList);
                        }
                        else
                            userGroupList = _userGroups[member.Attributes.GetNamedItem("name").Value];

                        if (!userGroupList.Contains(group.GroupID))
                            userGroupList.Add(group.GroupID);

                        members[member.Attributes.GetNamedItem("name").Value] = (IdentityType)Enum.Parse(typeof(IdentityType), member.Attributes.GetNamedItem("type").Value);
                    }
                }

                Trace.WriteLine(string.Format("XmlRoleProvider.LoadUserStore(Loaded)"));
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public string Login(string connectionString)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.Login"));

            throw new NotImplementedException("XmlRoleProvider.Login");
        }

        /// <summary>
        /// Return the supported group properties
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> QueryGroupProperties()
        {
            return _groupProperties;
        }

        /// <summary>
        /// Return the supported user properties
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> QueryUserProperties()
        {
            return _userProperties;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public System.Collections.ArrayList ResolveQueue(string data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove the security label on the user or group name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string RemoveSecurityLabel(string name)
        {
            if (name.Contains(":"))
                return name.Substring(name.IndexOf(":") + 1);
            else
                return name;
        }

        #endregion

        #region IHostableSecurityProvider Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool RequiresAuthentication()
        {
            return false;
        }

        #endregion

        #region IHostableType Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceMarshalling"></param>
        /// <param name="serverMarshaling"></param>
        public void Init(IServiceMarshalling serviceMarshalling, IServerMarshaling serverMarshaling)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IHostableType.Init"));
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Unload()
        {
            if (_userProperties != null)
            {
                _userProperties.Clear();
                _userProperties = null;
            }
            if (_groupProperties != null)
            {
                _groupProperties.Clear();
                _groupProperties = null;
            }
            if (_users != null)
            {
                _users.Clear();
                _users = null;
            }
            if (_groups != null)
            {
                _groups.Clear();
                _groups = null;
            }
            if (_groupMembers != null)
            {
                _groupMembers.Clear();
                _groupMembers = null;
            }
            if (_userGroups != null)
            {
                _userGroups.Clear();
                _userGroups = null;
            }

            _lock = null;
        }

        #endregion

        #region IAuthenticationProvider Members

        /// <summary>
        /// Authenticate the user against this provider
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public bool AuthenticateUser(string userName, string password, string extraData)
        {
            Trace.WriteLine(string.Format("XmlRoleProvider.IAuthenticationProvider.AuthenticateUser {0}", userName));

            if (_users.ContainsKey(userName))
                return string.Compare(_users[userName].Properties["Password"] as string, password) == 0;
            return false;
        }

        #endregion
    }
}
