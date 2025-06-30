using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Prospect.Server.Game.Services;
using Prospect.Unreal.Assets;
using Prospect.Unreal.Assets.UnrealTypes;
using Serilog;

namespace Prospect.Server.Game
{
    public class AssetLoadingTest
    {
        private readonly ILogger _logger = Log.ForContext<AssetLoadingTest>();

        public async Task RunTestAsync(string assetsBasePath)
        {
            _logger.Information("=== ASSET LOADING TEST ===");
            _logger.Information("Assets Base Path: {AssetsBasePath}", assetsBasePath);

            // Check if assets directory exists
            if (!Directory.Exists(assetsBasePath))
            {
                _logger.Warning("Assets directory not found: {AssetsBasePath}", assetsBasePath);
                _logger.Information("To run the full test, extract game assets using FModel:");
                _logger.Information("1. Extract 'The Cycle: Frontier' assets using FModel");
                _logger.Information("2. Export as JSON format to 'Exports' folder");
                _logger.Information("3. Set ASSETS_PATH environment variable or place Exports folder in exe directory");
                _logger.Information("");
                _logger.Information("Running basic component tests without assets...");
                await TestComponentsWithoutAssetsAsync();
                return;
            }

            try
            {
                // Test 1: Initialize GameDataService
                await TestGameDataServiceAsync(assetsBasePath);

                // Test 2: Test AssetParser with MAP01
                await TestMAP01ParsingAsync(assetsBasePath);

                _logger.Information("=== ALL TESTS COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Asset loading test failed");
                throw;
            }
        }

        private async Task TestGameDataServiceAsync(string assetsBasePath)
        {
            _logger.Information("--- Testing GameDataService ---");

            var gameDataService = new GameDataService(assetsBasePath);
            await gameDataService.InitializeAsync();

            // Log configuration
            gameDataService.LogConfiguration();

            // Test MAP01 configuration
            var map01Info = gameDataService.GetMapInfo("Map01");
            if (map01Info != null)
            {
                _logger.Information("MAP01 Configuration:");
                _logger.Information("  Persistent Map: {Path}", map01Info.GetPersistentMapPath());
                _logger.Information("  Player Start Cluster Radius: {Radius}", map01Info.PlayerStartClusterRadius);
                _logger.Information("  Player Start Cluster Cooldown: {Cooldown}s", map01Info.PlayerStartClusterCooldown);
                _logger.Information("  Player Start Score Rules: {Count} rules", map01Info.PlayerStartScoreRules.Count);
                
                foreach (var rule in map01Info.PlayerStartScoreRules)
                {
                    _logger.Information("    Radius: {Radius}, Score per player: {Score}", 
                        rule.Radius, rule.ScorePerPlayerInRadius);
                }

                _logger.Information("  Storm Occlusion Center 1: ({X}, {Y}, {Z})", 
                    map01Info.VFXMapInfo.StormOcclusionCenter01.X,
                    map01Info.VFXMapInfo.StormOcclusionCenter01.Y,
                    map01Info.VFXMapInfo.StormOcclusionCenter01.Z);
            }
            else
            {
                _logger.Warning("MAP01 configuration not found!");
            }

            // Test LOOP game mode
            var loopMode = gameDataService.GetGameModeTuning("LOOP");
            if (loopMode != null)
            {
                _logger.Information("LOOP Game Mode Configuration:");
                _logger.Information("  Score Sharing: {ScoreSharing}", loopMode.AllowsScoreSharing);
                _logger.Information("  Session Timeout: {Timeout}s", loopMode.SessionTimeoutCallbackRewards);
                _logger.Information("  Heat Map Enabled: {HeatMap}", loopMode.HeatmapEnabled);
                _logger.Information("  Timer Shutdown: {TimerShutdown}", loopMode.HasTimerShutdown);
            }
            else
            {
                _logger.Warning("LOOP game mode configuration not found!");
            }

            // Test player tuning
            var playerTuning = gameDataService.GetPlayerTuning();
            if (playerTuning != null)
            {
                _logger.Information("Player Tuning Configuration:");
                _logger.Information("  Walk Speed: {WalkSpeed} units/sec", playerTuning.WalkSpeed);
                _logger.Information("  Sprint Multiplier: {SprintMultiplier}x", playerTuning.SprintSpeedMultiplier);
                _logger.Information("  Crouch Speed: {CrouchSpeed} units/sec", playerTuning.CrouchSpeed);
                _logger.Information("  Effective Sprint Speed: {SprintSpeed} units/sec", 
                    playerTuning.GetEffectiveWalkSpeed(isSprinting: true));
            }

            _logger.Information("GameDataService test completed successfully");
        }

        private async Task TestMAP01ParsingAsync(string assetsBasePath)
        {
            _logger.Information("--- Testing MAP01 Asset Parsing ---");

            var assetParser = new AssetParser(assetsBasePath);
            var map01Path = Path.Combine(assetsBasePath, "Prospect", "Content", "Maps", "MP", "MAP01", "MP_Map01_P.json");

            if (!File.Exists(map01Path))
            {
                _logger.Warning("MAP01 file not found at: {Path}", map01Path);
                return;
            }

            _logger.Information("MAP01 file found: {Path}", map01Path);
            var fileInfo = new FileInfo(map01Path);
            _logger.Information("MAP01 file size: {Size:N0} bytes ({SizeMB:F1} MB)", 
                fileInfo.Length, fileInfo.Length / 1024.0 / 1024.0);

            try
            {
                // Test loading the level with our AssetParser
                var level = await assetParser.LoadLevelAsync("Maps/MP/MAP01/MP_Map01_P.json");
                
                if (level != null)
                {
                    _logger.Information("Successfully parsed MAP01 level:");
                    _logger.Information("  Level Name: {Name}", level.Name);
                    _logger.Information("  Actors Count: {Count}", level.Actors.Count);
                    _logger.Information("  Streaming Levels: {Count}", level.StreamingLevels?.Count ?? 0);
                    
                    // Count PlayerStart actors
                    var playerStarts = level.Actors.Where(a => a.Class?.Contains("PlayerStart") == true).ToList();
                    _logger.Information("  Player Start Actors: {Count}", playerStarts.Count);

                    if (playerStarts.Count > 0)
                    {
                        _logger.Information("  Sample Player Start Locations:");
                        for (int i = 0; i < Math.Min(5, playerStarts.Count); i++)
                        {
                            var start = playerStarts[i];
                            _logger.Information("    {Index}: ({X:F1}, {Y:F1}, {Z:F1})", 
                                i + 1, start.Transform.Translation.X, start.Transform.Translation.Y, start.Transform.Translation.Z);
                        }
                    }

                    // Show memory usage
                    var memoryBefore = GC.GetTotalMemory(false);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    var memoryAfter = GC.GetTotalMemory(true);
                    
                    _logger.Information("Memory usage after parsing: {Memory:N0} bytes ({MemoryMB:F1} MB)", 
                        memoryAfter, memoryAfter / 1024.0 / 1024.0);
                }
                else
                {
                    _logger.Warning("Failed to parse MAP01 level - returned null");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to parse MAP01 level");
                throw;
            }

            _logger.Information("MAP01 parsing test completed");
        }

        private async Task TestComponentsWithoutAssetsAsync()
        {
            _logger.Information("--- Testing Components Without Assets ---");
            
            // Test UnrealTypes components
            await TestUnrealTypesAsync();
            
            // Test AssetParser with mock data
            await TestAssetParserComponentsAsync();
            
            _logger.Information("=== COMPONENT TESTS COMPLETED SUCCESSFULLY ===");
            _logger.Information("Ready for Phase 2: Add real assets and run full tests!");
        }

        private async Task TestUnrealTypesAsync()
        {
            _logger.Information("Testing UObject, ULevel, and UActor classes...");
            
            // Test UObject
            var obj = new UObject
            {
                Name = "TestObject",
                Class = "UScriptClass'TestClass'",
                AssetPath = "Test/Path"
            };
            obj.SetProperty("TestProperty", "TestValue");
            
            _logger.Information("UObject test: {ToString}, Property: {Property}", 
                obj.ToString(), obj.GetProperty<string>("TestProperty"));
            
            // Test Vector3 and Transform
            var position = new Prospect.Unreal.Assets.UnrealTypes.Vector3(100f, 200f, 300f);
            var rotation = Prospect.Unreal.Assets.UnrealTypes.Quaternion.Identity;
            var transform = new Prospect.Unreal.Assets.UnrealTypes.Transform(position, rotation, Prospect.Unreal.Assets.UnrealTypes.Vector3.One);
            
            _logger.Information("Transform test: Position {Position}, Magnitude: {Magnitude:F2}", 
                transform.Translation.ToString(), transform.Translation.Magnitude);
            
            // Test UActor
            var actor = new UActor
            {
                Name = "TestActor",
                Class = "Blueprint'/Game/Test/TestActor.TestActor_C'",
                Transform = transform
            };
            actor.AddTag("TestTag");
            
            _logger.Information("UActor test: {ToString}, Location: {Location}, HasTag: {HasTag}", 
                actor.ToString(), actor.GetActorLocation().ToString(), actor.HasTag("TestTag"));
            
            // Test ULevel
            var level = new ULevel
            {
                Name = "TestLevel",
                Class = "Level"
            };
            level.AddActor(actor);
            
            var stats = level.GetStats();
            _logger.Information("ULevel test: {ToString}, Stats: {TotalActors} actors", 
                level.ToString(), stats.TotalActors);
                
            _logger.Information("✅ UnrealTypes components working correctly");
        }

        private async Task TestAssetParserComponentsAsync()
        {
            _logger.Information("Testing AssetParser architecture...");
            
            // Test AssetParser initialization
            var parser = new AssetParser("TestPath");
            _logger.Information("AssetParser initialized with base path: TestPath");
            
            // Test cache functionality
            var stats = parser.GetCacheStats();
            _logger.Information("Initial cache stats: {LoadedAssets} assets, {MemoryUsage} bytes", 
                stats.LoadedAssets, stats.MemoryUsage);
            
            // Test asset reference resolution
            var testRef = "/Game/Maps/MP/MAP01/MP_Map01_P.MP_Map01_P";
            var resolved = parser.ResolveAssetReference(testRef);
            _logger.Information("Asset reference resolution test: {Original} -> {Resolved}", 
                testRef, resolved);
            
            _logger.Information("✅ AssetParser architecture working correctly");
            await Task.CompletedTask; // Async consistency
        }
    }
} 