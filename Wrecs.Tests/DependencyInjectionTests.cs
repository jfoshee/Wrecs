namespace Wrecs.Tests;

public class DependencyInjectionTests
{
    private interface ITestSpatialDependency : ISystem;

    private interface IAmbiguousDependency : ISystem;

    private class DependencyA : ISystem;

    private class DependencyB : ISystem;

    private class InterfaceDependency : ITestSpatialDependency;

    private class AmbiguousDependencyOne : IAmbiguousDependency;

    private class AmbiguousDependencyTwo : IAmbiguousDependency;

    private class RequiresDependencyA : ISystem, IRequire<DependencyA>
    {
        public DependencyA? Injected { get; private set; }

        public void Inject(DependencyA dependency) => Injected = dependency;
    }

    private class RequiresDependencyAAndB : ISystem, IRequire<DependencyA>, IRequire<DependencyB>
    {
        public DependencyA? InjectedA { get; private set; }
        public DependencyB? InjectedB { get; private set; }

        public void Inject(DependencyA dependency) => InjectedA = dependency;
        public void Inject(DependencyB dependency) => InjectedB = dependency;
    }

    private class RequiresInterfaceDependency : ISystem, IRequire<ITestSpatialDependency>
    {
        public ITestSpatialDependency? Injected { get; private set; }

        public void Inject(ITestSpatialDependency dependency) => Injected = dependency;
    }

    private class RequiresAmbiguousDependency : ISystem, IRequire<IAmbiguousDependency>
    {
        public IAmbiguousDependency? Injected { get; private set; }

        public void Inject(IAmbiguousDependency dependency) => Injected = dependency;
    }

    [Fact(DisplayName = "DI base case: no requirements and no providers")]
    public void DependencyInjection_NoRequirementsAndNoProviders_DoesNotThrow()
    {
        var sim = new Sim();

        sim.Invoking(s => s.Tick())
            .Should().NotThrow();
    }

    [Fact(DisplayName = "DI injects one required dependency")]
    public void DependencyInjection_InjectsSingleDependency()
    {
        var sim = new Sim();
        var dependency = new DependencyA();
        var consumer = new RequiresDependencyA();

        sim.AddSystems(dependency, consumer);

        sim.Tick();

        consumer.Injected.Should().BeSameAs(dependency);
    }

    [Fact(DisplayName = "DI injects two different required dependencies")]
    public void DependencyInjection_InjectsTwoDifferentDependencies()
    {
        var sim = new Sim();
        var dependencyA = new DependencyA();
        var dependencyB = new DependencyB();
        var consumer = new RequiresDependencyAAndB();

        sim.AddSystems(dependencyA, dependencyB, consumer);

        sim.Tick();

        consumer.InjectedA.Should().BeSameAs(dependencyA);
        consumer.InjectedB.Should().BeSameAs(dependencyB);
    }

    [Fact(DisplayName = "DI injects interface dependency when exactly one match exists")]
    public void DependencyInjection_InjectsInterfaceDependency_WhenSingleMatchExists()
    {
        var sim = new Sim();
        var dependency = new InterfaceDependency();
        var consumer = new RequiresInterfaceDependency();

        sim.AddSystems(dependency, consumer);

        sim.Tick();

        consumer.Injected.Should().BeSameAs(dependency);
    }

    [Fact(DisplayName = "DI throws when interface dependency has multiple matches")]
    public void DependencyInjection_Throws_WhenInterfaceDependencyHasMultipleMatches()
    {
        var sim = new Sim();
        var dependencyOne = new AmbiguousDependencyOne();
        var dependencyTwo = new AmbiguousDependencyTwo();
        var consumer = new RequiresAmbiguousDependency();

        sim.AddSystems(dependencyOne, dependencyTwo, consumer);

        sim.Invoking(s => s.Tick())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Multiple systems match IAmbiguousDependency*");
    }
}
