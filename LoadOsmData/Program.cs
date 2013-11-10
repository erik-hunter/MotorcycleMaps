// OsmSharp - OpenStreetMap tools & library.
//
// Copyright (C) 2013 Simon Hughes
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
using System.IO;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Data.PBF.Raw.Processor;
using OsmSharp.Osm.Data.SQLServer.SimpleSchema.Processor;
using OsmSharp.Osm.Data.SQLServer.SimpleSchema.SchemaTools;
using OsmSharp.Osm.Data.XML.Processor;

namespace LoadOsmData
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                ShowParameters();
                return;
            }

            try
            {
                OsmSharp.Tools.Output.OutputStreamHost.RegisterOutputStream(new OsmSharp.Tools.Output.ConsoleOutputStream());
                string connectionString = @"Server=.;User Id=OsmSharp;Password=OsmSharp;Database=OsmData;Application Name=LoadOsmData";
                if (args[0] != ".")
                    connectionString = args[0];

                bool recreateSchema = args[1].ToLower() == "y";
                bool routingOnly = args[3].ToLower() == "y";

                var sqlConnection = new SqlConnection(connectionString);
                if (recreateSchema)
                {
                    sqlConnection.Open();
                    SQLServerSimpleSchemaTools.Remove(sqlConnection);
                    sqlConnection.Close();
                }

                // create the source from the osm file.
                DataProcessorSource source;
                if (args[2].EndsWith("pbf", StringComparison.InvariantCultureIgnoreCase))
                    source = new PBFDataProcessorSource(new FileInfo(args[2]).OpenRead());
                else
                    source = new XmlDataProcessorSource(args[2]);

                // create the SQLServer processor target.
                var target = new SQLServerSimpleSchemaDataProcessorTarget(connectionString, recreateSchema);
                target.RegisterSource(source); // register the source.

                Log("Importing data.");
                target.Pull(); // pull the data from source to target.
                Log("Completed importing data.");
            
                if(routingOnly)
                {
                    sqlConnection.Open();
                    SQLServerSimpleSchemaTools.RemoveNonRoutingData(sqlConnection);
                    sqlConnection.Close();
                }

            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.InnerException.Message);
                Console.WriteLine();
                Log("Aborting import.");
            }
        }

        private static void Log(string str)
        {
            Console.WriteLine("{0} {1}", DateTime.Now, str);
        }

        private static void ShowParameters()
        {
            //                                                                                                x 80 char wrap point
            Console.WriteLine("Parameters:");
            Console.WriteLine("[connection string] Using \".\" will default to");
            Console.WriteLine("  \"Server=.;User Id=OsmSharp;Password=OsmSharp;Database=OsmData;Application Name=LoadOsmData\"");
            Console.WriteLine("[re-create schema] Use Y to drop and re-create schema, anything else will");
            Console.WriteLine("                   leave the database alone.");
            Console.WriteLine("[OSM filename] Filename to import. i.e: \"C:\\british-isles-latest.osm\"");
            Console.WriteLine("                                        \"C:\\great-britain-latest.osm.pbf\"");
            Console.WriteLine("[routing only] Use Y to removes any non-routing data. This will keep the");
            Console.WriteLine("               database size to the bare minimum required for routing.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("LoadOsmData \"Server=.;User Id=OsmSharp;Password=OsmSharp;Database=OsmData;Application Name=LoadOsmData\" Y C:\\great-britain-latest.osm.pbf Y");
            Console.WriteLine("LoadOsmData . Y C:\\great-britain-latest.osm.pbf Y");
        }
    }
}
