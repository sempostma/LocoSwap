using Ionic.Zip;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LocoSwap
{
    public class RouteDetailed : Route
    {
        private int _recordsTotal;

        private int _tilesTotal;

        public ConcurrentDictionary<string, int> TilesTotalGrouped { get => _tilesTotalGrouped; set => _tilesTotalGrouped = value; }
        public int RecordsTotal { get => _recordsTotal; set => _recordsTotal = value; }
        public int TilesTotal { get => _tilesTotal; set => _tilesTotal = value; }
        public XDocument TracksDotBinDocument { get => _tracksDotBinDocument; set => _tracksDotBinDocument = value; }
        public ConcurrentDictionary<string, XDocument> Tiles { get => _tiles; set => _tiles = value; }

        private ConcurrentDictionary<string, int> _tilesTotalGrouped = new ConcurrentDictionary<string, int>();

        private ConcurrentDictionary<string, XDocument> _tiles = new ConcurrentDictionary<string, XDocument>();

        NetworkInfo networkInfo;
        XDocument _tracksDotBinDocument;

        public RouteDetailed(string routeId) : base(routeId)
        {
            
        }

        private string[] GetTileNames()
        {
            string trackTilesDir = GetFilePath("Networks\\Track Tiles");
            var tiles = Directory.GetFiles(trackTilesDir);
            return tiles;
        }

        private void ProcessTracksDotBin()
        {
            networkInfo = new NetworkInfo();
            networkInfo.NetworkDevString = TracksDotBinDocument.XPathSelectElement("/cRecordSet/Record/Network-cTrackNetwork/NetworkID/cGUID/DevString").Value;
            networkInfo.RibbonContainer = TracksDotBinDocument.XPathSelectElement("/cRecordSet/Record/Network-cTrackNetwork/RibbonContainer/Network-cRibbonContainerUnstreamed");
            networkInfo.TrackRibbons = new List<TrackRibbon>();

            foreach (var ribbon in networkInfo.RibbonContainer.XPathSelectElements("Ribbon/*")) 
            {
                if (ribbon.Name == "NetworkRibbon-cTrackRibbon" || ribbon.Name == "Network-cTrackRibbon")
                {
                    var trackRibbon = new TrackRibbon();
                    networkInfo.TrackRibbons.Add(trackRibbon);
                    trackRibbon.RibbonId = ribbon.XPathSelectElement("RibbonID/cGUID/DevString").Value;
                    trackRibbon.Length = Double.Parse(ribbon.XPathSelectElement("_length").Value);
                    trackRibbon.HeightPoints = new List<HeightPoint>();

                    var heightNodes = ribbon.XPathSelectElements("Height/*");

                    foreach (var heightNode in heightNodes)
                    {
                        if (heightNode.Name == "Network-iRibbon-cHeight")
                        {
                            var point = new HeightPoint();
                            point.Height = Double.Parse(heightNode.XPathSelectElement("_height").Value);
                            point.Position = Double.Parse(heightNode.XPathSelectElement("_position").Value);
                            point.Manual = heightNode.XPathSelectElement("_manual").Value == "1" ? true : false;
                            trackRibbon.HeightPoints.Add(point);
                        } else
                        {
                            Log.Debug("Eexpected heightnode but got {0} instead", heightNode.Name);
                        }
                    }

                    trackRibbon.LockCounterWhenModified = ribbon.XPathSelectElement("LockCounterWhenModified").Value == "1" ? true : false;
                    trackRibbon.SuperElevated = ribbon.XPathSelectElement("Superelevated").Value == "1" ? true : false;
                } else
                {
                    Log.Debug("Expected NetworkRibbon-cTrackRibbon but got {0} instead", ribbon.Name);
                }
            }

            var trackNodes = networkInfo.RibbonContainer.XPathSelectElements("Node/*");
            foreach (var trackNode in trackNodes)
            {
                if (trackNode.Name == "Network-cTrackNode")
                {
                    var rConnectionNodes = trackNode.XPathSelectElements("Network-cNetworkNode-sRConnection/*");
                    foreach (var rconnectionNode in rConnectionNodes)
                    {
                        var devString = rconnectionNode.XPathSelectElement("_id/cGUID/DevString").Value;
                    }

                    var fixPat = trackNode.XPathSelectElement("FixedPatternRef/Network-cNetworkNode-sFixedPatternRef/FixedPatternNodeIndex");

                    // routevector

                    var patternRefNodes = trackNode.XPathSelectElements("PatternRef/Network-cNetworkNode-sPatternRef/*");
                    foreach (var trackPatternNode in patternRefNodes)
                    {
                        string nodeName = trackPatternNode.Name.ToString();
                        int id = int.Parse(trackPatternNode.Attribute("d:id").Value);
                        int nodeIndex;
                        TrackPattern tp = null;

                        string left = nodeName.Substring(0, 9);
                        string right = nodeName.Substring(nodeName.Length - 7);

                        if (nodeName == "PatternNodeIndex")
                        {
                            nodeIndex = int.Parse(trackPatternNode.XPathSelectElement("PatternNodeIndex").Value);
                        }
                        else if (nodeName == "d:nil")
                        {
                            tp = new TrackPattern();
                            tp.RefId = id;
                        }
                        else if (left == "Network-c" && right == "Pattern")
                        {
                            bool manual = Boolean.Parse(trackPatternNode.XPathSelectElement("Manual").Value);
                            string state = trackPatternNode.XPathSelectElement("State").Value;
                            double transitionTime = Double.Parse(trackPatternNode.XPathSelectElement("TransitionTime").Value);
                            string pState = trackPatternNode.XPathSelectElement("PreviousState").Value;

                            if (nodeName == "Network-cTurnoutPattern")
                            {
                                tp = new TurnoutPattern();
                            }
                            else if (nodeName == "Network-c3WayPattern")
                            {
                                tp = new ThreeWayPattern();
                            }
                            else if (nodeName == "Network-cSingleSlipPattern")
                            {
                                tp = new SingleSlipPattern();
                            }
                            else if (nodeName == "Network-cDoubleSlipPattern")
                            {
                                tp = new DoubleSlipPattern();
                            }
                            else if (nodeName == "Network-cCrossingPattern")
                            {
                                tp = new CrossingPattern();
                            }
                            else
                            {
                                Log.Debug("Expecting tack pattern but got {0} instead", nodeName);
                            }
                            if (tp != null)
                            {
                                tp.RefId = id;
                            }
                        } else
                        {
                            Log.Debug("Unexpected track pattern element {0}", nodeName);
                        }
                    }
                } else
                {
                    Log.Debug("Expected Network-cTrackNode but got {0} instead", trackNode.Name);
                }
            }
        }

        public async Task LoadTiles(IProgress<int> progress = null)
        {
            var ScanCancellationTokenSource = new CancellationTokenSource();
            var token = ScanCancellationTokenSource.Token;

            progress?.Report(10);
            var startDateTime = DateTime.Now;

            var tracksBinDocumentTask = Task.Run(() =>
            {
                string _trackTilesDir = GetFilePath("Networks\\Tracks.bin");
                TracksDotBinDocument = TsSerializer.Load(_trackTilesDir);
                ProcessTracksDotBin();
            }, token);

            var binTask = Task.Run(() =>
            {
                progress.Report(0);
                string[] tileNames = GetTileNames();
                TilesTotal = tileNames.Length;
                RecordsTotal = 0;

                Parallel.ForEach(tileNames.Select((value, i) => (value, i)), (item) =>
                {
                    var tilePath = item.value;
                    var tileName = Path.GetFileName(tilePath);
                    var index = item.i;

                    int localRecordsTotal = 0;
                    Dictionary<string, int> localTilesTotalGrouped = new Dictionary<string, int>();

                    try
                    {
                        Log.Debug("Try: {0}", tilePath);
                        var trackTile = TsSerializer.Load(tilePath);
                        var records = trackTile.XPathSelectElements("/cRecordSet/Record/*");
                        localRecordsTotal += records.Count();
                        foreach (var record in records)
                        {
                            var name = record.Name.ToString();
                            if (!localTilesTotalGrouped.ContainsKey(name)) localTilesTotalGrouped[name] = 0;
                            localTilesTotalGrouped[name]++;
                        }
                        Log.Debug("Found # Records: {0}", records.Count());
                        Tiles.AddOrUpdate(tileName, trackTile, (_k, v) => v);
                    }
                    catch (Exception e)
                    {
                        Log.Debug("{0}: {1}", e.Message, tilePath);
                    }

                    Interlocked.Add(ref _recordsTotal, localRecordsTotal);

                    foreach (KeyValuePair<string, int> entry in localTilesTotalGrouped)
                    {
                        _tilesTotalGrouped.AddOrUpdate(entry.Key, entry.Value, (k, v) => Interlocked.Add(ref v, entry.Value));
                    }

                    progress.Report((int)Math.Ceiling((float)index / tileNames.Count() * 100));
                    token.ThrowIfCancellationRequested();
                });
            }, token);

            try
            {
                await Task.WhenAll(binTask, tracksBinDocumentTask);
                var endDateTime = DateTime.Now;
                Log.Debug(".bin scan took {0} seconds", (endDateTime - startDateTime).TotalSeconds);
            } catch(Exception e)
            {

            }
            
            progress?.Report(100);
        }
    }
}
