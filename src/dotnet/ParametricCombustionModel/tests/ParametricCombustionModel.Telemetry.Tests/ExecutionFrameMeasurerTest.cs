using ParametricCombustionModel.Telemetry.Instruments;

namespace ParametricCombustionModel.Telemetry.Tests
{
    public class ExecutionFrameMeasurerTest
    {
        private const string MeasurerPath = "Test/ExecutionFrameMeasurer";

        [Fact]
        public void StartFrame_ShouldRestartStopwatch()
        {
            // Arrange
            var measurer = new EnhancedExecutionFrameMeasurer("Test", "Test description", "ms", MeasurerPath);

            // Act
            measurer.StartFrame();
            Thread.Sleep(100); // Simulate some execution time
            measurer.EndFrame();

            // Assert
            Assert.True(measurer.MeanExecutionTime > 0);
        }

        [Fact]
        public void EndFrame_ShouldUpdateMeanExecutionTime()
        {
            // Arrange
            var measurer = new EnhancedExecutionFrameMeasurer("Test", "Test description", "ms", MeasurerPath);
            measurer.StartFrame();
            Thread.Sleep(100); // Simulate some execution time
            measurer.EndFrame();
            var initialMeanExecutionTime = measurer.MeanExecutionTime;

            // Act
            measurer.StartFrame();
            Thread.Sleep(200); // Simulate some execution time
            measurer.EndFrame();

            // Assert
            Assert.True(measurer.MeanExecutionTime > initialMeanExecutionTime);
        }

        [Fact]
        public void EndFrame_ShouldUpdateCallsCount()
        {
            // Arrange
            var measurer = new EnhancedExecutionFrameMeasurer("Test", "Test description", "ms", MeasurerPath);

            // Act
            measurer.StartFrame();
            Thread.Sleep(100); // Simulate some execution time
            measurer.EndFrame();

            // Assert
            Assert.Equal(1u, measurer.CallsCount);
        }

        [Fact]
        public void EndFrame_ShouldUpdateStdDevExecutionTime()
        {
            // Arrange
            var measurer = new EnhancedExecutionFrameMeasurer("Test", "Test description", "ms", MeasurerPath);
            measurer.StartFrame();
            Thread.Sleep(100); // Simulate some execution time
            measurer.EndFrame();
            var initialStdDevExecutionTime = measurer.StdDevExecutionTime;

            // Act
            measurer.StartFrame();
            Thread.Sleep(200); // Simulate some execution time
            measurer.EndFrame();

            // Assert
            Assert.True(measurer.StdDevExecutionTime > initialStdDevExecutionTime);
        }
    }
}
