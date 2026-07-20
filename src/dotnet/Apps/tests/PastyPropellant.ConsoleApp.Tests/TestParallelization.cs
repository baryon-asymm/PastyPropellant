// Test classes in this assembly share process-wide static state that xUnit's default
// cross-collection parallelism would make non-deterministic:
//
//   * EventBus<TEvent> is a static, process-wide pub/sub. A test that measures how many handlers
//     are subscribed (PreparePropellantDataHelperTest's leak characterisation) cannot get a stable
//     count while another class is subscribing and unsubscribing on the same bus — and both
//     PreparePropellantDataHelperTest and ConstructPropellantJsonHelperTest drive PrepareAsync,
//     which does exactly that.
//   * PdfSharp's GlobalFontSettings.FontResolver is likewise a single global slot.
//
// The assembly is small (well under a second for the non-LongRunning tests), so serialising it
// costs nothing measurable and buys determinism.

[assembly: CollectionBehavior(DisableTestParallelization = true)]
