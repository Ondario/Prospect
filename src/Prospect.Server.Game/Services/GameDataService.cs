using System;
using System.Linq;
using System.Threading.Tasks;
using Prospect.Unreal.Assets.DataTables;
using Serilog;

namespace Prospect.Server.Game.Services
{
    public class GameDataService
    {
        private readonly ILogger _logger = Log.ForContext<GameDataService>();
        private readonly DataTableParser _dataTableParser;
        
        private MapInfoCollection _mapInfos;
        private GameModeTuningCollection _gameModeTuning;
        private PlayerTuningCollection _playerTuning;

        public GameDataService(string assetsBasePath)
        {
            _dataTableParser = new DataTableParser(assetsBasePath);
        }

        public async Task InitializeAsync()
        {
            _logger.Information("Initializing GameDataService...");
            
            try
            {
                // Load all critical data tables
                await LoadAllDataTablesAsync();
                
                _logger.Information("GameDataService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize GameDataService");
                throw;
            }
        }

        private async Task LoadAllDataTablesAsync()
        {
            // Load map information
            _mapInfos = await _dataTableParser.LoadDataTableAsync<MapInfoCollection>("MapsInfos_DT");
            if (_mapInfos == null)
            {
                _logger.Warning("Failed to load MapsInfos_DT, using empty collection");
                _mapInfos = new MapInfoCollection();
            }
            else
            {
                _logger.Information("Loaded {Count} map configurations", _mapInfos.Count);
            }

            // Load game mode tuning
            _gameModeTuning = await _dataTableParser.LoadDataTableAsync<GameModeTuningCollection>("GameModeTuning_DT");
            if (_gameModeTuning == null)
            {
                _logger.Warning("Failed to load GameModeTuning_DT, using empty collection");
                _gameModeTuning = new GameModeTuningCollection();
            }
            else
            {
                _logger.Information("Loaded {Count} game mode configurations", _gameModeTuning.Count);
            }

            // Load player tuning
            _playerTuning = await _dataTableParser.LoadDataTableAsync<PlayerTuningCollection>("PlayerTuning_DT");
            if (_playerTuning == null)
            {
                _logger.Warning("Failed to load PlayerTuning_DT, using empty collection");
                _playerTuning = new PlayerTuningCollection();
            }
            else
            {
                _logger.Information("Loaded {Count} player tuning configurations", _playerTuning.Count);
            }
        }

        public MapInfo GetMapInfo(string mapId)
        {
            if (_mapInfos?.TryGetValue(mapId, out var mapInfo) == true)
            {
                return mapInfo;
            }

            _logger.Warning("MapInfo not found for map: {MapId}", mapId);
            return null;
        }

        public GameModeTuning GetGameModeTuning(string gameMode)
        {
            if (_gameModeTuning?.TryGetValue(gameMode, out var tuning) == true)
            {
                return tuning;
            }

            _logger.Warning("GameModeTuning not found for mode: {GameMode}", gameMode);
            return null;
        }

        public PlayerTuning GetPlayerTuning(string tuningId = "Default")
        {
            if (_playerTuning?.TryGetValue(tuningId, out var tuning) == true)
            {
                return tuning;
            }

            // Return default player tuning if specific not found
            if (_playerTuning?.Count > 0)
            {
                var defaultTuning = _playerTuning.Values.First();
                _logger.Information("Using default player tuning for: {TuningId}", tuningId);
                return defaultTuning;
            }

            _logger.Warning("No PlayerTuning found, creating default");
            return new PlayerTuning();
        }

        public bool IsMapSupported(string mapId)
        {
            return _mapInfos?.ContainsKey(mapId) == true;
        }

        public bool IsGameModeSupported(string gameMode)
        {
            return _gameModeTuning?.ContainsKey(gameMode) == true;
        }

        public string[] GetSupportedMaps()
        {
            return _mapInfos?.Keys.ToArray() ?? Array.Empty<string>();
        }

        public string[] GetSupportedGameModes()
        {
            return _gameModeTuning?.Keys.ToArray() ?? Array.Empty<string>();
        }

        public void LogConfiguration()
        {
            _logger.Information("=== GAME CONFIGURATION ===");
            
            if (_mapInfos?.Count > 0)
            {
                _logger.Information("Available Maps:");
                foreach (var kvp in _mapInfos)
                {
                    var map = kvp.Value;
                    _logger.Information("  {MapId}: {Name} ({Path})", 
                        kvp.Key, 
                        map.Name?.GetDisplayText() ?? "Unknown", 
                        map.GetPersistentMapPath());
                }
            }

            if (_gameModeTuning?.Count > 0)
            {
                _logger.Information("Available Game Modes:");
                foreach (var kvp in _gameModeTuning)
                {
                    var mode = kvp.Value;
                    _logger.Information("  {GameMode}: Score Sharing={ScoreSharing}, Timer Shutdown={TimerShutdown}", 
                        kvp.Key, 
                        mode.AllowsScoreSharing,
                        mode.HasTimerShutdown);
                }
            }

            _logger.Information("===========================");
        }

        public async Task ReloadDataTablesAsync()
        {
            _logger.Information("Reloading all data tables...");
            _dataTableParser.ClearCache();
            await LoadAllDataTablesAsync();
            _logger.Information("Data tables reloaded successfully");
        }
    }
} 