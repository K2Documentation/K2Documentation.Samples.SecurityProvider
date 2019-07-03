using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourceCode.Hosting.Server.Interfaces;
using System.Collections;

namespace SourceCode.Security.Providers
{

    /// <summary>
    /// Provider Group
    /// </summary>
    class Group : IGroup
    {

        /// <summary>
        /// Provider User
        /// </summary>
        public Group()
        {
            Properties = new Dictionary<string, object>();
        }

        #region IGroup Members

        /// <summary>
        /// ID of the group
        /// </summary>
        public string GroupID
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the User
        /// </summary>
        public string GroupName
        {
            get;
            set;
        }

        /// <summary>
        /// Provider supported group properties
        /// </summary>
        public IDictionary<string, object> Properties
        {
            get;
            set;
        }

        #endregion
    }

    /// <summary>
    /// Collection of provider groups
    /// </summary>
    class GroupCollection : CollectionBase, IGroupCollection
    {

        /// <summary>
        /// Collection constructor
        /// </summary>
        public GroupCollection()
        {

        }
        
        /// <summary>
        /// Adds the provider group to the collection
        /// </summary>
        /// <param name="group"></param>
        public void Add(IGroup group)
        {
            base.InnerList.Add(group);
        }

        #region IGroupCollection Members

        /// <summary>
        /// Returns a group at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IGroup this[int index]
        {
            get { return (IGroup)base.InnerList[index]; }
        }

        #endregion

      
    }

}
