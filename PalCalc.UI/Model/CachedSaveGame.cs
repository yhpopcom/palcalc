﻿using Newtonsoft.Json;
using PalCalc.Model;
using PalCalc.SaveReader;
using PalCalc.SaveReader.SaveFile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.Model
{
    public class CachedSaveGame
    {
        private static readonly string SaveReaderVersion = "v18";

        public CachedSaveGame(ISaveGame underlyingSave)
        {
            UnderlyingSave = underlyingSave;
            ReaderVersion = SaveReaderVersion;
        }

        public DateTime LastModified { get; set; }

        public bool IsServerSave { get; set; }

        public string WorldName { get; set; }
        public string PlayerName { get; set; }
        public int? PlayerLevel { get; set; }
        public int InGameDay { get; set; }

        public string DatabaseVersion { get; set; }
        public string ReaderVersion { get; set; }

        public List<PlayerInstance> Players { get; set; }
        public List<GuildInstance> Guilds { get; set; }
        public List<PalInstance> OwnedPals { get; set; }

        // note: `OwnedPals` is the primary source of pal info, `Bases` and `PalContainers` are
        //       just used for supplemental info like which bases belong to which guild, which
        //       viewing cages belong to bases, world coordinates of bases, etc.

        public List<BaseInstance> Bases { get; set; }
        public List<IPalContainer> PalContainers { get; set; }

        private Dictionary<string, PlayerInstance> playersByName;
        [JsonIgnore]
        public Dictionary<string, PlayerInstance> PlayersById =>
            playersByName ??= Players.ToDictionary(p => p.PlayerId);

        private Dictionary<string, GuildInstance> playerGuilds;
        [JsonIgnore]
        public Dictionary<string, GuildInstance> GuildsByPlayerId =>
            playerGuilds ??= Players.ToDictionary(p => p.PlayerId, p => Guilds.FirstOrDefault(g => g.MemberIds.Contains(p.PlayerId)));

        private Dictionary<string, List<BaseInstance>> basesByGuild;
        [JsonIgnore]
        public Dictionary<string, List<BaseInstance>> BasesByGuildId =>
            basesByGuild ??= Guilds.Select(g => g.Id).Concat(Bases.Select(b => b.OwnerGuildId)).Distinct().ToDictionary(g => g, g => Bases.Where(b => b.OwnerGuildId == g).ToList());

        private Dictionary<string, GuildInstance> containerGuilds;
        [JsonIgnore]
        public Dictionary<string, GuildInstance> GuildsByContainerId =>
            containerGuilds ??= PalContainers?.ToDictionary(
                c => c.Id,
                c => c switch
                {
                    PalboxPalContainer pbc => GuildsByPlayerId.GetValueOrDefault(pbc.PlayerId),
                    PlayerPartyContainer ppc => GuildsByPlayerId.GetValueOrDefault(ppc.PlayerId),
                    BasePalContainer bpc => Guilds.FirstOrDefault(g => g.Id == Bases.FirstOrDefault(b => b.Id == bpc.BaseId)?.OwnerGuildId),
                    ViewingCageContainer vcc => Guilds.FirstOrDefault(g => g.Id == Bases.FirstOrDefault(b => b.Id == vcc.BaseId)?.OwnerGuildId),
                    _ => null
                }
            );

        // NOTE: Any new fields should be added here
        public void CopyFrom(CachedSaveGame src)
        {
            if (UnderlyingSave != src.UnderlyingSave) throw new InvalidOperationException();

            LastModified = src.LastModified;
            IsServerSave = src.IsServerSave;
            WorldName = src.WorldName;
            PlayerName = src.PlayerName;
            PlayerLevel = src.PlayerLevel;
            InGameDay = src.InGameDay;
            DatabaseVersion = src.DatabaseVersion;
            ReaderVersion = src.ReaderVersion;
            PalContainers = [.. src.PalContainers];
            Players = [.. src.Players];
            Guilds = [.. src.Guilds];
            OwnedPals = [.. src.OwnedPals];
            Bases = [.. src.Bases];

            playersByName = null;
            playerGuilds = null;
            basesByGuild = null;
        }

        [JsonIgnore]
        public ISaveGame UnderlyingSave { get; set; }

        public bool IsValid => UnderlyingSave.IsValid;

        public bool IsOutdated(PalDB currentDb) => LastModified != UnderlyingSave.LastModified || DatabaseVersion != currentDb.Version || ReaderVersion != SaveReaderVersion;

        public string StateId => $"{IdentifierFor(UnderlyingSave)}-{LastModified.Ticks}";

        public static event Action<ISaveGame> SaveFileLoadStart;
        public static event Action<ISaveGame, CachedSaveGame> SaveFileLoadEnd;
        public static event Action<ISaveGame, Exception> SaveFileLoadError;

        public static string IdentifierFor(ISaveGame game)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();

            foreach (var c in $"{game.UserId}-{game.GameId}")
                if (!invalidChars.Contains(c)) sb.Append(c);

            return sb.ToString();
        }

        private static CachedSaveGame sampleForDesignerView;
        public static CachedSaveGame SampleForDesignerView =>
            sampleForDesignerView ??=
                DirectSavesLocation.AllLocal
                    .SelectMany(l => l.ValidSaveGames)
                    .OrderByDescending(g => g.LastModified)
                    .Select(g => FromSaveGame(g, PalDB.LoadEmbedded()))
                    .First();


        public static CachedSaveGame FromSaveGame(ISaveGame game, PalDB db)
        {
            var isDesignMode = DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (!isDesignMode) SaveFileLoadStart?.Invoke(game);

            CachedSaveGame result;
#if HANDLE_ERRORS
            try
            {
#endif
                GameMeta meta = null;
                // `LevelMeta` is sometimes unavailable for Xbox saves, which shouldn't prevent us from
                // being able to load the data
                try { meta = game.LevelMeta.ReadGameOptions(); } catch { }
                
                var charData = game.Level.ReadCharacterData(db, game.Players);
                result = new CachedSaveGame(game)
                {
                    DatabaseVersion = db.Version,
                    LastModified = game.LastModified,
                    OwnedPals = charData.Pals,
                    Guilds = charData.Guilds,
                    Players = charData.Players,
                    Bases = charData.Bases,
                    PalContainers = charData.PalContainers,
                    PlayerLevel = meta?.PlayerLevel,
                    PlayerName = meta?.PlayerName ?? "UNKNOWN",
                    WorldName = meta?.WorldName ?? "UNKNOWN WORLD",
                    InGameDay = meta?.InGameDay ?? 0,
                };
#if HANDLE_ERRORS
            }
            catch (Exception ex)
            {
                SaveFileLoadError?.Invoke(game, ex);
                return null;
            }
#endif

            if (!isDesignMode) SaveFileLoadEnd?.Invoke(game, result);

            return result;
        }

        private static JsonConverter<IPalContainer> palContainerConverter = new PalContainerJsonConverter();
        public string ToJson(PalDB db) => JsonConvert.SerializeObject(this, new PalInstanceJsonConverter(db), palContainerConverter);

        public static CachedSaveGame FromJson(string json, PalDB db) => JsonConvert.DeserializeObject<CachedSaveGame>(json, new PalInstanceJsonConverter(db), palContainerConverter);
    }
}
