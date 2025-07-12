using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NeverSerender.Changes;
using NeverSerender.Output;
using NeverSerender.Snapshot;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.Entity;
using VRage.Utils;

namespace NeverSerender
{
    public class Capture
    {
        private readonly Exporter exporter;

        private readonly BlockingCollection<ChangeSnapshot> exportQueue = new BlockingCollection<ChangeSnapshot>();
        private readonly Stream logFile;
        private readonly MiniLog mainLog;
        private readonly Stream outFile;
        private readonly ExporterSettings settings;
        private readonly MiniLog threadLog;

        private readonly EntityChangeTracker trackedEntities;
        private readonly EntityChangeTracker trackedEntitiesDelayed;
        private readonly Dictionary<long, GridChangeTracker> trackedGrids = new Dictionary<long, GridChangeTracker>();
        private readonly SpaceModelWriter writer;

        public Capture(ExporterSettings settings)
        {
            this.settings = settings;

            MyLog.Default.WriteLineAndConsole("NeverSerender Capture starting");

            var logDir = Path.GetDirectoryName(settings.LogPath);
            if (logDir != null)
                Directory.CreateDirectory(logDir);

            logFile = File.OpenWrite(settings.LogPath);
            logFile.SetLength(0);

            var rootLog = new MiniLog(new StreamWriter(logFile), settings.AutoFlush);
            mainLog = rootLog.Named("Capture");
            threadLog = mainLog.Named("Thread");

            var outDir = Path.GetDirectoryName(settings.OutPath);
            if (outDir != null)
                Directory.CreateDirectory(outDir);

            outFile = new BufferedStream(File.OpenWrite(settings.OutPath));
            outFile.SetLength(0);

            writer = new SpaceModelWriter(outFile, mainLog.Named("SpaceWriter"));
            writer.Header(MySession.Static.Name, MySession.Static.LocalCharacter.Name, settings.ViewMatrix);

            exporter = new Exporter(mainLog.Named("Exporter"), writer);

            trackedEntities = new EntityChangeTracker(mainLog.Named("Entities"));
            trackedEntitiesDelayed = new EntityChangeTracker(mainLog.Named("EntitiesDelayed"));

            new Thread(ExportThread).Start();
        }

        private void ExportThread()
        {
            try
            {
                threadLog.WriteLine("Export thread started");

                while (!exportQueue.IsCompleted)
                {
                    ChangeSnapshot snapshot;
                    try
                    {
                        snapshot = exportQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        threadLog.WriteLine("Export thread stopped");
                        break;
                    }

                    try
                    {
                        ProcessSnapshot(snapshot);
                    }
                    catch (Exception ex)
                    {
                        threadLog.WriteLine($"Error processing snapshot: {ex}");
                    }
                }

                threadLog.WriteLine("Export thread finished");

                writer.End();
            }
            finally
            {
                exportQueue.CompleteAdding();
                exportQueue.Dispose();

                threadLog.WriteLine("Unloading");
                exporter.Dispose();

                threadLog.WriteLine("Export thread stopped");
                threadLog.Flush();

                logFile.Dispose();
                outFile.Flush();
                outFile.Dispose();
            }
        }

        private void ProcessSnapshot(ChangeSnapshot snapshot)
        {
            threadLog.WriteLine($"Processing snapshot Advance={snapshot.Advance}");

            if (snapshot.Advance.HasValue)
                writer.Advance(snapshot.Advance.Value);

            threadLog.WriteLine($"Modify {snapshot.Entities.Count} entities");
            foreach (var entity in snapshot.Entities)
                exporter.ExportEntity(entity);

            threadLog.WriteLine($"Modify {snapshot.Grids.Count} grids");
            foreach (var grid in snapshot.Grids)
                exporter.ExportGrid(grid);

            threadLog.WriteLine($"Modify {snapshot.EntitiesDelayed.Count} entities (delayed)");
            foreach (var entity in snapshot.EntitiesDelayed)
                exporter.ExportEntity(entity);

            threadLog.WriteLine($"Modify {snapshot.Lights.Count} lights");
            foreach (var light in snapshot.Lights)
                exporter.ExportLight(light);
        }

        public void Step(float? advance)
        {
            mainLog.WriteLine("Step\n");
            mainLog.Flush();

            var grids = new List<GridSnapshot>();

            // var character = MySession.Static.LocalCharacter;
            // var transforms = character.BoneRelativeTransforms;
            // var bones = character.Model.Bones;
            // var mapping = character.Model.BoneMapping;
            //
            // foreach (var bone in bones)
            //     mainLog.WriteLine($"Bone #{bone.Index} {bone.Name} Parent: {bone.Parent}");
            //
            // mainLog.WriteLine($"transforms {transforms?.Length} bones {bones?.Length} mapping {mapping?.Length}");

            if (settings.ExportEntityIds == null)
                foreach (var entity in MyEntities.GetEntities())
                    TrackEntity(entity, grids);
            else
                foreach (var entityId in settings.ExportEntityIds)
                {
                    var entity = MyEntities.GetEntityById(entityId);
                    if (entity != null)
                        TrackEntity(entity, grids);
                }

            var lights = exporter.GetLights();

            mainLog.WriteLine("Done");

            var changes = new ChangeSnapshot
            {
                Advance = advance,
                Entities = trackedEntities.Next(),
                EntitiesDelayed = trackedEntitiesDelayed.Next(),
                Grids = grids,
                Lights = lights
            };
            exportQueue.Add(changes);
        }

        private void TrackEntity(MyEntity entity, List<GridSnapshot> grids)
        {
            // mainLog.WriteLine(
            //     $"Entity Name={entity.Name} DisplayName={entity.DisplayName} IsPreview={entity.IsPreview}");

            if (!settings.ExportNonGrids && !(entity is MyCubeGrid))
                return;

            trackedEntities.Add(entity);

            if (!(entity is MyCubeGrid grid)) return;

            // mainLog.WriteLine(
            //     $"Grid EntityId={grid.EntityId} DisplayName={grid.DisplayName} BlocksCount={grid.BlocksCount}");

            if (!trackedGrids.TryGetValue(grid.EntityId, out var tracker))
            {
                tracker = new GridChangeTracker(trackedEntitiesDelayed, grid);
                trackedGrids.Add(grid.EntityId, tracker);
            }

            var gridChanges = tracker.Next();
            if (gridChanges.Blocks.Count > 0 || gridChanges.RemovedBlocks.Count > 0)
                grids.Add(gridChanges);
        }

        public void Finish()
        {
            exportQueue.CompleteAdding();
            trackedEntities.Dispose();
            trackedEntitiesDelayed.Dispose();
            trackedGrids.ForEach(g => g.Value.Dispose());
        }
    }
}