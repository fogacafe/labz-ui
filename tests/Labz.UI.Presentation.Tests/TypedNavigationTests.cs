using Labz.UI.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Labz.UI.Presentation.Tests;

public sealed class TypedNavigationTests
{
    private sealed record ScreenRequest(int TradeId);

    [Fact]
    public async Task Typed_parameters_are_delivered_before_initialization()
    {
        var services = new ServiceCollection();
        services.AddTransient<ParameterForm>();
        services.AddTransient<ParameterPresenter>();
        services.AddSingleton<DeferredScreenFactory>();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<DeferredScreenFactory>();
        await using var screen = factory.Open<ParameterForm, ParameterPresenter, ScreenRequest>(new ScreenRequest(1024));
        await screen.Initialization;
        Assert.Equal(1024, screen.View.TradeId);
    }

    private sealed class ParameterForm : Form
    {
        public int TradeId { get; set; }
    }

    private sealed class ParameterPresenter : IAsyncScreenPresenter<ParameterForm>, IScreenParameters<ScreenRequest>
    {
        private ParameterForm? _view;
        private ScreenRequest? _parameters;
        public void Attach(ParameterForm view) => _view = view;
        public void SetParameters(ScreenRequest parameters) => _parameters = parameters;
        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_parameters is null) throw new InvalidOperationException("Parameters missing");
            _view!.TradeId = _parameters.TradeId;
            return Task.CompletedTask;
        }
        public Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
