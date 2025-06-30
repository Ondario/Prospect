using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Prospect.Unreal.Assets.DataTables
{
    public class DataTableParser
    {
        private readonly ILogger _logger = Log.ForContext<DataTableParser>();
        private readonly Dictionary<string, object> _loadedTables = new();
        private readonly string _dataTablesPath;

        public DataTableParser(string assetsBasePath)
        {
            _dataTablesPath = Path.Combine(assetsBasePath, "Prospect", "Content", "DataTables");
        }

        public async Task<T> LoadDataTableAsync<T>(string tableName) where T : class
        {
            var cacheKey = $"{tableName}_{typeof(T).Name}";
            
            if (_loadedTables.TryGetValue(cacheKey, out var cached))
            {
                return (T)cached;
            }

            var filePath = Path.Combine(_dataTablesPath, $"{tableName}.json");
            
            if (!File.Exists(filePath))
            {
                _logger.Warning("DataTable file not found: {FilePath}", filePath);
                return null;
            }

            try
            {
                _logger.Information("Loading DataTable: {TableName}", tableName);
                
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var dataTableArray = JsonConvert.DeserializeObject<JArray>(jsonContent);
                
                if (dataTableArray?.Count > 0)
                {
                    var dataTable = dataTableArray[0];
                    var rowsToken = dataTable["Rows"];
                    
                    if (rowsToken != null)
                    {
                        var result = rowsToken.ToObject<T>();
                        _loadedTables[cacheKey] = result;
                        
                        _logger.Information("Successfully loaded DataTable: {TableName}", tableName);
                        return result;
                    }
                }
                
                _logger.Warning("DataTable has no Rows: {TableName}", tableName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load DataTable: {TableName}", tableName);
                return null;
            }
        }

        public async Task<Dictionary<string, JObject>> LoadRawDataTableAsync(string tableName)
        {
            var filePath = Path.Combine(_dataTablesPath, $"{tableName}.json");
            
            if (!File.Exists(filePath))
            {
                _logger.Warning("DataTable file not found: {FilePath}", filePath);
                return new Dictionary<string, JObject>();
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var dataTableArray = JsonConvert.DeserializeObject<JArray>(jsonContent);
                
                if (dataTableArray?.Count > 0)
                {
                    var dataTable = dataTableArray[0];
                    var rowsToken = dataTable["Rows"];
                    
                    if (rowsToken is JObject rows)
                    {
                        var result = new Dictionary<string, JObject>();
                        foreach (var property in rows.Properties())
                        {
                            result[property.Name] = (JObject)property.Value;
                        }
                        return result;
                    }
                }
                
                return new Dictionary<string, JObject>();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load raw DataTable: {TableName}", tableName);
                return new Dictionary<string, JObject>();
            }
        }

        public void ClearCache()
        {
            _loadedTables.Clear();
            _logger.Information("DataTable cache cleared");
        }
    }
} 