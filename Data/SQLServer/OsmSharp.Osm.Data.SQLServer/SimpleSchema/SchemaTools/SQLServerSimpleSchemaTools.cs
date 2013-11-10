﻿// OsmSharp - OpenStreetMap tools & library.
//
// Copyright (C) 2013 Abelshausen Ben
//                    Alexander Sinitsyn
//                    Simon Hughes
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.IO;
using OsmSharp.Tools.Output;

namespace OsmSharp.Osm.Data.SQLServer.SimpleSchema.SchemaTools
{
    /// <summary>
    /// Tools for creation/detection of the simple schema in PostgreSQL.
    /// </summary>
    public static class SQLServerSimpleSchemaTools
    {
        /// <summary>
        /// Creates/detects the simple schema. Does not create the indexes and constraints. That is done after the data is loaded
        /// </summary>
        /// <param name="connection"></param>
        public static void CreateAndDetect(SqlConnection connection)
        {
            //check if Simple Schema table exists
            const string sql = "select object_id('dbo.node', 'U')";
            object res;
            using (var cmd = new SqlCommand(sql, connection))
            {
                res = cmd.ExecuteScalar();
            }
            //if table exists, we are OK
            if (!DBNull.Value.Equals(res))
                return;

            OutputStreamHost.WriteLine("Creating database schema");
            ExecuteSQL(connection, "SimpleSchemaDDL.sql");
        }

        /// <summary>
        /// Removes the simple schema.
        /// </summary>
        public static void Remove(SqlConnection connection)
        {
            OutputStreamHost.WriteLine("Removing database schema");
            ExecuteSQL(connection, "SimpleSchemaDROP.sql");
        }

        /// <summary>
        /// Removes non-routing data from the database. For the UK, this resulted in 34.4 million records being deleted.
        /// </summary>
        public static void RemoveNonRoutingData(SqlConnection connection)
        {
            OutputStreamHost.WriteLine("Removing non-routing data");
            ExecuteSQL(connection, "SimpleSchemaDeleteNonRouting.sql");
        }

        /// <summary>
        /// Add indexes, removed duplicates and adds constraints
        /// </summary>
        /// <param name="connection"></param>
        public static void AddConstraints(SqlConnection connection)
        {
            OutputStreamHost.WriteLine("Adding database constraints");
            ExecuteSQL(connection, "SimpleSchemaConstraints.sql");
        }

        private static void ExecuteSQL(SqlConnection connection, string resourceFilename)
        {
            foreach (string resource in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(resource => resource.EndsWith(resourceFilename)))
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            using (var cmd = new SqlCommand("", connection))
                            {
                                cmd.CommandTimeout = 1800;  // 30 minutes. Adding constraints can be time consuming
                                cmd.CommandText = reader.ReadToEnd();
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                break;
            }
        }
    }
}