using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 高度な地形生成アルゴリズム
    /// 水力浸食、熱浸食、風化システムを実装
    /// </summary>
    public static class AdvancedTerrainAlgorithms
    {
        #region 水力浸食システム

        [System.Serializable]
        public struct HydraulicErosionParams
        {
            [Header("基本設定")]
            public int iterations;
            public int maxDropletLifetime;
            public float inertia;
            public float sedimentCapacityFactor;
            public float minSedimentCapacity;
            
            [Header("浸食設定")]
            public float erodeSpeed;
            public float depositSpeed;
            public float evaporateSpeed;
            public float gravity;
            
            [Header("降雨設定")]
            public float initialWaterVolume;
            public float initialSpeed;
            
            public static HydraulicErosionParams Default()
            {
                return new HydraulicErosionParams
                {
                    iterations = 50000,
                    maxDropletLifetime = 30,
                    inertia = 0.05f,
                    sedimentCapacityFactor = 4f,
                    minSedimentCapacity = 0.01f,
                    erodeSpeed = 0.3f,
                    depositSpeed = 0.3f,
                    evaporateSpeed = 0.01f,
                    gravity = 4f,
                    initialWaterVolume = 1f,
                    initialSpeed = 1f
                };
            }
        }

        /// <summary>
        /// 水滴構造体
        /// </summary>
        private struct Droplet
        {
            public Vector2 position;
            public Vector2 direction;
            public float speed;
            public float water;
            public float sediment;
        }

        /// <summary>
        /// 水力浸食シミュレーション
        /// </summary>
        public static float[,] ApplyHydraulicErosion(float[,] heightmap, HydraulicErosionParams parameters)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] erodedMap = (float[,])heightmap.Clone();
            
            System.Random random = new System.Random();
            
            for (int iteration = 0; iteration < parameters.iterations; iteration++)
            {
                // ランダムな位置に水滴を生成
                Droplet droplet = new Droplet
                {
                    position = new Vector2(
                        random.Next(0, mapSize - 1) + (float)random.NextDouble(),
                        random.Next(0, mapSize - 1) + (float)random.NextDouble()
                    ),
                    direction = Vector2.zero,
                    speed = parameters.initialSpeed,
                    water = parameters.initialWaterVolume,
                    sediment = 0f
                };
                
                // 水滴のライフサイクル
                for (int lifetime = 0; lifetime < parameters.maxDropletLifetime; lifetime++)
                {
                    int nodeX = Mathf.FloorToInt(droplet.position.x);
                    int nodeY = Mathf.FloorToInt(droplet.position.y);
                    
                    // 境界チェック
                    if (nodeX < 0 || nodeX >= mapSize - 1 || nodeY < 0 || nodeY >= mapSize - 1)
                        break;
                    
                    // 現在位置の高度と勾配を計算
                    float cellOffsetX = droplet.position.x - nodeX;
                    float cellOffsetY = droplet.position.y - nodeY;
                    
                    // バイリニア補間で高度を取得
                    float heightNW = erodedMap[nodeY, nodeX];
                    float heightNE = erodedMap[nodeY, nodeX + 1];
                    float heightSW = erodedMap[nodeY + 1, nodeX];
                    float heightSE = erodedMap[nodeY + 1, nodeX + 1];
                    
                    float height = heightNW * (1 - cellOffsetX) * (1 - cellOffsetY) +
                                  heightNE * cellOffsetX * (1 - cellOffsetY) +
                                  heightSW * (1 - cellOffsetX) * cellOffsetY +
                                  heightSE * cellOffsetX * cellOffsetY;
                    
                    // 勾配を計算
                    float gradientX = (heightNE - heightNW) * (1 - cellOffsetY) + (heightSE - heightSW) * cellOffsetY;
                    float gradientY = (heightSW - heightNW) * (1 - cellOffsetX) + (heightSE - heightNE) * cellOffsetX;
                    
                    Vector2 gradient = new Vector2(gradientX, gradientY);
                    
                    // 新しい方向を計算（慣性と勾配の組み合わせ）
                    droplet.direction = droplet.direction * parameters.inertia - gradient * (1 - parameters.inertia);
                    
                    // 正規化
                    if (droplet.direction.magnitude > 0)
                        droplet.direction = droplet.direction.normalized;
                    
                    // 新しい位置を計算
                    Vector2 newPosition = droplet.position + droplet.direction * droplet.speed;
                    
                    // 新しい高度を取得
                    float newHeight = GetHeightAtPosition(erodedMap, newPosition);
                    
                    // 高度差を計算
                    float deltaHeight = newHeight - height;
                    
                    // 堆積物容量を計算
                    float sedimentCapacity = Mathf.Max(-deltaHeight * droplet.speed * droplet.water * parameters.sedimentCapacityFactor, parameters.minSedimentCapacity);
                    
                    // 浸食または堆積
                    if (droplet.sediment > sedimentCapacity || deltaHeight > 0)
                    {
                        // 堆積
                        float amountToDeposit = (deltaHeight > 0) ? 
                            Mathf.Min(deltaHeight, droplet.sediment) : 
                            (droplet.sediment - sedimentCapacity) * parameters.depositSpeed;
                        
                        droplet.sediment -= amountToDeposit;
                        
                        // 堆積物を地形に追加
                        DepositSediment(erodedMap, droplet.position, amountToDeposit);
                    }
                    else
                    {
                        // 浸食
                        float amountToErode = Mathf.Min((sedimentCapacity - droplet.sediment) * parameters.erodeSpeed, -deltaHeight);
                        
                        // 地形から土砂を除去
                        ErodeTerrain(erodedMap, droplet.position, amountToErode);
                        droplet.sediment += amountToErode;
                    }
                    
                    // 速度を更新
                    droplet.speed = Mathf.Sqrt(droplet.speed * droplet.speed + deltaHeight * parameters.gravity);
                    droplet.water *= (1 - parameters.evaporateSpeed);
                    
                    // 位置を更新
                    droplet.position = newPosition;
                }
            }
            
            return erodedMap;
        }

        /// <summary>
        /// 指定位置の高度を取得（バイリニア補間）
        /// </summary>
        private static float GetHeightAtPosition(float[,] heightmap, Vector2 position)
        {
            int mapSize = heightmap.GetLength(0);
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);
            
            if (x < 0 || x >= mapSize - 1 || y < 0 || y >= mapSize - 1)
                return 0f;
            
            float offsetX = position.x - x;
            float offsetY = position.y - y;
            
            float heightNW = heightmap[y, x];
            float heightNE = heightmap[y, x + 1];
            float heightSW = heightmap[y + 1, x];
            float heightSE = heightmap[y + 1, x + 1];
            
            return heightNW * (1 - offsetX) * (1 - offsetY) +
                   heightNE * offsetX * (1 - offsetY) +
                   heightSW * (1 - offsetX) * offsetY +
                   heightSE * offsetX * offsetY;
        }

        /// <summary>
        /// 地形を浸食
        /// </summary>
        private static void ErodeTerrain(float[,] heightmap, Vector2 position, float amount)
        {
            int mapSize = heightmap.GetLength(0);
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);
            
            if (x < 0 || x >= mapSize - 1 || y < 0 || y >= mapSize - 1)
                return;
            
            float offsetX = position.x - x;
            float offsetY = position.y - y;
            
            // 重み付きで浸食を分散
            float weightNW = (1 - offsetX) * (1 - offsetY);
            float weightNE = offsetX * (1 - offsetY);
            float weightSW = (1 - offsetX) * offsetY;
            float weightSE = offsetX * offsetY;
            
            heightmap[y, x] -= amount * weightNW;
            heightmap[y, x + 1] -= amount * weightNE;
            heightmap[y + 1, x] -= amount * weightSW;
            heightmap[y + 1, x + 1] -= amount * weightSE;
        }

        /// <summary>
        /// 堆積物を堆積
        /// </summary>
        private static void DepositSediment(float[,] heightmap, Vector2 position, float amount)
        {
            int mapSize = heightmap.GetLength(0);
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);
            
            if (x < 0 || x >= mapSize - 1 || y < 0 || y >= mapSize - 1)
                return;
            
            float offsetX = position.x - x;
            float offsetY = position.y - y;
            
            // 重み付きで堆積を分散
            float weightNW = (1 - offsetX) * (1 - offsetY);
            float weightNE = offsetX * (1 - offsetY);
            float weightSW = (1 - offsetX) * offsetY;
            float weightSE = offsetX * offsetY;
            
            heightmap[y, x] += amount * weightNW;
            heightmap[y, x + 1] += amount * weightNE;
            heightmap[y + 1, x] += amount * weightSW;
            heightmap[y + 1, x + 1] += amount * weightSE;
        }

        #endregion

        #region 複数回浸食サイクル

        /// <summary>
        /// 複数回の浸食サイクルによる自然な地形形成
        /// </summary>
        public static float[,] ApplyMultiCycleErosion(float[,] heightmap, int cycles = 3)
        {
            float[,] result = (float[,])heightmap.Clone();
            
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                // サイクルごとに異なるパラメータを使用
                var erosionParams = HydraulicErosionParams.Default();
                
                // サイクルが進むにつれて浸食を弱くする
                float cycleFactor = 1f - (cycle / (float)cycles * 0.5f);
                erosionParams.erodeSpeed *= cycleFactor;
                erosionParams.iterations = Mathf.RoundToInt(erosionParams.iterations * cycleFactor);
                
                result = ApplyHydraulicErosion(result, erosionParams);
                
                // 中間結果をスムージング
                if (cycle < cycles - 1)
                {
                    result = ApplySmoothing(result, 0.1f);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 地形スムージング
        /// </summary>
        private static float[,] ApplySmoothing(float[,] heightmap, float strength)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] smoothed = new float[mapSize, mapSize];
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    float totalHeight = 0f;
                    int sampleCount = 0;
                    
                    // 3x3カーネルで平均化
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int sampleX = Mathf.Clamp(x + dx, 0, mapSize - 1);
                            int sampleY = Mathf.Clamp(y + dy, 0, mapSize - 1);
                            
                            totalHeight += heightmap[sampleY, sampleX];
                            sampleCount++;
                        }
                    }
                    
                    float averageHeight = totalHeight / sampleCount;
                    smoothed[y, x] = Mathf.Lerp(heightmap[y, x], averageHeight, strength);
                }
            }
            
            return smoothed;
        }

        #endregion

        #region 降雨・流水・堆積の物理シミュレーション

        [System.Serializable]
        public struct RainfallSimulationParams
        {
            [Header("降雨設定")]
            public float rainfallIntensity;
            public float rainfallDuration;
            public int simulationSteps;
            
            [Header("流水設定")]
            public float flowSpeed;
            public float evaporationRate;
            public float infiltrationRate;
            
            [Header("堆積設定")]
            public float sedimentCarryingCapacity;
            public float depositionRate;
            
            public static RainfallSimulationParams Default()
            {
                return new RainfallSimulationParams
                {
                    rainfallIntensity = 0.1f,
                    rainfallDuration = 100f,
                    simulationSteps = 50,
                    flowSpeed = 1f,
                    evaporationRate = 0.01f,
                    infiltrationRate = 0.02f,
                    sedimentCarryingCapacity = 0.5f,
                    depositionRate = 0.1f
                };
            }
        }

        /// <summary>
        /// 降雨・流水・堆積の統合シミュレーション
        /// </summary>
        public static float[,] ApplyRainfallSimulation(float[,] heightmap, RainfallSimulationParams parameters)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] result = (float[,])heightmap.Clone();
            float[,] waterLevel = new float[mapSize, mapSize];
            float[,] sedimentLevel = new float[mapSize, mapSize];
            
            for (int step = 0; step < parameters.simulationSteps; step++)
            {
                // 降雨
                ApplyRainfall(waterLevel, parameters.rainfallIntensity);
                
                // 流水計算
                SimulateWaterFlow(result, waterLevel, sedimentLevel, parameters);
                
                // 蒸発と浸透
                ApplyEvaporationAndInfiltration(waterLevel, parameters);
                
                // 堆積
                ApplyDeposition(result, sedimentLevel, parameters.depositionRate);
            }
            
            return result;
        }

        /// <summary>
        /// 降雨を適用
        /// </summary>
        private static void ApplyRainfall(float[,] waterLevel, float intensity)
        {
            int mapSize = waterLevel.GetLength(0);
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    waterLevel[y, x] += intensity;
                }
            }
        }

        /// <summary>
        /// 流水シミュレーション
        /// </summary>
        private static void SimulateWaterFlow(float[,] heightmap, float[,] waterLevel, float[,] sedimentLevel, RainfallSimulationParams parameters)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] newWaterLevel = (float[,])waterLevel.Clone();
            float[,] newSedimentLevel = (float[,])sedimentLevel.Clone();
            
            for (int y = 1; y < mapSize - 1; y++)
            {
                for (int x = 1; x < mapSize - 1; x++)
                {
                    float currentHeight = heightmap[y, x] + waterLevel[y, x];
                    float totalFlow = 0f;
                    
                    // 8方向への流水を計算
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            int neighborX = x + dx;
                            int neighborY = y + dy;
                            
                            float neighborHeight = heightmap[neighborY, neighborX] + waterLevel[neighborY, neighborX];
                            float heightDiff = currentHeight - neighborHeight;
                            
                            if (heightDiff > 0)
                            {
                                float flow = Mathf.Min(waterLevel[y, x], heightDiff * parameters.flowSpeed);
                                
                                newWaterLevel[y, x] -= flow;
                                newWaterLevel[neighborY, neighborX] += flow;
                                
                                // 堆積物も一緒に運ぶ
                                float sedimentFlow = flow * parameters.sedimentCarryingCapacity;
                                newSedimentLevel[y, x] -= sedimentFlow;
                                newSedimentLevel[neighborY, neighborX] += sedimentFlow;
                                
                                totalFlow += flow;
                            }
                        }
                    }
                    
                    // 流水による浸食
                    if (totalFlow > 0.01f)
                    {
                        float erosion = totalFlow * 0.01f;
                        heightmap[y, x] -= erosion;
                        newSedimentLevel[y, x] += erosion;
                    }
                }
            }
            
            // 結果を適用
            System.Array.Copy(newWaterLevel, waterLevel, waterLevel.Length);
            System.Array.Copy(newSedimentLevel, sedimentLevel, sedimentLevel.Length);
        }

        /// <summary>
        /// 蒸発と浸透を適用
        /// </summary>
        private static void ApplyEvaporationAndInfiltration(float[,] waterLevel, RainfallSimulationParams parameters)
        {
            int mapSize = waterLevel.GetLength(0);
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    // 蒸発
                    waterLevel[y, x] *= (1f - parameters.evaporationRate);
                    
                    // 浸透
                    waterLevel[y, x] *= (1f - parameters.infiltrationRate);
                }
            }
        }

        /// <summary>
        /// 堆積を適用
        /// </summary>
        private static void ApplyDeposition(float[,] heightmap, float[,] sedimentLevel, float depositionRate)
        {
            int mapSize = heightmap.GetLength(0);
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    float deposition = sedimentLevel[y, x] * depositionRate;
                    heightmap[y, x] += deposition;
                    sedimentLevel[y, x] -= deposition;
                }
            }
        }

        #endregion

        #region 熱浸食・風化システム

        [System.Serializable]
        public struct ThermalErosionParams
        {
            [Header("基本設定")]
            public int iterations;
            public float talusAngle;
            public float thermalErosionRate;
            
            [Header("温度設定")]
            public float baseTemperature;
            public float temperatureVariation;
            public float altitudeTemperatureGradient;
            
            [Header("風化設定")]
            public float weatheringRate;
            public float freezeThawCycles;
            public float chemicalWeatheringRate;
            
            public static ThermalErosionParams Default()
            {
                return new ThermalErosionParams
                {
                    iterations = 100,
                    talusAngle = 30f, // 安息角（度）
                    thermalErosionRate = 0.1f,
                    baseTemperature = 15f, // 摂氏
                    temperatureVariation = 20f,
                    altitudeTemperatureGradient = -6.5f, // 1000mあたり-6.5度
                    weatheringRate = 0.05f,
                    freezeThawCycles = 50f,
                    chemicalWeatheringRate = 0.02f
                };
            }
        }

        /// <summary>
        /// 熱浸食シミュレーション
        /// 斜面の安定化と自然な角度の維持
        /// </summary>
        public static float[,] ApplyThermalErosion(float[,] heightmap, ThermalErosionParams parameters)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] result = (float[,])heightmap.Clone();
            
            float talusAngleRadians = parameters.talusAngle * Mathf.Deg2Rad;
            float maxHeightDiff = Mathf.Tan(talusAngleRadians);
            
            for (int iteration = 0; iteration < parameters.iterations; iteration++)
            {
                float[,] newHeightmap = (float[,])result.Clone();
                
                for (int y = 1; y < mapSize - 1; y++)
                {
                    for (int x = 1; x < mapSize - 1; x++)
                    {
                        float currentHeight = result[y, x];
                        float totalMaterialToMove = 0f;
                        List<Vector2Int> lowerNeighbors = new List<Vector2Int>();
                        List<float> heightDifferences = new List<float>();
                        
                        // 8方向の隣接セルをチェック
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                
                                int neighborX = x + dx;
                                int neighborY = y + dy;
                                float neighborHeight = result[neighborY, neighborX];
                                float heightDiff = currentHeight - neighborHeight;
                                
                                // 距離を考慮（対角線は√2倍）
                                float distance = (dx != 0 && dy != 0) ? 1.414f : 1f;
                                float slope = heightDiff / distance;
                                
                                if (slope > maxHeightDiff)
                                {
                                    lowerNeighbors.Add(new Vector2Int(neighborX, neighborY));
                                    heightDifferences.Add(heightDiff);
                                    totalMaterialToMove += heightDiff - maxHeightDiff * distance;
                                }
                            }
                        }
                        
                        // 材料を移動
                        if (lowerNeighbors.Count > 0 && totalMaterialToMove > 0)
                        {
                            float materialToMove = totalMaterialToMove * parameters.thermalErosionRate;
                            newHeightmap[y, x] -= materialToMove;
                            
                            // 各隣接セルに材料を分配
                            for (int i = 0; i < lowerNeighbors.Count; i++)
                            {
                                Vector2Int neighbor = lowerNeighbors[i];
                                float proportion = heightDifferences[i] / totalMaterialToMove;
                                newHeightmap[neighbor.y, neighbor.x] += materialToMove * proportion;
                            }
                        }
                    }
                }
                
                result = newHeightmap;
            }
            
            return result;
        }

        /// <summary>
        /// 温度変化による岩石の風化シミュレーション
        /// </summary>
        public static float[,] ApplyWeatheringSimulation(float[,] heightmap, ThermalErosionParams parameters)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] result = (float[,])heightmap.Clone();
            float[,] temperatureMap = GenerateTemperatureMap(heightmap, parameters);
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    float currentHeight = result[y, x];
                    float temperature = temperatureMap[y, x];
                    
                    // 物理的風化（凍結融解）
                    float physicalWeathering = CalculatePhysicalWeathering(temperature, parameters);
                    
                    // 化学的風化
                    float chemicalWeathering = CalculateChemicalWeathering(temperature, currentHeight, parameters);
                    
                    // 総風化量
                    float totalWeathering = (physicalWeathering + chemicalWeathering) * parameters.weatheringRate;
                    
                    // 風化による高度減少
                    result[y, x] -= totalWeathering;
                    
                    // 風化した材料を周囲に分散
                    DistributeWeatheredMaterial(result, x, y, totalWeathering);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 温度マップを生成
        /// </summary>
        private static float[,] GenerateTemperatureMap(float[,] heightmap, ThermalErosionParams parameters)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] temperatureMap = new float[mapSize, mapSize];
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    float altitude = heightmap[y, x] * 1000f; // メートル単位に変換
                    
                    // 高度による温度変化
                    float altitudeEffect = altitude * parameters.altitudeTemperatureGradient / 1000f;
                    
                    // 基本温度 + 高度効果 + ランダム変動
                    float temperature = parameters.baseTemperature + altitudeEffect + 
                                      (Random.Range(-1f, 1f) * parameters.temperatureVariation);
                    
                    temperatureMap[y, x] = temperature;
                }
            }
            
            return temperatureMap;
        }

        /// <summary>
        /// 物理的風化を計算（凍結融解）
        /// </summary>
        private static float CalculatePhysicalWeathering(float temperature, ThermalErosionParams parameters)
        {
            // 0度付近で最大の凍結融解効果
            float freezeThawIntensity = 1f - Mathf.Abs(temperature) / 10f;
            freezeThawIntensity = Mathf.Clamp01(freezeThawIntensity);
            
            return freezeThawIntensity * parameters.freezeThawCycles * 0.001f;
        }

        /// <summary>
        /// 化学的風化を計算
        /// </summary>
        private static float CalculateChemicalWeathering(float temperature, float height, ThermalErosionParams parameters)
        {
            // 温度が高いほど化学的風化が活発
            float temperatureEffect = Mathf.Clamp01((temperature + 10f) / 40f);
            
            // 低地ほど水分が多く化学的風化が活発
            float moistureEffect = 1f - Mathf.Clamp01(height);
            
            return temperatureEffect * moistureEffect * parameters.chemicalWeatheringRate;
        }

        /// <summary>
        /// 風化した材料を周囲に分散
        /// </summary>
        private static void DistributeWeatheredMaterial(float[,] heightmap, int centerX, int centerY, float material)
        {
            int mapSize = heightmap.GetLength(0);
            float materialPerCell = material / 9f; // 3x3グリッドに分散
            
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (x >= 0 && x < mapSize && y >= 0 && y < mapSize)
                    {
                        // 中心から遠いほど少ない材料を堆積
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float weight = 1f / (1f + distance);
                        
                        heightmap[y, x] += materialPerCell * weight;
                    }
                }
            }
        }

        /// <summary>
        /// 気候条件に応じた風化パターンの実装
        /// </summary>
        public static float[,] ApplyClimateBasedWeathering(float[,] heightmap, ClimateConditions climate)
        {
            int mapSize = heightmap.GetLength(0);
            float[,] result = (float[,])heightmap.Clone();
            
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    float currentHeight = result[y, x];
                    
                    // 気候に基づく風化計算
                    float weatheringRate = CalculateClimateWeathering(climate, currentHeight);
                    
                    // 風化を適用
                    result[y, x] -= weatheringRate;
                    
                    // 風化した材料を低地に移動
                    MoveWeatheredMaterialDownhill(result, x, y, weatheringRate);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 気候条件構造体
        /// </summary>
        [System.Serializable]
        public struct ClimateConditions
        {
            public float temperature;      // 平均気温
            public float precipitation;    // 降水量
            public float humidity;         // 湿度
            public float windSpeed;        // 風速
            public float seasonalVariation; // 季節変動
            
            public static ClimateConditions Temperate()
            {
                return new ClimateConditions
                {
                    temperature = 15f,
                    precipitation = 800f,
                    humidity = 0.6f,
                    windSpeed = 10f,
                    seasonalVariation = 0.5f
                };
            }
            
            public static ClimateConditions Arid()
            {
                return new ClimateConditions
                {
                    temperature = 25f,
                    precipitation = 200f,
                    humidity = 0.2f,
                    windSpeed = 15f,
                    seasonalVariation = 0.3f
                };
            }
            
            public static ClimateConditions Tropical()
            {
                return new ClimateConditions
                {
                    temperature = 28f,
                    precipitation = 2000f,
                    humidity = 0.8f,
                    windSpeed = 5f,
                    seasonalVariation = 0.2f
                };
            }
        }

        /// <summary>
        /// 気候に基づく風化率を計算
        /// </summary>
        private static float CalculateClimateWeathering(ClimateConditions climate, float height)
        {
            // 温度効果（高温ほど化学的風化が活発）
            float temperatureEffect = Mathf.Clamp01(climate.temperature / 30f);
            
            // 降水量効果（多雨ほど風化が活発）
            float precipitationEffect = Mathf.Clamp01(climate.precipitation / 1500f);
            
            // 湿度効果
            float humidityEffect = climate.humidity;
            
            // 風速効果（物理的風化）
            float windEffect = Mathf.Clamp01(climate.windSpeed / 20f);
            
            // 高度効果（高地ほど風化が激しい）
            float altitudeEffect = 1f + height * 0.5f;
            
            // 総合風化率
            float totalWeathering = (temperatureEffect * precipitationEffect * humidityEffect + windEffect) * 
                                  altitudeEffect * climate.seasonalVariation * 0.001f;
            
            return totalWeathering;
        }

        /// <summary>
        /// 風化した材料を下り坂に移動
        /// </summary>
        private static void MoveWeatheredMaterialDownhill(float[,] heightmap, int x, int y, float material)
        {
            int mapSize = heightmap.GetLength(0);
            float currentHeight = heightmap[y, x];
            
            // 最も低い隣接セルを見つける
            int lowestX = x;
            int lowestY = y;
            float lowestHeight = currentHeight;
            
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int neighborX = x + dx;
                    int neighborY = y + dy;
                    
                    if (neighborX >= 0 && neighborX < mapSize && neighborY >= 0 && neighborY < mapSize)
                    {
                        float neighborHeight = heightmap[neighborY, neighborX];
                        if (neighborHeight < lowestHeight)
                        {
                            lowestHeight = neighborHeight;
                            lowestX = neighborX;
                            lowestY = neighborY;
                        }
                    }
                }
            }
            
            // 材料を最も低い場所に移動
            if (lowestX != x || lowestY != y)
            {
                heightmap[lowestY, lowestX] += material * 0.5f; // 50%を移動
            }
        }

        #endregion

        #region 統合システム

        /// <summary>
        /// 水力浸食と熱浸食を組み合わせた統合システム
        /// </summary>
        public static float[,] ApplyIntegratedErosion(float[,] heightmap, 
            HydraulicErosionParams hydraulicParams, 
            ThermalErosionParams thermalParams,
            ClimateConditions climate)
        {
            float[,] result = (float[,])heightmap.Clone();
            
            // 1. 熱浸食（斜面安定化）
            result = ApplyThermalErosion(result, thermalParams);
            
            // 2. 気候ベース風化
            result = ApplyClimateBasedWeathering(result, climate);
            
            // 3. 水力浸食（詳細な地形形成）
            result = ApplyHydraulicErosion(result, hydraulicParams);
            
            // 4. 最終的な風化処理
            result = ApplyWeatheringSimulation(result, thermalParams);
            
            return result;
        }

        /// <summary>
        /// 長期的な地形変化プロセス
        /// </summary>
        public static float[,] ApplyLongTermTerrainEvolution(float[,] heightmap, int timeSteps = 10)
        {
            float[,] result = (float[,])heightmap.Clone();
            
            var hydraulicParams = HydraulicErosionParams.Default();
            var thermalParams = ThermalErosionParams.Default();
            var climate = ClimateConditions.Temperate();
            
            for (int step = 0; step < timeSteps; step++)
            {
                // 時間経過による気候変化をシミュレート
                climate.temperature += Random.Range(-2f, 2f);
                climate.precipitation *= Random.Range(0.8f, 1.2f);
                
                // 段階的な浸食強度の調整
                float timeProgress = step / (float)timeSteps;
                hydraulicParams.erodeSpeed *= (1f - timeProgress * 0.3f);
                thermalParams.weatheringRate *= (1f + timeProgress * 0.2f);
                
                // 統合浸食を適用
                result = ApplyIntegratedErosion(result, hydraulicParams, thermalParams, climate);
                
                // 中間スムージング
                if (step % 3 == 0)
                {
                    result = ApplySmoothing(result, 0.05f);
                }
            }
            
            return result;
        }

        #endregion
    }
}