﻿using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using AAEmu.Commons.IO;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Json;
using AAEmu.Game.Utils.Converters;
using Newtonsoft.Json;
using AAEmu.Game.Utils.Scripts.SubCommands;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Scripts.SubCommands.Doodads;

public class DoodadSaveSubCommand : SubCommandBase
{
    public DoodadSaveSubCommand()
    {
        Title = "[Doodad Save]";
        Description = "Save current state of a doodad to the doodads world file.";
        CallPrefix = $"{CommandManager.CommandPrefix}doodad save";
        AddParameter(new NumericSubCommandParameter<uint>("ObjId", "Object Id", true));
    }

    public override void Execute(ICharacter character, string triggerArgument,
        IDictionary<string, ParameterValue> parameters, IMessageOutput messageOutput)
    {
        uint doodadObjId = parameters["ObjId"];
        var doodad = WorldManager.Instance.GetDoodad(doodadObjId);
        if (doodad is null)
        {
            SendColorMessage(messageOutput, Color.Red, $"Doodad with objId {doodadObjId} does not exist");
            return;
        }

        var world = WorldManager.Instance.GetWorld(doodad.Transform.WorldId);
        if (world is null)
        {
            SendColorMessage(messageOutput, Color.Red, $"Could not find the worldId {doodad.Transform.WorldId}");
            return;
        }

        // Load Doodad spawns
        Logger.Info("Loading spawns...");
        var worldPath = Path.Combine(FileManager.AppPath, "Data", "Worlds", world.Name);
        var jsonFileName = Path.Combine(worldPath, "doodad_spawns_new.json");

        if (!File.Exists(jsonFileName))
        {
            SendColorMessage(messageOutput, Color.Red, $"World file {jsonFileName} is missing for world {world.Name}");
            Logger.Info($"World file {jsonFileName} is missing for world {world.Name}");
            return;
        }

        var contents = FileManager.GetFileContents(jsonFileName);
        if (string.IsNullOrWhiteSpace(contents))
        {
            Logger.Warn($"World file {jsonFileName} is empty, using empty spawners list");
            contents = "[]";
        }

        if (!JsonHelper.TryDeserializeObject<List<JsonDoodadSpawns>>(contents, out var fileSpawnersList, out _))
        {
            SendColorMessage(messageOutput, Color.Red, $"Incorrect Json format for file {jsonFileName}");
            return;
        }

        var fileSpawners = fileSpawnersList.ToDictionary(s => s.Id, s => s);

        var spawn = new JsonDoodadSpawns
        {
            Id = doodad.Id,
            UnitId = doodad.TemplateId,
            Position = new JsonPosition
            {
                X = doodad.Transform.Local.Position.X,
                Y = doodad.Transform.Local.Position.Y,
                Z = doodad.Transform.Local.Position.Z,
                Roll = doodad.Transform.Local.Rotation.X.RadToDeg(),
                Pitch = doodad.Transform.Local.Rotation.Y.RadToDeg(),
                Yaw = doodad.Transform.Local.Rotation.Z.RadToDeg()
            }
        };

        if (!fileSpawners.TryAdd(spawn.Id, spawn))
        {
            fileSpawners[spawn.Id] = spawn;
        }

        var serialized = JsonConvert.SerializeObject(fileSpawners.Values.ToArray(), Formatting.Indented,
            new JsonModelsConverter());
        FileManager.SaveFile(serialized, string.Format(jsonFileName, FileManager.AppPath));
        SendMessage(messageOutput, $"Doodad ObjId: {doodad.ObjId} has been saved!");
    }
}
