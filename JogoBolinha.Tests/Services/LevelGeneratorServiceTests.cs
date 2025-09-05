
using Xunit;
using JogoBolinha.Services;
using JogoBolinha.Models.Game;

namespace JogoBolinha.Tests.Services
{
    public class LevelGeneratorServiceTests
    {
        private readonly LevelGeneratorService _levelGeneratorService = new LevelGeneratorService();

        [Fact]
        public void GenerateLevel_ShouldReturnSolvableLevel()
        {
            for (int i = 1; i <= 200; i++)
            {
                var level = _levelGeneratorService.GenerateLevel(i);
                Assert.True(_levelGeneratorService.ValidateLevel(level.InitialState));
            }
        }

        [Fact]
        public void GenerateLevel_ShouldHaveCorrectDifficulty()
        {
            var level1 = _levelGeneratorService.GenerateLevel(1);
            Assert.Equal(Difficulty.Easy, level1.Difficulty);

            var level11 = _levelGeneratorService.GenerateLevel(11);
            Assert.Equal(Difficulty.Medium, level11.Difficulty);

            var level31 = _levelGeneratorService.GenerateLevel(31);
            Assert.Equal(Difficulty.Hard, level31.Difficulty);

            var level61 = _levelGeneratorService.GenerateLevel(61);
            Assert.Equal(Difficulty.Expert, level61.Difficulty);
        }

        [Fact]
        public void GenerateLevel_ShouldHaveVariety()
        {
            var level1 = _levelGeneratorService.GenerateLevel(1);
            System.Threading.Thread.Sleep(100);
            var level2 = _levelGeneratorService.GenerateLevel(1);

            Assert.NotEqual(level1.InitialState, level2.InitialState);
        }
    }
}
