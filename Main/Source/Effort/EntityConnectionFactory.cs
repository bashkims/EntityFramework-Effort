﻿#region License

// Copyright (c) 2011 Effort Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

using System;
using System.Configuration;
using System.Data.Common;
using System.Data.EntityClient;
using Effort.Internal.Caching;
using Effort.Internal.Common;
using Effort.Provider;
using System.Data.Metadata.Edm;
using System.Reflection;
using Effort.DataProviders;

namespace Effort
{
    public static class EntityConnectionFactory
    {

        #region Persistent

        public static EntityConnection CreatePersistent(string entityConnectionString, IDataProvider dataProvider)
        {
            MetadataWorkspace metadata = GetMetadataWorkspace(ref entityConnectionString);

            DbConnection connection = DbConnectionFactory.CreatePersistent(entityConnectionString);

            return CreateEntityConnection(metadata, connection);
        }

        public static EntityConnection CreatePersistent(string entityConnectionString)
        {
            return CreatePersistent(entityConnectionString);
        }

        #endregion

        #region Transient

        public static EntityConnection CreateTransient(string entityConnectionString, IDataProvider dataProvider)
        {
            MetadataWorkspace metadata = GetMetadataWorkspace(ref entityConnectionString);

            DbConnection connection = DbConnectionFactory.CreateTransient(dataProvider);

            return CreateEntityConnection(metadata, connection);
        }

        public static EntityConnection CreateTransient(string entityConnectionString)
        {
            return CreateTransient(entityConnectionString, null);
        }

        #endregion

        public static EntityConnection Create(string entityConnectionString, string effortConnectionString, bool persistent)
        {
            MetadataWorkspace metadata = GetMetadataWorkspace(ref entityConnectionString);

            EffortConnectionStringBuilder ecsb = new EffortConnectionStringBuilder(effortConnectionString);
            
            if (persistent)
            {
                ecsb.InstanceId = entityConnectionString;
            }
            else
            {
                ecsb.InstanceId = Guid.NewGuid().ToString();
            }

            EffortConnection connection = new EffortConnection() { ConnectionString = ecsb.ConnectionString };

            if (!persistent)
            {
                connection.MarkAsTransient();
            }

            return CreateEntityConnection(metadata, connection);
        }


        private static string GetFullEntityConnectionString(string entityConnectionString)
        {
            EntityConnectionStringBuilder builder = new EntityConnectionStringBuilder(entityConnectionString);

            if (!string.IsNullOrWhiteSpace(builder.Name))
            {
                string connectionStringName = builder.Name;

                ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (setting == null)
                {
                    throw new ArgumentException("Connectionstring was not found", "entityConnectionString");
                }

                entityConnectionString = setting.ConnectionString;
            }

            return entityConnectionString;
        }

        private static EntityConnection CreateEntityConnection(MetadataWorkspace metadata, DbConnection connection)
        {
            EntityConnection entityConnection = new EntityConnection(metadata, connection);

            FieldInfo owned = typeof(EntityConnection).GetField("_userOwnsStoreConnection", BindingFlags.Instance | BindingFlags.NonPublic);
            owned.SetValue(entityConnection, false);
            return entityConnection;
        }

        private static MetadataWorkspace GetMetadataWorkspace(ref string entityConnectionString)
        {
            entityConnectionString = GetFullEntityConnectionString(entityConnectionString);
            EntityConnectionStringBuilder ecsb = new EntityConnectionStringBuilder(entityConnectionString);

            return MetadataWorkspaceHelper.Rewrite(ecsb.Metadata, EffortProviderConfiguration.ProviderInvariantName, EffortProviderManifestTokens.Version1);
        }
    }
}
