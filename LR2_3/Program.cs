using System;
using System.Collections.Generic;

namespace LR2_3
{
    // ==========================
    // 1. МОДЕЛЬ ЗАПРОСА И ОТВЕТА
    // ==========================
    public class Request
    {
        public string ServiceName { get; set; } = string.Empty;
        public int PayloadSize { get; set; }
        public int? DeadlineMs { get; set; }

        public Request(string serviceName, int payloadSize, int? deadlineMs = null)
        {
            ServiceName = serviceName;
            PayloadSize = payloadSize;
            DeadlineMs = deadlineMs;
        }
    }

    public class Response
    {
        public bool IsSuccess { get; set; }
        public int LatencyMs { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public Response(bool isSuccess, int latencyMs, string? errorCode = null, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            LatencyMs = latencyMs;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }

    // ====================
    // 2. ИНТЕРФЕЙС СЕРВИСА
    // ====================
    public interface IService
    {
        string Name { get; }
        int BaseLatencyMs { get; }
        double FailureProbability { get; }
        Response Process(Request request);
    }

    // ========================
    // 3. БАЗОВЫЙ КЛАСС СЕРВИСА
    // ========================
    public abstract class ServiceBase : IService
    {
        public string Name { get; protected set; }
        public int BaseLatencyMs { get; protected set; }
        public double FailureProbability { get; protected set; }

        protected ServiceBase(string name, int baseLatencyMs, double failureProbability)
        {
            Name = name;
            BaseLatencyMs = baseLatencyMs;
            FailureProbability = failureProbability;
        }

        public abstract Response Process(Request request);

        protected virtual void Log(Request request, Response response)
        {
            Console.WriteLine($"{Name}: size = {request.PayloadSize} --> " +
                              $"{(response.IsSuccess ? "OK" : "FAIL")} ({response.LatencyMs}ms)");
        }
    }

    public class FastService : ServiceBase
    {
        public FastService() : base("FastService", baseLatencyMs: 50, failureProbability: 0.05) { }

        public override Response Process(Request request)
        {
            int latency = BaseLatencyMs + Random.Shared.Next(-10, 20);
            latency = Math.Max(1, latency);

            bool isSuccess = Random.Shared.NextDouble() > FailureProbability;
            var response = new Response(isSuccess, latency,
                isSuccess ? null : "Error_Fast",
                isSuccess ? null : "FastService failed");

            Log(request, response);
            return response;
        }
    }

    public class SlowService : ServiceBase
    {
        public SlowService() : base("SlowService", baseLatencyMs: 200, failureProbability: 0.15) { }

        public override Response Process(Request request)
        {
            int latency = BaseLatencyMs + Random.Shared.Next(-30, 60);
            latency = Math.Max(10, latency);

            bool isSuccess = Random.Shared.NextDouble() > FailureProbability;
            var response = new Response(isSuccess, latency,
                isSuccess ? null : "Error_Slow",
                isSuccess ? null : "SlowService failed");

            Log(request, response);
            return response;
        }
    }

    // ===============================
    // 4. МЕТРИКИ И СБОР НАБЛЮДАЕМОСТИ
    // ===============================
    public class ServiceMetrics
    {
        public string ServiceName { get; }
        public int TotalRequests { get; private set; }
        public int SuccessfulRequests { get; private set; }
        public int FailedRequests { get; private set; }
        public double AverageLatencyMs { get; private set; }
        public int MaxLatencyMs { get; private set; }

        private double totalLatency = 0;

        public ServiceMetrics(string serviceName)
        {
            ServiceName = serviceName;
        }

        public double ErrorRate => TotalRequests == 0 ? 0.0 : (double)FailedRequests / TotalRequests;

        public void Update(Response response)
        {
            TotalRequests++;
            if (response.IsSuccess)
                SuccessfulRequests++;
            else
                FailedRequests++;

            totalLatency += response.LatencyMs;
            AverageLatencyMs = totalLatency / TotalRequests;
            if (response.LatencyMs > MaxLatencyMs)
            {
                MaxLatencyMs = response.LatencyMs;
            }
        }
    }

    public interface IMetricsCollector
    {
        void RegisterService(IService service);
        void Record(Request request, Response response);
        IReadOnlyCollection<ServiceMetrics> GetCurrentMetrics();
        event Action<ServiceMetrics>? OnMetricsUpdated;
    }

    public class InMemoryMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, ServiceMetrics> _metricsByService = new();

        public event Action<ServiceMetrics>? OnMetricsUpdated;

        public void RegisterService(IService service)
        {
            if (!_metricsByService.ContainsKey(service.Name))
            {
                _metricsByService[service.Name] = new ServiceMetrics(service.Name);
            }
        }

        public void Record(Request request, Response response)
        {
            if (_metricsByService.TryGetValue(request.ServiceName, out var metrics))
            {
                metrics.Update(response);
                OnMetricsUpdated?.Invoke(metrics);
            }
        }

        public IReadOnlyCollection<ServiceMetrics> GetCurrentMetrics()
        {
            return _metricsByService.Values;
        }

    }

    // ============================
    // 5. ОЦЕНКА «ЗДОРОВЬЯ» СЕРВИСА
    // ============================
    public enum ServiceHealth
    {
        Healthy, Degraded, Unhealthy
    }

    public class ServiceHealthEvaluator
    {
        public double MaxHealthyErrorRate { get; set; } = 0.05;
        public double MaxDegradedErrorRate { get; set; } = 0.20;
        public int MaxHealthyLatencyMs { get; set; } = 150;
        public int MaxDegradedLatencyMs { get; set; } = 400;

        public ServiceHealth Evaluate(ServiceMetrics metrics)
        {
            bool highError = metrics.ErrorRate >= MaxDegradedErrorRate;
            bool highLatency = metrics.AverageLatencyMs > MaxDegradedLatencyMs;

            if (highError || highLatency)
                return ServiceHealth.Unhealthy;

            bool mediumError = metrics.ErrorRate > MaxHealthyErrorRate;
            bool mediumLatency = metrics.AverageLatencyMs > MaxHealthyLatencyMs;

            if (mediumError || mediumLatency)
                return ServiceHealth.Degraded;

            return ServiceHealth.Healthy;
        }

        public ServiceHealth EvaluateOverall(double avgLatency, double errorRate)
        {
            bool highError = errorRate >= MaxDegradedErrorRate;
            bool highLatency = avgLatency > MaxDegradedLatencyMs;

            if (highError || highLatency)
                return ServiceHealth.Unhealthy;

            bool mediumError = errorRate > MaxHealthyErrorRate;
            bool mediumLatency = avgLatency > MaxHealthyLatencyMs;

            if (mediumError || mediumLatency)
                return ServiceHealth.Degraded;

            return ServiceHealth.Healthy;
        }
    }
    // ======================================================
    // 6. БАЛАНСИРОВКА НАГРУЗКИ МЕЖДУ СЕРВИСАМИ (Вариант №14)
    // ======================================================

    public interface ILoadBalancer
    {
        IService SelectService(Request request, IReadOnlyList<IService> services, IMetricsCollector metricsCollector);
        string StrategyName { get; }
    }

    //Round-robin (циклический выбор)
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        private int currentIndex = 0;
        private readonly object lockObject = new object();

        public string StrategyName => "Round-Robin";

        public IService SelectService(Request request, IReadOnlyList<IService> services, IMetricsCollector metricsCollector)
        {
            if (services == null || services.Count == 0)
                throw new InvalidOperationException("Нет доступных сервисов");

            lock (lockObject)
            {
                var service = services[currentIndex];
                currentIndex = (currentIndex + 1) % services.Count;
                return service;
            }
        }
    }

    //Least-latency (выбор сервиса с минимальной средней задержкой)
    public class LeastLatencyLoadBalancer : ILoadBalancer
    {
        private readonly Random random = new Random();

        public string StrategyName => "Least-Latency";

        public IService SelectService(Request request, IReadOnlyList<IService> services, IMetricsCollector metricsCollector)
        {
            if (services == null || services.Count == 0)
                throw new InvalidOperationException("Нет доступных сервисов");

            var metrics = metricsCollector.GetCurrentMetrics();
            var metricsDict = metrics.ToDictionary(m => m.ServiceName);

            // Группировка сервисов по типу
            var fastServices = services.Where(s => s.Name.Contains("Fast")).ToList();
            var slowServices = services.Where(s => s.Name.Contains("Slow")).ToList();

            if (metrics.Count == 0)
            {
                return services[random.Next(services.Count)];
            }

            IService bestFastService = null;
            double bestFastLatency = double.MaxValue;

            foreach (var service in fastServices)
            {
                if (metricsDict.TryGetValue(service.Name, out var serviceMetrics) && serviceMetrics.TotalRequests > 0)
                {
                    // Учитывается не абсолютная задержка, а эффективность относительно базовой
                    double efficiency = service.BaseLatencyMs / (double)serviceMetrics.AverageLatencyMs;
                    double adjustedLatency = serviceMetrics.AverageLatencyMs * (1.0 / efficiency);

                    if (adjustedLatency < bestFastLatency)
                    {
                        bestFastLatency = adjustedLatency;
                        bestFastService = service;
                    }
                }
                else if (bestFastService == null)
                {
                    bestFastService = service;
                }
            }

            IService bestSlowService = null;
            double bestSlowLatency = double.MaxValue;

            foreach (var service in slowServices)
            {
                if (metricsDict.TryGetValue(service.Name, out var serviceMetrics) && serviceMetrics.TotalRequests > 0)
                {
                    // Для SlowService используется взвешенная оценка с учетом ошибок
                    double penalty = serviceMetrics.ErrorRate * 100; 
                    double adjustedLatency = serviceMetrics.AverageLatencyMs * (1 + penalty);

                    if (adjustedLatency < bestSlowLatency)
                    {
                        bestSlowLatency = adjustedLatency;
                        bestSlowService = service;
                    }
                }
                else if (bestSlowService == null)
                {
                    bestSlowService = service;
                }
            }

            bool useFastService = random.NextDouble() < 0.7;

            if (useFastService && bestFastService != null)
                return bestFastService;
            else if (bestSlowService != null)
                return bestSlowService;
            else
                return services[random.Next(services.Count)];
        }
    }

    // Random
    public class RandomLoadBalancer : ILoadBalancer
    {
        private readonly Random random = new Random();

        public string StrategyName => "Random";

        public IService SelectService(Request request, IReadOnlyList<IService> services, IMetricsCollector metricsCollector)
        {
            if (services == null || services.Count == 0)
                throw new InvalidOperationException("Нет доступных сервисов");

            int index = random.Next(services.Count);
            return services[index];
        }
    }

    public class LoadBalancingSystem
    {
        private readonly ILoadBalancer _loadBalancer;
        private readonly IReadOnlyList<IService> _services;
        private readonly IMetricsCollector _metricsCollector;

        public LoadBalancingSystem(ILoadBalancer loadBalancer, IReadOnlyList<IService> services, IMetricsCollector metricsCollector)
        {
            _loadBalancer = loadBalancer;
            _services = services;
            _metricsCollector = metricsCollector;
        }

        public Response ProcessRequest(Request request)
        {
            var service = _loadBalancer.SelectService(request, _services, _metricsCollector);

            request.ServiceName = service.Name;

            return service.Process(request);
        }

        public string GetStrategyName() => _loadBalancer.StrategyName;
    }


    // ========================
    // 7. ГЛАВНЫЙ МОДУЛЬ (Main)
    // ========================
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ЛР 2-3. Моделирование системы обработки запросов ===\n");
            Console.WriteLine("=== Вариант 14: Балансировка нагрузки между сервисами ===\n");

            var services = new List<IService>
            {
                new FastService(),
                new FastService(),
                new SlowService(),
                new SlowService()
            };

            Console.WriteLine($"Создано {services.Count} сервиса для балансировки:");
            foreach (var service in services)
            {
                Console.WriteLine($"  - {service.Name} (базовая задержка: {service.BaseLatencyMs}ms, вероятность отказа: {service.FailureProbability:P0})");
            }
            Console.WriteLine();

            var loadBalancingStrategies = new List<ILoadBalancer>
            {
                new RoundRobinLoadBalancer(),
                new LeastLatencyLoadBalancer(),
                new RandomLoadBalancer()
            };

            Dictionary<string, IMetricsCollector> results = new Dictionary<string, IMetricsCollector>();

            foreach (var strategy in loadBalancingStrategies)
            {
                Console.WriteLine($"\n=== Тестирование стратегии: {strategy.StrategyName} ===");

                IMetricsCollector metricsCollector = new InMemoryMetricsCollector();

                foreach (var service in services)
                {
                    metricsCollector.RegisterService(service);
                }

                ServiceHealthEvaluator healthEvaluator = new ServiceHealthEvaluator();
                LoadBalancingSystem loadBalancer = new LoadBalancingSystem(strategy, services, metricsCollector);

                List<Request> requests = GenerateRandomRequests(50, 100, services.Select(s => s.Name).Distinct().ToArray());

                Console.WriteLine($"Сгенерировано {requests.Count} запросов.");
                Console.WriteLine("Начало обработки...\n");

                int requestCount = 0;
                foreach (var request in requests)
                {
                    requestCount++;

                    Response response;
                    try
                    {
                        response = loadBalancer.ProcessRequest(request);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"[Error] Исключение при обработке запроса: {ex.Message}");
                        Console.ResetColor();
                        response = new Response(false, 0, "Exeption", ex.Message);
                    }

                    metricsCollector.Record(request, response);

                    if (requestCount % 20 == 0)
                    {
                        Console.WriteLine($"\n--- Промежуточные метрики ({strategy.StrategyName}, количество запросов: {requestCount}) ---");
                        PrintCurrentMetrics(metricsCollector, healthEvaluator);
                    }
                }

                results[strategy.StrategyName] = metricsCollector;

                Console.WriteLine($"\n=== ФИНАЛЬНЫЙ ОТЧЕТ: {strategy.StrategyName} ===");
                PrintCurrentMetrics(metricsCollector, healthEvaluator);
            }

            Console.WriteLine("\n" + "=".PadRight(80, '='));
            Console.WriteLine("СРАВНИТЕЛЬНЫЙ АНАЛИЗ СТРАТЕГИЙ БАЛАНСИРОВКИ");
            Console.WriteLine("=".PadRight(80, '='));

            Console.WriteLine($"\n{"Стратегия",-20} {"Всего запросов",-15} {"Ср.задержка",-15} {"Ошибок %",-15} {"Состояние системы",-20}");
            Console.WriteLine(new string('-', 85));

            ServiceHealthEvaluator evaluator = new ServiceHealthEvaluator();
            foreach (var result in results)
            {
                var metrics = result.Value.GetCurrentMetrics();

                int totalRequests = 0;
                int successfulRequests = 0;
                int failedRequests = 0;
                double totalLatencySum = 0;
                int maxLatency = 0;

                foreach (var m in metrics)
                {
                    totalRequests += m.TotalRequests;
                    successfulRequests += m.SuccessfulRequests;
                    failedRequests += m.FailedRequests;
                    totalLatencySum += m.AverageLatencyMs * m.TotalRequests;

                    if (m.MaxLatencyMs > maxLatency)
                        maxLatency = m.MaxLatencyMs;
                }

                double avgLatencyOverall = totalRequests > 0 ? totalLatencySum / totalRequests : 0;
                double errorRateOverall = totalRequests > 0 ? (double)failedRequests / totalRequests : 0;

             
                ServiceHealth systemHealth = evaluator.EvaluateOverall(avgLatencyOverall, errorRateOverall);

                string healthStr = systemHealth switch
                {
                    ServiceHealth.Healthy => "Healthy",
                    ServiceHealth.Degraded => "Degraded",
                    ServiceHealth.Unhealthy => "Unhealthy",
                    _ => "Unknown"
                };

                Console.WriteLine($"{result.Key,-20}    " +
                                  $"{totalRequests,-15}" +
                                  $"{avgLatencyOverall:F1} ms         " +
                                  $"{errorRateOverall * 100:F1}%            " +
                                  $"{healthStr,-20}");
            }
            Console.WriteLine("\nМоделирование завершено.");
        }

        private static List<Request> GenerateRandomRequests(int minCount, int maxCount, string[] serviceNames)
        {
            Random random = new Random();
            int count = random.Next(minCount, maxCount + 1);
            var requests = new List<Request>();

            for (int i = 0; i < count; i++)
            {
                string serviceName = "any"; 
                int payloadSize = random.Next(10, 1000);
                int? deadlineMs = random.Next(0, 100) < 30 ? random.Next(100, 1000) : (int?)null;

                requests.Add(new Request(serviceName, payloadSize, deadlineMs));
            }

            return requests;
        }

        private static void PrintCurrentMetrics(IMetricsCollector metricsCollector, ServiceHealthEvaluator healthEvaluator)
        {
            var metrics = metricsCollector.GetCurrentMetrics();

            Console.WriteLine($"{"Сервис",-15} {"Запросы",-10} {"Успешные",-10} {"Ошибки",-10} {"Ошибок %",-10} {"Ср.задержка",-15} {"Макс.задержка",-15} {"Состояние"}");
            Console.WriteLine(new string('-', 110));

            foreach (var m in metrics)
            {
                var health = healthEvaluator.Evaluate(m);
                string healthStr = health switch
                {
                    ServiceHealth.Healthy => "Healthy",
                    ServiceHealth.Degraded => "Degraded",
                    ServiceHealth.Unhealthy => "Unhealthy",
                    _ => "Unknown"
                };

                Console.WriteLine($"{m.ServiceName,-15} " +
                                  $"{m.TotalRequests,-10} " +
                                  $"{m.SuccessfulRequests,-10} " +
                                  $"{m.FailedRequests,-10} " +
                                  $"{m.ErrorRate * 100:F1}%       " +
                                  $"{m.AverageLatencyMs:F1} ms          " +
                                  $"{m.MaxLatencyMs} ms         " +
                                  $"{healthStr}");
            }
        }
    }
}