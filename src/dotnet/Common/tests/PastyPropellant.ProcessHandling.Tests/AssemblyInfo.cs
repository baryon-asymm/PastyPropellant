// EventBus is a static, process-wide bus and these tests subscribe to the real
// ProcessInfoLogEvent / ProcessErrorLogEvent types, which every test in this assembly shares.
// Running the collections serially keeps one test's subprocess chatter out of another's
// assertions, and keeps the timeout/cancellation timings free of CPU contention.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
