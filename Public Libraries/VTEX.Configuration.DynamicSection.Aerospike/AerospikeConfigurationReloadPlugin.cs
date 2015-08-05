﻿using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VTEX.Configuration.DynamicSection.Aerospike
{
    public class AerospikeConfigurationReloadPlugin : IConfigurationReloadPlugin
    {
        private static readonly object LockState = new object();
        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding();
        private static readonly WritePolicy AerospikeWritePolicy = GetDefaultAerospikeWritePolicy();

        private static AsyncClient AerospikeAsyncClient = null;

        public event EventHandler<Tuple<string, object>> PropertyLoaded = delegate { };

        private static string appName = string.Empty;
        private static string appVersion = string.Empty;

        public void GetLastestPropertiesValues(ConfigurationSection configurationSection)
        {
            try
            {
                var clusterIps = configurationSection.ElementInformation.Properties[AerospikeConfigurationSection.Property_ClusterIps].Value.ToString();
                var port = (int)configurationSection.ElementInformation.Properties[AerospikeConfigurationSection.Property_Port].Value;
                CheckClient(clusterIps, port);

                var aerospikeNamespace = configurationSection.ElementInformation.Properties[AerospikeConfigurationSection.Property_Namespace].Value.ToString();
                var key = GetKey(aerospikeNamespace);

                var record = AerospikeAsyncClient.Get(new BatchPolicy() { }, key);
                foreach (PropertyInformation item in configurationSection.ElementInformation.Properties)
                {
                    if (record != null)
                    {
                        if (!record.bins.ContainsKey(item.Name))
                        {
                            SetData(key, item);
                        }
                        else if (item.Value.ToString() != record.bins[item.Name].ToString())
                        {
                            this.PropertyLoaded(this, new Tuple<string, object>(item.Name, record.bins[item.Name]));
                        }
                    }
                    else
                        SetData(key, item);
                }
            }
            catch
            {
                //Fail safe
            }
        }

        private static void SetData(Key key, PropertyInformation item)
        {
            AerospikeAsyncClient.Put(AerospikeWritePolicy, key, new Bin(item.Name, item.Value));
        }

        private static Key GetKey(string aerospikeNamespace)
        {
            return new Key(aerospikeNamespace, GetAppName(), GetAppVersion());
        }

        private static string GetAppName()
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                var executingAssembly = Assembly.GetExecutingAssembly();
                appName = executingAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            }
            return appName;
        }

        private static string GetAppVersion()
        {
            if (string.IsNullOrWhiteSpace(appVersion))
            {
                var executingAssembly = Assembly.GetExecutingAssembly();
                AssemblyInformationalVersionAttribute assemblyInformationalVersion = executingAssembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault() as AssemblyInformationalVersionAttribute;
                if (assemblyInformationalVersion == null)
                {
                    AssemblyFileVersionAttribute assemblyFileVersion = executingAssembly.GetCustomAttributes<AssemblyFileVersionAttribute>().FirstOrDefault() as AssemblyFileVersionAttribute;
                    if (assemblyFileVersion == null)
                        appVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location).ProductVersion;
                    else
                        appVersion = assemblyFileVersion.Version;
                }
                else
                    appVersion = assemblyInformationalVersion.InformationalVersion;

                appVersion = appVersion.Replace('.', '_').Replace('-', '_');
            }

            return appVersion;
        }

        private static void CheckClient(string hosts, int port)
        {
            if (AerospikeAsyncClient == null)
            {
                //Se o client for nulo, há um lock para que apenas 1 thread possa chamar a próxima parte de criação do cliente
                lock (LockState)
                {
                    //Aqui a uma nova checagem pois uma thread que estava parada do lock pode evitar essa passo, caso a thread anterior já tenha criado o client 
                    if (AerospikeAsyncClient == null)
                    {
                        List<Host> aerospikeHosts = new List<Host>();
                        foreach (string hostIp in hosts.Split(new char[] { ',' }))
                        {
                            aerospikeHosts.Add(new Host(hostIp, port));
                        }
                        AerospikeAsyncClient = new AsyncClient(
                            new AsyncClientPolicy()
                            {
                                asyncMaxCommandAction = MaxCommandAction.REJECT,
                                failIfNotConnected = false
                            },
                            aerospikeHosts.ToArray());
                    }
                }
            }

        }

        private static WritePolicy GetDefaultAerospikeWritePolicy()
        {
            WritePolicy writePolicy = new WritePolicy();
            writePolicy.recordExistsAction = RecordExistsAction.UPDATE;
            writePolicy.commitLevel = CommitLevel.COMMIT_MASTER;
            writePolicy.expiration = 0;
            writePolicy.timeout = 300;
            return writePolicy;
        }
    }
}