using Microsoft.VisualStudio.Sdk.TestFramework;


namespace UndoCloseTab;


[CollectionDefinition(CollectionName, DisableParallelization = true)]
public class VisualStudioTests : ICollectionFixture<GlobalServiceProvider>, ICollectionFixture<MefHostingFixture> {

    public const string CollectionName = nameof(VisualStudioTests);

}
