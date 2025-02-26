using System.Threading;
using ParametricCombustionModel.Telemetry.ExecutionFrameMeasurers;
using Xunit;

namespace ParametricCombustionModel.Telemetry.Tests
{
    public class ExecutionFrameMeasurerTest
    {
        [Fact]
        public void StartFrame_ShouldRestartStopwatch()
        {
            // Arrange
            var measurer = new ExecutionFrameMeasurer();

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
            var measurer = new ExecutionFrameMeasurer();
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
            var measurer = new ExecutionFrameMeasurer();

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
            var measurer = new ExecutionFrameMeasurer();
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

        [Fact]
        public void EndFrame_ShouldEqualizeMeanExecutionTimeAndElapsedMs()
        {
            // Arrange
            const int elapsedMs = 10;
            var measurer = new ExecutionFrameMeasurer();

            // Act
            for (var i = 0; i < 100; i++)
            {
                measurer.StartFrame();
                Thread.Sleep(elapsedMs); // Simulate some execution time
                measurer.EndFrame();
            }

            // Assert
            const double epsMs = 1.0;
            Assert.True(measurer.MeanExecutionTime - elapsedMs < epsMs);

            // Assert
            Assert.True(measurer.StdDevExecutionTime < epsMs);
        }
    }
}
