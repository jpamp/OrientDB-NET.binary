﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orient.Client.Protocol.Serializers;

namespace Orient.Client.Protocol.Operations
{
    internal class DbOpen : IOperation
    {
        internal string DatabaseName { get; set; }
        internal ODatabaseType DatabaseType { get; set; }
        internal string UserName { get; set; }
        internal string UserPassword { get; set; }
        internal string ClusterConfig { get { return "null"; } }

        public Request Request(Request request)
        {
            // standard request fields
            request.AddDataItem((byte)OperationType.DB_OPEN);
            request.AddDataItem(request.SessionId);
            // operation specific fields
            if (OClient.ProtocolVersion >= 7)
            {
                request.AddDataItem(OClient.DriverName);
                request.AddDataItem(OClient.DriverVersion);
                request.AddDataItem(OClient.ProtocolVersion);
                request.AddDataItem(OClient.ClientID);
            }
            if (OClient.ProtocolVersion > 21)
            {
                request.AddDataItem(OClient.SerializationImpl);
            }

            request.AddDataItem(DatabaseName);
            if (OClient.ProtocolVersion >= 8)
            {
                request.AddDataItem(DatabaseType.ToString().ToLower());
            }
            request.AddDataItem(UserName);
            request.AddDataItem(UserPassword);

            return request;
        }

        public ODocument Response(Response response)
        {
            ODocument document = new ODocument();

            if (response == null)
            {
                return document;
            }

            var reader = response.Reader;

            // operation specific fields
            document.SetField("SessionId", reader.ReadInt32EndianAware());
            int clusterCount = -1;

            if (OClient.ProtocolVersion >= 7)
                clusterCount = (int)reader.ReadInt16EndianAware();
            else
                clusterCount = reader.ReadInt32EndianAware();

            document.SetField("ClusterCount", clusterCount);

            if (clusterCount > 0)
            {
                List<OCluster> clusters = new List<OCluster>();

                for (int i = 1; i <= clusterCount; i++)
                {
                    OCluster cluster = new OCluster();

                    int clusterNameLength = reader.ReadInt32EndianAware();

                    cluster.Name = System.Text.Encoding.Default.GetString(reader.ReadBytes(clusterNameLength));

                    cluster.Id = reader.ReadInt16EndianAware();

                    if (OClient.ProtocolVersion < 24)
                    {
                        int clusterTypeLength = reader.ReadInt32EndianAware();

                        string clusterType = System.Text.Encoding.Default.GetString(reader.ReadBytes(clusterTypeLength));
                        //cluster.Type = (OClusterType)Enum.Parse(typeof(OClusterType), clusterType, true);
                        if (OClient.ProtocolVersion >= 12)
                            cluster.DataSegmentID = reader.ReadInt16EndianAware();
                        else
                            cluster.DataSegmentID = 0;
                    }
                    clusters.Add(cluster);
                }

                document.SetField("Clusters", clusters);
            }

            int clusterConfigLength = reader.ReadInt32EndianAware();

            byte[] clusterConfig = null;

            if (clusterConfigLength > 0)
            {
                clusterConfig = reader.ReadBytes(clusterConfigLength);
            }

            document.SetField("ClusterConfig", clusterConfig);

            string release = reader.ReadInt32PrefixedString();
            document.SetField("OrientdbRelease", release);

            return document;
        }
    }
}
