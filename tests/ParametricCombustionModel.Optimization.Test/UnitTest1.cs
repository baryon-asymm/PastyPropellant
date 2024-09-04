using ParametricCombustionModel.Optimization.Events;
using ParametricCombustionModel.Optimization.Test.Models;
using ParametricCombustionModel.Telemetry.GCMetricsRecorders;
using ParametricCombustionModel.Telemetry.MetricsRecorders;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Test
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var context = TestPropellant.GetOptimizationContext();

            var optimizer = new MetricsGlobalSearchOptimizer();

            EventBus<OptimizationUpdatedEvent>.Subscribe((x) => HandleOptimizationUpdatedEvent(x, optimizer));

            var result = await optimizer.RunAsync(context);
            var a = 0;
        }

        private void HandleOptimizationUpdatedEvent(OptimizationUpdatedEvent updatedEvent, MetricsGlobalSearchOptimizer optimizer)
        {
            string message = $"GC time {GCCounter.PauseTimePercentage} %\nAllocMemory {(GCCounter.TotalMemory / 1024.0 / 1024.0):#,0.000000} MB\n";
            message += $"Gen0CollCount {GCCounter.GetCollectionCount(0)}\n";
            message += $"Gen1CollCount {GCCounter.GetCollectionCount(1)}\n";
            message += $"Gen2CollCount {GCCounter.GetCollectionCount(2)}\n";

            message += $"TargetFunctionCallsCount {optimizer.TargetFunctionMetricsRecorder.TargetFunctionCallsCount}\n";
            message += $"MeanExecutionTime {optimizer.TargetFunctionMetricsRecorder.MeanExecutionTime}\n";

            message += $"State: {updatedEvent.State}\nStepIndex: {updatedEvent.Args.Span[0]}\nFunCounts: {updatedEvent.Args.Span[1]}";
            if (updatedEvent.State == State.iter || updatedEvent.State == State.done)
            {
                message += $"\nBestFVal: {updatedEvent.Args.Span[2]}\n\n";
                for (int i = 3; i < updatedEvent.Args.Span.Length; i++)
                {
                    message += $"Point[{i - 3}]: {updatedEvent.Args.Span[i]}\n";
                }
            }
        }
    }
}
