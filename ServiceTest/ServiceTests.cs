using AutoMapper;
using Moq;
using NodeCurrencyConverter.Contracts;
using NodeCurrencyConverter.DTOs;
using NodeCurrencyConverter.Entities;
using NodeCurrencyConverter.Services;
using System.Reflection;

namespace NodeCurrencyConverter.Service.Test
{
    public class CurrencyExchangeServiceTests
    {
        private readonly Mock<ICurrencyExchangeDomainService> _mockDomainService;
        private readonly Mock<ICurrencyExchangeRepository> _mockRepository;
        private readonly Mock<ICurrencyRepositoryCache> _mockCache;
        private readonly Mock<IMapper> _mockMapper;
        private readonly CurrencyExchangeService _service;

        public CurrencyExchangeServiceTests()
        {
            _mockDomainService = new Mock<ICurrencyExchangeDomainService>();
            _mockRepository = new Mock<ICurrencyExchangeRepository>();
            _mockCache = new Mock<ICurrencyRepositoryCache>();
            _mockMapper = new Mock<IMapper>();
            _service = new CurrencyExchangeService(_mockDomainService.Object, 
                _mockRepository.Object, _mockCache.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetAllCurrencies_WithFullCache_ReturnsFromCache()
        {
            /// Arrange
            var cachedCurrencies = new List<CurrencyEntity>
            {
                new CurrencyEntity("USD"),
                new CurrencyEntity("EUR")
            };

            _mockCache.Setup(c => c.GetCacheList<CurrencyEntity>("currency")).Returns(cachedCurrencies);

            _mockMapper.Setup(m => m.Map<List<CurrencyDto>>(It.IsAny<List<CurrencyEntity>>()))
                .Returns((List<CurrencyEntity> entities) =>
                    entities.Select(e => new CurrencyDto(e.Code)).ToList());

            /// Act
            var result = await _service.GetAllCurrencies();

            /// Assert
            Assert.Equal(cachedCurrencies.Count, result.Count);
            Assert.Equal(cachedCurrencies[0].Code, result[0].Code);
            Assert.Equal(cachedCurrencies[1].Code, result[1].Code);
            _mockRepository.Verify(r => r.GetAllCurrencyExchanges(), Times.Never);
        }

        [Fact]
        public async Task GetAllCurrencies_WithEmptyCurrencyCacheButFullExchangeCache_ReturnsFromExchangeCache()
        {
            /// Arrange
            _mockCache.Setup(c => c.GetCacheList<CurrencyEntity>("currency")).Returns(new List<CurrencyEntity>());

            var cachedExchanges = new List<CurrencyExchangeEntity>
            {
                new ( new CurrencyEntity("USD"), new CurrencyEntity("EUR"), 0.85m),
                new ( new CurrencyEntity("EUR"), new CurrencyEntity("GBP"), 0.9m)
            };

            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(cachedExchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyDto>>(It.IsAny<List<CurrencyEntity>>()))
                .Returns((List<CurrencyEntity> entities) =>
                    entities.Select(e => new CurrencyDto(e.Code)).ToList());

            /// Act
            var result = await _service.GetAllCurrencies();

            /// Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.Code == "USD");
            Assert.Contains(result, c => c.Code == "EUR");
            Assert.Contains(result, c => c.Code == "GBP");
            _mockRepository.Verify(r => r.GetAllCurrencyExchanges(), Times.Never);
            _mockCache.Verify(c => c.SetCacheList("currency", It.IsAny<List<CurrencyEntity>>(), TimeSpan.FromSeconds(30)), Times.Once);
        }

        [Fact]
        public async Task GetAllCurrencies_WithEmptyCaches_ReturnsFromRepository()
        {
            /// Arrange
            _mockCache.Setup(c => c.GetCacheList<CurrencyEntity>("currency")).Returns(new List<CurrencyEntity>());

            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(new List<CurrencyExchangeEntity>());

            var repositoryExchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new("USD"),
                    new("EUR"),
                    0.85m
                ),
                new
                (
                    new("EUR"),
                    new("GBP"),
                    0.9m
                )
            };

            _mockRepository.Setup(r => r.GetAllCurrencyExchanges()).ReturnsAsync(repositoryExchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyDto>>(It.IsAny<List<CurrencyEntity>>()))
                .Returns((List<CurrencyEntity> entities) =>
                    entities.Select(e => new CurrencyDto(e.Code)).ToList());

            /// Act
            var result = await _service.GetAllCurrencies();

            /// Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.Code == "USD");
            Assert.Contains(result, c => c.Code == "EUR");
            Assert.Contains(result, c => c.Code == "GBP");
            _mockRepository.Verify(r => r.GetAllCurrencyExchanges(), Times.Once);
            _mockCache.Verify(c => c.SetCacheList("currencyExchange", It.IsAny<List<CurrencyExchangeEntity>>(), TimeSpan.FromMinutes(1)), Times.Once);
            _mockCache.Verify(c => c.SetCacheList("currency", It.IsAny<List<CurrencyEntity>>(), TimeSpan.FromSeconds(30)), Times.Once);
        }

        [Fact]
        public async Task GetAllCurrencyExchanges_WithFullCache_ReturnsFromCache()
        {
            /// Arrange 
            var cachedExchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new CurrencyEntity("USD"),
                    new CurrencyEntity("EUR"),
                    0.85m
                )
            };

            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(cachedExchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
                .Returns((List<CurrencyExchangeEntity> entities) =>
                    entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            /// Act
            var result = await _service.GetAllCurrencyExchanges();

            /// Assert
            Assert.Single(result);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(0.85m, result[0].Value);
            _mockRepository.Verify(r => r.GetAllCurrencyExchanges(), Times.Never);
        }

        [Fact]
        public async Task GetAllCurrencyExchanges_WithEmptyCache_ReturnsFromRepository()
        {            
            /// Arrange
            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(new List<CurrencyExchangeEntity>());
            
            var repositoryExchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new CurrencyEntity("USD"),
                    new CurrencyEntity("EUR"),
                    0.85m
                )        
            };

            _mockRepository.Setup(r => r.GetAllCurrencyExchanges()).ReturnsAsync(repositoryExchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
                .Returns((List<CurrencyExchangeEntity> entities) =>
                    entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            /// Act
            var result = await _service.GetAllCurrencyExchanges();

            /// Assert
            Assert.Single(result);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(0.85m, result[0].Value);
            _mockCache.Verify(c => c.SetCacheList("currencyExchange", It.IsAny<List<CurrencyExchangeEntity>>(), TimeSpan.FromMinutes(1)), Times.Once);
            _mockRepository.Verify(r => r.GetAllCurrencyExchanges(), Times.Once);
        }

        [Fact]
        public async Task GetAllCurrencyExchanges_WithEmptyCacheAndEmptyRepository_ReturnsEmpty()
        {
            /// Arrange
            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(new List<CurrencyExchangeEntity>());
            
            _mockRepository.Setup(r => r.GetAllCurrencyExchanges()).ReturnsAsync(new List<CurrencyExchangeEntity>());

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
                .Returns((List<CurrencyExchangeEntity> entities) =>
                    entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());
            /// Act
            var result = await _service.GetAllCurrencyExchanges();

            /// Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetShortestPath_DirectConversion_ReturnsCorrectPath()
        {
            /// Arrange
            var exchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new CurrencyEntity("USD"),
                    new CurrencyEntity("EUR"),
                    0.85m
                )
            };

            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(exchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
                .Returns((List<CurrencyExchangeEntity> entities) =>
                    entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            _mockMapper.Setup(m => m.Map<CurrencyExchangeEntity>(It.IsAny<CurrencyExchangeDto>()))
                .Returns((CurrencyExchangeDto dto) =>
                    new CurrencyExchangeEntity
                    (
                        new CurrencyEntity(dto.From), 
                        new CurrencyEntity(dto.To), 
                        dto.Value
                    ));

            /// Act
            var result = await _service.GetShortestPath(new CurrencyExchangeDto
            (
                "USD",
                "EUR",
                100m
            ));

            /// Assert
            Assert.Single(result);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(85m, result[0].Value);
        }

        [Fact]
        public async Task GetShortestPath_IndirectConversion_ReturnsCorrectPath()
        {            
            /// Arrange
            var exchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new CurrencyEntity("USD"),
                    new CurrencyEntity("EUR"),
                    0.85m
                ),
                new
                (
                    new CurrencyEntity("EUR"),
                    new CurrencyEntity("GBP"),
                    0.9m
                )
            };

            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(exchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
               .Returns((List<CurrencyExchangeEntity> entities) =>
                   entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            _mockMapper.Setup(m => m.Map<CurrencyExchangeEntity>(It.IsAny<CurrencyExchangeDto>()))
                .Returns((CurrencyExchangeDto dto) =>
                    new CurrencyExchangeEntity
                    (
                        new CurrencyEntity(dto.From),
                        new CurrencyEntity(dto.To),
                        dto.Value
                    ));

            /// Act
            var result = await _service.GetShortestPath(new CurrencyExchangeDto
            (
                "USD",
                "GBP",
                100m
            ));

            /// Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(85m, result[0].Value);
            Assert.Equal("EUR", result[1].From);
            Assert.Equal("GBP", result[1].To);
            Assert.Equal(76.5m, result[1].Value);
        }

        [Fact]
        public async Task GetShortestPath_NoConversionPath_ThrowsException()
        {
            /// Arrange
            var exchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new CurrencyEntity("USD"),
                    new CurrencyEntity("EUR"),
                    0.85m
                )
            };

            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(exchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
               .Returns((List<CurrencyExchangeEntity> entities) =>
                   entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            _mockMapper.Setup(m => m.Map<CurrencyExchangeEntity>(It.IsAny<CurrencyExchangeDto>()))
                .Returns((CurrencyExchangeDto dto) =>
                    new CurrencyExchangeEntity
                    (
                        new CurrencyEntity(dto.From),
                        new CurrencyEntity(dto.To),
                        dto.Value
                    ));

            /// Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
            _service.GetShortestPath(new CurrencyExchangeDto
            (
                "USD",
                "GBP",
                100m
            )));
        }

        [Fact]
        public async Task GetShortestPath_EmptyCache_FetchesFromRepository()
        {
            /// Arrange
            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeEntity>("currencyExchange")).Returns(new List<CurrencyExchangeEntity>());
            
            var repositoryExchanges = new List<CurrencyExchangeEntity>
            {
                new
                (
                    new CurrencyEntity("USD"),
                    new CurrencyEntity("EUR"),
                    0.85m
                )
            };

            _mockRepository.Setup(r => r.GetAllCurrencyExchanges()).ReturnsAsync(repositoryExchanges);

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
               .Returns((List<CurrencyExchangeEntity> entities) =>
                   entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            _mockMapper.Setup(m => m.Map<CurrencyExchangeEntity>(It.IsAny<CurrencyExchangeDto>()))
                .Returns((CurrencyExchangeDto dto) =>
                    new CurrencyExchangeEntity
                    (
                        new CurrencyEntity(dto.From),
                        new CurrencyEntity(dto.To),
                        dto.Value
                    ));

            /// Act
            var result = await _service.GetShortestPath(new CurrencyExchangeDto
            (
                "USD",
                "EUR",
                100m
            ));

            /// Assert
            Assert.Single(result);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(85m, result[0].Value);
            _mockCache.Verify(c => c.SetCacheList("currencyExchange", It.IsAny<List<CurrencyExchangeEntity>>(), TimeSpan.FromSeconds(60)), Times.Once);
            _mockRepository.Verify(r => r.GetAllCurrencyExchanges(), Times.Once);
        }

        [Fact]
        public async Task GetShortestPath_EmptyCacheAndEmptyRepository_ThrowsException()
        {
            /// Arrange
            _mockCache.Setup(c => c.GetCacheList<CurrencyExchangeDto>("currencyExchange")).Returns(new List<CurrencyExchangeDto>());
            
            _mockRepository.Setup(r => r.GetAllCurrencyExchanges()).ReturnsAsync(new List<CurrencyExchangeEntity>());

            _mockMapper.Setup(m => m.Map<List<CurrencyExchangeDto>>(It.IsAny<List<CurrencyExchangeEntity>>()))
               .Returns((List<CurrencyExchangeEntity> entities) =>
                   entities.Select(e => new CurrencyExchangeDto(e.From.Code, e.To.Code, e.Value)).ToList());

            _mockMapper.Setup(m => m.Map<CurrencyExchangeEntity>(It.IsAny<CurrencyExchangeDto>()))
                .Returns((CurrencyExchangeDto dto) =>
                    new CurrencyExchangeEntity
                    (
                        new CurrencyEntity(dto.From),
                        new CurrencyEntity(dto.To),
                        dto.Value
                    ));

            /// Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
            _service.GetShortestPath(new CurrencyExchangeDto
            (
                "USD",
                "GBP",
                100m
            )));
        }

        // Pruebas para m�todos privados usando reflection
        [Fact]
        public void ProcessConversion_ValidPath_ReturnsCorrectConversion()
        {
            var exchanges = new List<CurrencyExchangeDto>
            {
                new
                (
                    "USD",
                    "EUR",
                    0.85m
                ),
                new
                (
                    "EUR",
                    "GBP",
                    0.9m
                )
            };

            var result = InvokePrivateMethod<List<CurrencyExchangeDto>>(_service, "ProcessConversion",
                new object[] { exchanges, "USD", "GBP", 100m });

            Assert.Equal(2, result.Count);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(85m, result[0].Value);
            Assert.Equal("EUR", result[1].From);
            Assert.Equal("GBP", result[1].To);
            Assert.Equal(76.5m, result[1].Value);
        }

        [Fact]
        public void BuildGraph_ValidExchanges_ReturnsCorrectGraph()
        {
            var exchanges = new List<CurrencyExchangeDto>
            {
                new
                (
                    "USD",
                    "EUR",
                    0.85m
                ),
                new
                (
                    "EUR",
                    "GBP",
                    0.9m
                )
            };

            var result = InvokePrivateMethod<Dictionary<string, List<(string To, decimal Value)>>>(_service, "BuildGraph",
                new object[] { exchanges });

            Assert.Equal(2, result.Count);
            Assert.Contains("USD", result.Keys);
            Assert.Contains("EUR", result.Keys);
            Assert.Single(result["USD"]);
            Assert.Single(result["EUR"]);
            Assert.Equal("EUR", result["USD"][0].To);
            Assert.Equal(0.85m, result["USD"][0].Value);
            Assert.Equal("GBP", result["EUR"][0].To);
            Assert.Equal(0.9m, result["EUR"][0].Value);
        }

        [Fact]
        public void FindShortestPath_ValidPath_ReturnsCorrectPath()
        {
            var graph = new Dictionary<string, List<(string To, decimal Value)>>
            {
                {
                    "USD", 
                    new List<(string, decimal)> { ("EUR", 0.85m) } 
                },
                {
                    "EUR", 
                    new List<(string, decimal)> { ("GBP", 0.9m) } 
                }
            };

            var result = InvokePrivateMethod<List<string>>(_service, "FindShortestPath",
                new object[] { graph, "USD", "GBP" });

            Assert.Equal(3, result.Count);
            Assert.Equal("USD", result[0]);
            Assert.Equal("EUR", result[1]);
            Assert.Equal("GBP", result[2]);
        }

        [Fact]
        public void CalculateConversion_ValidPath_ReturnsCorrectConversion()
        {
            var path = new List<string> { "USD", "EUR", "GBP" };
            var exchanges = new List<CurrencyExchangeDto>
            {
                new
                (
                    "USD",
                    "EUR",
                    0.85m
                ),
                new
                (
                    "EUR",
                    "GBP",
                    0.9m
                )
            };

            var result = InvokePrivateMethod<List<CurrencyExchangeDto>>(_service, "CalculateConversion",
                new object[] { path, 100m, exchanges });

            Assert.Equal(2, result.Count);
            Assert.Equal("USD", result[0].From);
            Assert.Equal("EUR", result[0].To);
            Assert.Equal(85m, result[0].Value);
            Assert.Equal("EUR", result[1].From);
            Assert.Equal("GBP", result[1].To);
            Assert.Equal(76.5m, result[1].Value);
        }

        [Fact]
        public void ValidateInformationReceived_WithNullList_ReturnsFalse()
        {
            var result = InvokePrivateMethod<bool>(_service, "ValidateInformationRecived", new Type[] { typeof(object) }, new object[] { null });
            Assert.False(result);
        }

        [Fact]
        public void ValidateInformationReceived_WithEmptyList_ReturnsFalse()
        {
            var result = InvokePrivateMethod<bool>(_service, "ValidateInformationRecived", new Type[] { typeof(object) }, new object[] { new List<object>() });
            Assert.False(result);
        }

        [Fact]
        public void ValidateInformationReceived_WithNonEmptyList_ReturnsTrue()
        {
            var result = InvokePrivateMethod<bool>(_service, "ValidateInformationRecived", new Type[] { typeof(object) }, new object[] { new List<object> { new object() } });
            Assert.True(result);
        }

        // M�todos auxiliar para invocar m�todos privados usando reflexion
        private T InvokePrivateMethod<T>(object obj, string methodName, object[] parameters)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)method.Invoke(obj, parameters);
        }

        private T InvokePrivateMethod<T>(object obj, string methodName, Type[] typeArguments, object[] parameters)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(typeArguments);
            return (T)genericMethod.Invoke(obj, parameters);
        }
    }
}