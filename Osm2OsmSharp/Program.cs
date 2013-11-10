using System;
using System.IO;
using OsmSharp.Osm;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Data.Core.Processor.Progress;
using OsmSharp.Osm.Data.PBF.Raw.Processor;
using OsmSharp.Osm.Data.XML.Processor;
using OsmSharp.Routing;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.DynamicGraph.PreProcessed;
using OsmSharp.Routing.Graph.Serialization.v1;
using OsmSharp.Routing.Osm.Data.Processing;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Tools.Collections;

namespace Osm2OsmSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                ShowParameters();
                return;
            }

            try
            {
                OsmSharp.Tools.Output.OutputStreamHost.RegisterOutputStream(new OsmSharp.Tools.Output.ConsoleOutputStream());

                // create the source from the osm file.
                DataProcessorSource source;
                if(args[0].EndsWith("pbf", StringComparison.InvariantCultureIgnoreCase))
                    source = new PBFDataProcessorSource(new FileInfo(args[0]).OpenRead());
                else
                    source = new XmlDataProcessorSource(args[0]);

                var interpreter = new OsmRoutingInterpreter();
                var tagsIndex = new OsmTagsIndex(new ObjectTable<OsmTagsIndex.OsmTags>(true));
                var data = new DynamicGraphRouterDataSource<PreProcessedEdge>(tagsIndex);
                var targetData = new PreProcessedDataGraphProcessingTarget(data, interpreter, data.TagsIndex, VehicleEnum.Car);
                var progressSource = new ProgressDataProcessorSource(source);
                targetData.RegisterSource(progressSource);
                targetData.Pull();

                Log("Saving data to: " + args[1]);
                var serializer = new V1RoutingSerializer();
                using (var stream = new FileStream(args[1], FileMode.Create, FileAccess.ReadWrite))
                {
                    serializer.Serialize(stream, data);
                    stream.Flush();
                    stream.Close();
                }

                Log("Finished");
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
            Console.WriteLine("[OSM input filename] Filename to import.");
            Console.WriteLine("    i.e: \"C:\\british-isles-latest.osm\"");
            Console.WriteLine("         \"C:\\great-britain-latest.osm.pbf\"");
            Console.WriteLine();
            Console.WriteLine("[OsmSharp output filename] Filename to write pre-calculated data to.");
            Console.WriteLine("    i.e: \"C:\\great-britain.oss\"");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("Osm2OsmSharp C:\\great-britain-latest.osm.pbf C:\\great-britain.oss");
        }
    }
}