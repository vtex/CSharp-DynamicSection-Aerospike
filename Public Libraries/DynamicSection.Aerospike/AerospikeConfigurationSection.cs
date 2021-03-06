﻿using System.Configuration;

namespace DynamicSection.Aerospike
{
    /// <summary>
    /// Define a configuration section capable of save/retrieve properties from values at an Aerospike cluster
    /// </summary>
    public class AerospikeConfigurationSection : DynamicConfigurationSection
    {
        internal const string Property_ClusterIps = "clusterIps";
        internal const string Property_ClusterDns = "clusterDns";
        internal const string Property_Port = "port";
        internal const string Property_Namespace = "namespace";

        /// <summary>
        /// Get or set a string array splitted by ',' with aerospike nodes ips
        /// </summary>
        [ConfigurationProperty(Property_ClusterIps, IsRequired = false)]
        public string ClusterIps
        {
            get
            {
                return this[Property_ClusterIps].ToString();
            }
            set
            {
                this[Property_ClusterIps] = value;
            }
        }

        /// <summary>
        /// Get or set a string array splitted by ',' with aerospike nodes ips
        /// </summary>
        [ConfigurationProperty(Property_ClusterDns, IsRequired = false)]
        public string ClusterDns
        {
            get
            {
                return this[Property_ClusterDns].ToString();
            }
            set
            {
                this[Property_ClusterDns] = value;
            }
        }

        /// <summary>
        /// Get or set aerospike nodes port
        /// </summary>
        [ConfigurationProperty(Property_Port, IsRequired = false, DefaultValue = 3000)]
        public int Port
        {
            get
            {
                return int.Parse(this[Property_Port].ToString());
            }
            set
            {
                this[Property_Port] = value;
            }
        }

        /// <summary>
        /// Get or set aerospike namespace where data will be stored/retreived
        /// </summary>
        [ConfigurationProperty(Property_Namespace, IsRequired = true)]
        public string Namespace
        {
            get
            {
                return this[Property_Namespace].ToString();
            }
            set
            {
                this[Property_Namespace] = value;
            }
        }
    }
}