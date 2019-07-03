using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourceCode.Hosting.Server.Interfaces;
using System.Collections;

namespace SourceCode.Security.Providers
{

    /// <summary>
    /// Provider User 
    /// </summary>
    class User: IUser
    {

        /// <summary>
        /// Provider User
        /// </summary>
        public User()
        {
            Properties = new Dictionary<string, object>();
        }
        
        #region IUser Members

        /// <summary>
        /// Provider supported user properties
        /// </summary>
        public IDictionary<string, object> Properties
        {
            get;
            set;
        }

        /// <summary>
        /// ID of the user
        /// </summary>
        public string UserID
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the User
        /// </summary>
        public string UserName
        {
            get;
            set;
        }

        #endregion
    }

    /// <summary>
    /// Collection of provider users
    /// </summary>
    class UserCollection : CollectionBase, IUserCollection
    {

        /// <summary>
        /// Collection constructor
        /// </summary>
        public UserCollection()
        {

        }

        /// <summary>
        /// Adds the provider user to the collection
        /// </summary>
        /// <param name="user"></param>
        public void Add(IUser user)
        {
            base.InnerList.Add(user);
        }

        #region IUserCollection Members

        /// <summary>
        /// Returns a user at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IUser this[int index]
        {
            get { return (IUser) base.InnerList[index]; }
        }

        #endregion
    }

}
